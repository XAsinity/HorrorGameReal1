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

    // ── Ship Scale (level-driven) ──────────────────────────────────────────
    [Header("Ship Scale (level-driven)")]
    [Tooltip("Base room budget at level 1.  Actual budget = baseRoomBudget + level * roomBudgetPerLevel")]
    public int baseRoomBudget = 5;
    [Tooltip("Rooms added per level (~35 rooms at level 200 with default 0.15).")]
    public float roomBudgetPerLevel = 0.15f;

    // ── Room Type Weights ──────────────────────────────────────────────────
    [Header("Room Type Weights (relative)")]
    [Tooltip("Relative frequency of spine corridor slots.")]
    public float weightCorridor = 1.0f;
    [Tooltip("Relative frequency of large hub rooms (engineering, cargo, dock).")]
    public float weightHub = 0.5f;
    [Tooltip("Relative frequency of medium utility rooms.")]
    public float weightUtility = 0.8f;
    [Tooltip("Relative frequency of dead-end terminal rooms.")]
    public float weightTerminal = 0.3f;

    // ── Branching Rules ────────────────────────────────────────────────────
    [Header("Branching Rules")]
    [Range(0f, 1f)]
    [Tooltip("Chance that each available branch slot off engineering is filled.")]
    public float branchChance = 0.3f;
    [Range(1, 5)]
    [Tooltip("Maximum branches that can fork off engineering.")]
    public int maxBranchDepth = 3;
    [Range(0f, 1f)]
    [Tooltip("Chance a spine corridor gets at least one side room.")]
    public float sideRoomChance = 0.6f;
    [Range(0f, 1f)]
    [Tooltip("Chance a corridor with one side room also gets a room on the opposite side.")]
    public float doubleSideChance = 0.4f;

    // ── Hub Repetition ─────────────────────────────────────────────────────
    [Header("Hub Repetition")]
    [Range(0f, 1f)]
    [Tooltip("Chance of placing an additional engineering hub in the spine.")]
    public float extraEngChance = 0.2f;
    [Range(0f, 1f)]
    [Tooltip("Chance of placing an additional cargo bay in the spine.")]
    public float extraCargoChance = 0.3f;

    // ── Connection Rules ───────────────────────────────────────────────────
    [Header("Connection Rules")]
    [Range(0f, 1f)]
    [Tooltip("(Reserved for future use) Chance corridors loop back to form a cycle.")]
    public float loopChance = 0.15f;
    [Range(0f, 1f)]
    [Tooltip("Chance an L-shape is attempted before Z-shape for branches (complement of zShapeBias).")]
    public float lShapeBias = 0.30f;

    [Header("Trained Fitness")]
    [Tooltip("Average score across the evaluation seeds — set automatically by ShipLayoutTrainer.")]
    public float trainedFitnessScore = 0f;
    [Tooltip("Number of seeds evaluated to produce this param set.")]
    public int   evaluationSeedCount = 0;
    [Tooltip("Generation in which this param set was produced.")]
    public int   trainedGeneration   = 0;
}
