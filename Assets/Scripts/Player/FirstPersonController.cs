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
    private float _xRotation;
    private float _bobTimer;
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

        _audioSource.spatialBlend = 0f; // 2D for player's own footsteps
        _audioSource.playOnAwake = false;

        _targetHeight = standingHeight;

        if (cameraHolder != null)
            _defaultCameraY = cameraHolder.localPosition.y;
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
    public void OnSprint(InputValue value) => _isSprinting = value.isPressed;
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
        bool grounded = _controller.isGrounded;

        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f; // small downward force to keep grounded

        float speed = _isCrouching ? crouchSpeed :
                      _isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _controller.Move(move * speed * Time.deltaTime);

        // Jump
        if (_jumpRequested && grounded && !_isCrouching)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        _jumpRequested = false;

        // Gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    // ─── CROUCH ───────────────────────────────────────────────────

    private void HandleCrouch()
    {
        float currentHeight = _controller.height;
        float newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.deltaTime);

        _controller.height = newHeight;
        // Adjust center so we shrink from the top, not from the center
        _controller.center = new Vector3(0, newHeight / 2f, 0);

        if (cameraHolder != null)
        {
            float cameraY = _defaultCameraY * (newHeight / standingHeight);
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = cameraY;
            cameraHolder.localPosition = camPos;
        }
    }

    // ─── HEAD BOB ─────────────────────────────────────────────────

    private void HandleHeadBob()
    {
        if (cameraHolder == null) return;

        bool isMoving = _moveInput.sqrMagnitude > 0.1f && _controller.isGrounded;

        if (isMoving)
        {
            float multiplier = _isSprinting ? sprintBobMultiplier : 1f;
            _bobTimer += Time.deltaTime * bobFrequency * multiplier;

            float bobOffset = Mathf.Sin(_bobTimer) * bobAmplitude * multiplier;

            Vector3 camPos = cameraHolder.localPosition;
            float baseCameraY = _defaultCameraY * (_controller.height / standingHeight);
            camPos.y = baseCameraY + bobOffset;
            cameraHolder.localPosition = camPos;
        }
        else
        {
            _bobTimer = 0f;
        }
    }

    // ─── FOOTSTEPS ────────────────────────────────────────────────

    private void HandleFootsteps()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (!_controller.isGrounded || _moveInput.sqrMagnitude < 0.1f) return;

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
