using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 5.5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float gravity = -15.0f;
    [SerializeField] private float jumpHeight = 1.0f;

    [Header("Movement Inertia")]
    [SerializeField] private float accelerationTime = 0.15f;   // Time to reach full speed
    [SerializeField] private float decelerationTime = 0.2f;    // Time to stop from full speed
    [SerializeField] private float directionChangeTime = 0.18f; // Time to switch directions

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private Transform cameraHolder;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 2.0f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float sprintBobMultiplier = 1.4f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.5f;

    // Internal state
    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _velocity;
    private Vector3 _currentMoveVelocity;  // Smoothed horizontal movement
    private Vector3 _moveDampVelocity;     // Used by SmoothDamp internally
    private float _xRotation;
    private float _bobTimer;
    private float _previousBobOffset;
    private float _footstepTimer;
    private float _defaultCameraY;
    private float _targetHeight;
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _jumpRequested;
    private AudioSource _audioSource;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake = false;

        _targetHeight = standingHeight;

        if (cameraHolder != null)
        {
            _defaultCameraY = cameraHolder.localPosition.y;

            if (Mathf.Approximately(_defaultCameraY, 0f))
            {
                _defaultCameraY = standingHeight - 0.2f;
                cameraHolder.localPosition = new Vector3(
                    cameraHolder.localPosition.x,
                    _defaultCameraY,
                    cameraHolder.localPosition.z
                );
            }
        }

        _controller.center = new Vector3(0, standingHeight / 2f, 0);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleCrouch();
        HandleHeadBob();
        HandleFootsteps();
    }

    // ─── INPUT SYSTEM CALLBACKS ───────────────────────────────────

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value) => _jumpRequested = true;

    public void OnCrouch(InputValue value)
    {
        _isCrouching = !_isCrouching;
        _targetHeight = _isCrouching ? crouchHeight : standingHeight;
    }

    public void OnInteract(InputValue value) => TryInteract();

    // ─── LOOK ─────────────────────────────────────────────────────

    private void HandleLook()
    {
        float mouseX = _lookInput.x * lookSensitivity;
        float mouseY = _lookInput.y * lookSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // ─── MOVEMENT ─────────────────────────────────────────────────

    private void HandleMovement()
    {
        // Poll sprint directly every frame
        _isSprinting = false;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            _isSprinting = true;
        else if (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
            _isSprinting = true;

        bool grounded = _controller.isGrounded;

        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f;

        float speed = _isCrouching ? crouchSpeed :
                      _isSprinting ? sprintSpeed : walkSpeed;

        // Calculate the target velocity based on raw input
        Vector3 targetMoveVelocity = (transform.right * _moveInput.x + transform.forward * _moveInput.y) * speed;

        // Pick the right smoothing time based on what the player is doing
        float smoothTime = GetSmoothTime(targetMoveVelocity);

        // Smoothly blend toward the target velocity (this creates the inertia)
        _currentMoveVelocity = Vector3.SmoothDamp(
            _currentMoveVelocity,
            targetMoveVelocity,
            ref _moveDampVelocity,
            smoothTime
        );

        // Apply smoothed horizontal movement
        _controller.Move(_currentMoveVelocity * Time.deltaTime);

        // Jump
        if (_jumpRequested && grounded && !_isCrouching)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        _jumpRequested = false;

        // Gravity (vertical — not smoothed, gravity should feel instant)
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
    }

    /// <summary>
    /// Returns the appropriate smooth time based on whether the player is
    /// accelerating, decelerating, or changing direction.
    /// </summary>
    private float GetSmoothTime(Vector3 targetVelocity)
    {
        bool hasInput = targetVelocity.sqrMagnitude > 0.01f;
        bool isMoving = _currentMoveVelocity.sqrMagnitude > 0.01f;

        if (!hasInput)
        {
            // Player released all keys — decelerate to stop
            return decelerationTime;
        }

        if (isMoving)
        {
            // Check if we're changing direction (dot product < 0 means opposite-ish)
            float dot = Vector3.Dot(_currentMoveVelocity.normalized, targetVelocity.normalized);
            if (dot < 0.3f)
            {
                // Significant direction change — use direction change time
                return directionChangeTime;
            }
        }

        // Accelerating or maintaining speed
        return accelerationTime;
    }

    // ─── CROUCH ───────────────────────────────────────────────────

    private void HandleCrouch()
    {
        float currentHeight = _controller.height;

        if (Mathf.Abs(currentHeight - _targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.deltaTime);

            _controller.height = newHeight;
            _controller.center = new Vector3(0, newHeight / 2f, 0);

            if (cameraHolder != null)
            {
                float cameraY = _defaultCameraY * (newHeight / standingHeight);
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = cameraY;
                cameraHolder.localPosition = camPos;
            }
        }
    }

    // ─── HEAD BOB ─────────────────────────────────────────────────

    private void HandleHeadBob()
    {
        if (cameraHolder == null) return;

        // Use actual smoothed velocity to drive head bob, not raw input
        // This means bob ramps up and down with the inertia — feels connected
        bool isMoving = _currentMoveVelocity.sqrMagnitude > 0.2f && _controller.isGrounded;

        if (isMoving)
        {
            float multiplier = _isSprinting ? sprintBobMultiplier : 1f;
            _bobTimer += Time.deltaTime * bobFrequency * multiplier;

            float bobOffset = Mathf.Sin(_bobTimer) * bobAmplitude * multiplier;

            Vector3 camPos = cameraHolder.localPosition;
            camPos.y -= _previousBobOffset;
            camPos.y += bobOffset;
            _previousBobOffset = bobOffset;
            cameraHolder.localPosition = camPos;
        }
        else
        {
            if (_previousBobOffset != 0f)
            {
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y -= _previousBobOffset;
                _previousBobOffset = 0f;
                cameraHolder.localPosition = camPos;
            }
            _bobTimer = 0f;
        }
    }

    // ─── FOOTSTEPS ────────────────────────────────────────────────

    private void HandleFootsteps()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        // Use smoothed velocity for footsteps too — no footstep sounds during the
        // brief deceleration slide when you're barely moving
        if (!_controller.isGrounded || _currentMoveVelocity.sqrMagnitude < 0.5f) return;

        float interval = _isCrouching ? footstepInterval * 1.5f :
                         _isSprinting ? footstepInterval * 0.65f : footstepInterval;

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer >= interval)
        {
            _footstepTimer = 0f;
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            _audioSource.PlayOneShot(clip, _isCrouching ? 0.3f : 0.7f);
        }
    }

    // ─── INTERACTION ──────────────────────────────────────────────

    private void TryInteract()
    {
        if (cameraHolder == null) return;

        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            interactable?.Interact(gameObject);
        }
    }
}