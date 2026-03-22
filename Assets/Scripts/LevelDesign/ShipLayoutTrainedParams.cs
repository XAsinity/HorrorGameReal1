using UnityEngine;

/// <summary>
/// ScriptableObject storing the parameter ranges that the evolutionary training loop
/// has determined produce the best ship layouts.  Assign to ShipLayoutGenerator.trainedParams
/// to use trained values instead of the hardcoded defaults.
///
/// Create via: Assets → Create → ProcGen → Ship Layout Params
/// </summary>
[CreateAssetMenu(fileName = "ShipLayoutParams", menuName = "ProcGen/Ship Layout Params")]
public class ShipLayoutTrainedParams : ScriptableObject
{
    [Header("Spine Corridor Dimensions")]
    public Vector2 corridorWidthRange  = new Vector2(2.5f, 3.5f);
    public Vector2 corridorHeightRange = new Vector2(2.5f, 3.2f);
    public Vector2 roomHeightRange     = new Vector2(3.0f, 3.5f);
    public Vector2 spineLenRange       = new Vector2(6f,  14f);

    [Header("Major Rooms")]
    public Vector2 dockWRange  = new Vector2(14f, 18f);
    public Vector2 dockDRange  = new Vector2(10f, 14f);
    public Vector2 dockHRange  = new Vector2( 5f,  6f);
    public Vector2 cargoWRange = new Vector2( 7f, 10f);
    public Vector2 cargoDRange = new Vector2( 5f,  8f);
    public Vector2 cargoHRange = new Vector2(3.5f, 5f);

    [Header("Engineering Hub")]
    public Vector2 engWRange = new Vector2(10f, 14f);
    public Vector2 engDRange = new Vector2( 9f, 12f);
    public Vector2 engHRange = new Vector2( 4f,  5f);

    [Header("Branch Configuration")]
    [Range(1, 3)] public int branchCountMin = 1;
    [Range(1, 3)] public int branchCountMax = 3;
    public Vector2 zStrLenRange    = new Vector2(4f,  8f);
    public Vector2 zSideLenRange   = new Vector2(5f, 12f);
    public Vector2 zFinLenRange    = new Vector2(5f, 10f);

    [Header("Placement Biases  (0=always skip, 1=always place)")]
    [Range(0f, 1f)] public float spineRoomBias    = 0.75f; // 1 - original 0.25 skip chance
    [Range(0f, 1f)] public float spineRoom2Bias   = 0.75f;
    [Range(0f, 1f)] public float engSideRoomBias  = 0.65f;
    [Range(0f, 1f)] public float branchSideRoomBias = 0.60f;
    [Range(0f, 1f)] public float zShapeBias       = 0.50f; // chance initial roll chooses Z over straight

    [Header("Trained Fitness")]
    [Tooltip("Average score across the evaluation seeds — set automatically by ShipLayoutTrainer.")]
    public float trainedFitnessScore = 0f;
    [Tooltip("Number of seeds evaluated to produce this param set.")]
    public int   evaluationSeedCount = 0;
    [Tooltip("Generation in which this param set was produced.")]
    public int   trainedGeneration   = 0;
}
