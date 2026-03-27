using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Offline evolutionary-strategy trainer for ShipLayoutGenerator.
///
/// Works in Edit Mode (no Play needed) using [ExecuteInEditMode] and DestroyImmediate cleanup.
/// Attach this component to the same GameObject as ShipLayoutGenerator.
///
/// ── HOW IT WORKS ──────────────────────────────────────────────────────────
///  1. A "genome" is a set of parameter ranges (encoded from ShipLayoutTrainedParams).
///  2. A population of N genomes is evaluated — each genome is tested across
///     <seedsPerIndividual> random seeds. The average score is its fitness.
///  3. Top 50% survive.  Top 10% are kept unchanged ("elitism").
///     The other slots are filled by mutating or crossing over top survivors.
///  4. Repeat for G generations. Save the best genome as a ShipLayoutTrainedParams asset.
///
/// ── USAGE ─────────────────────────────────────────────────────────────────
///  • Right-click the component → "Train Layout AI (100 generations)"
///  • Right-click the component → "Train Layout AI (200 generations)"
///  • Right-click the component → "Evaluate Current Params (100 seeds)"
///
/// The trainer shows a cancelable progress bar so you can stop early.
/// A checkpoint is saved every 50 generations so progress is not lost
/// if Unity crashes mid-run.
/// </summary>
[ExecuteInEditMode]
public class ShipLayoutTrainer : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Population Settings")]
    [Tooltip("Number of parameter sets in each generation.")]
    public int populationSize    = 20;
    [Tooltip("How many random seeds each individual is scored on. More = more accurate but slower.")]
    public int seedsPerIndividual = 8;
    [Tooltip("Mutation strength (0=no mutation, 1=completely random).")]
    [Range(0f, 1f)]
    public float mutationStrength = 0.15f;

    [Header("Output")]
    [Tooltip("After training, the best parameter set is written here (or a new asset is created at the path below).")]
    public ShipLayoutTrainedParams outputParams;
    [Tooltip("Asset path for auto-created output (relative to Assets/).")]
    public string outputAssetPath = "Scripts/LevelDesign/TrainedParams_Best.asset";

    // ── Internals ──────────────────────────────────────────────────────────
    private ShipLayoutGenerator _gen;

    // Set to true by the outer RunTraining loop when cancel is detected.
    // EvaluateGenome checks this between seeds so it can exit early without
    // waiting for all seedsPerIndividual layouts to finish.
    private bool _cancelRequested = false;

    // ── Genome encoding ────────────────────────────────────────────────────
    // Each genome is a float[] of length GENOME_LEN.
    // Values are in [0,1] and are decoded into the actual parameter ranges at evaluation time.
    private const int GENOME_LEN = 39;

    // Genome indices (keep in sync with Decode / Encode helpers below)
    // ── Original 22 ─────────────────────────────────────────────
    private const int G_COR_W_MIN    =  0; private const int G_COR_W_MAX    =  1;
    private const int G_COR_H_MIN    =  2; private const int G_COR_H_MAX    =  3;
    private const int G_ROOM_H_MIN   =  4; private const int G_ROOM_H_MAX   =  5;
    private const int G_ENG_W_MIN    =  6; private const int G_ENG_W_MAX    =  7;
    private const int G_ENG_D_MIN    =  8; private const int G_ENG_D_MAX    =  9;
    private const int G_ENG_H_MIN    = 10; private const int G_ENG_H_MAX    = 11;
    private const int G_STR_MIN      = 12; private const int G_STR_MAX      = 13;
    private const int G_SIDE_MIN     = 14; private const int G_SIDE_MAX     = 15;
    private const int G_FIN_MIN      = 16; private const int G_FIN_MAX      = 17;
    private const int G_SPINE_BIAS   = 18;
    private const int G_ENG_BIAS     = 19;
    private const int G_ZSHAPE_BIAS  = 20;
    private const int G_BR_SIDE_BIAS = 21;
    // ── New structural parameters (indices 22-35) ────────────────
    private const int G_BASE_BUDGET     = 22; // baseRoomBudget   [3..20]
    private const int G_BUDGET_PER_LVL  = 23; // roomBudgetPerLevel [0.05..1.50]
    private const int G_W_CORRIDOR      = 24; // weightCorridor   [0..2]
    private const int G_W_HUB           = 25; // weightHub        [0..2]
    private const int G_W_UTILITY       = 26; // weightUtility    [0..2]
    private const int G_W_TERMINAL      = 27; // weightTerminal   [0..2]
    private const int G_BRANCH_CHANCE   = 28; // branchChance     [0..1]
    private const int G_MAX_BRANCH      = 29; // maxBranchDepth   [1..8]
    private const int G_SIDE_ROOM_CH    = 30; // sideRoomChance   [0..1]
    private const int G_DOUBLE_SIDE_CH  = 31; // doubleSideChance [0..1]
    private const int G_EXTRA_ENG       = 32; // extraEngChance   [0..1]
    private const int G_EXTRA_CARGO     = 33; // extraCargoChance [0..1]
    private const int G_LOOP_CHANCE     = 34; // loopChance       [0..1]
    private const int G_LSHAPE_BIAS     = 35; // lShapeBias       [0..1]
    private const int G_SPINE_LEN_MIN   = 36; // spineLenRange.x  [4..80]
    private const int G_SPINE_LEN_MAX   = 37; // spineLenRange.y  [4..80]
    private const int G_SIDE_SPAWN_BIAS = 38; // sideSpawnBias    [0..1]

    // ══════════════════════════════════════════════════════════════════════
    //  ContextMenu entry points
    // ══════════════════════════════════════════════════════════════════════

    [ContextMenu("Train Layout AI (100 generations)")]
    public void Train100() => RunTraining(100);

    [ContextMenu("Train Layout AI (200 generations)")]
    public void Train200() => RunTraining(200);

    [ContextMenu("Train Layout AI (100 gen × 1000 seeds)")]
    public void Train100_1000Seeds()
    {
        int prev = seedsPerIndividual;
        seedsPerIndividual = 1000;
        RunTraining(100);
        seedsPerIndividual = prev;
    }

    [ContextMenu("Train Layout AI (200 gen × 1000 seeds)")]
    public void Train200_1000Seeds()
    {
        int prev = seedsPerIndividual;
        seedsPerIndividual = 1000;
        RunTraining(200);
        seedsPerIndividual = prev;
    }

    [ContextMenu("Evaluate Current Params (100 seeds)")]
    public void EvalCurrent100() => EvaluateCurrentParams(100);

    // ══════════════════════════════════════════════════════════════════════
    //  Training loop
    // ══════════════════════════════════════════════════════════════════════

    private void RunTraining(int generations)
    {
        _gen = GetComponent<ShipLayoutGenerator>();
        if (_gen == null)
        {
            Debug.LogError("[ProcGen:Train] ShipLayoutTrainer requires a ShipLayoutGenerator on the same GameObject.");
            return;
        }
        if (!_gen.proceduralLayout)
        {
            Debug.LogWarning("[ProcGen:Train] ShipLayoutGenerator.proceduralLayout is false — enabling it for training.");
            _gen.proceduralLayout = true;
        }

        string separator = new string('═', 56);
        Debug.Log("[ProcGen:Train] " + separator);
        Debug.Log(string.Format(
            "[ProcGen:Train] <b>EVOLUTIONARY TRAINING START</b>  generations={0}  pop={1}  seeds={2}  mutation={3:F2}",
            generations, populationSize, seedsPerIndividual, mutationStrength));
        Debug.Log("[ProcGen:Train] " + separator);

        var rng = new System.Random(42);

        // ── Initialise population ──────────────────────────────────────────
        float[][] pop = new float[populationSize][];

        // Seed first individual from outputParams if assigned, else from hardcoded defaults
        pop[0] = outputParams != null ? EncodeParams(outputParams) : DefaultGenome();
        for (int i = 1; i < populationSize; i++)
            pop[i] = Mutate(pop[0], 0.5f, rng); // wide spread around the default

        float[] fitnesses  = new float[populationSize];
        float   bestEver   = float.NegativeInfinity;
        float[] bestGenome = (float[])pop[0].Clone();

        // ── Adaptive mutation state ────────────────────────────────────────────
        // When the best fitness hasn't improved for kStagnationLimit generations,
        // temporarily double the mutation strength to escape local optima.
        // The boost is reset whenever a new best is found.
        const int   kStagnationLimit    = 10;   // gens without improvement before boost kicks in
        const float kMutationBoost      = 2.5f; // multiplier applied to mutationStrength when stagnant
        const float kMaxMutationStrength= 0.6f; // upper cap on the boosted mutation to avoid pure random drift
        int stagnationCount = 0;

        // ── Generation loop ────────────────────────────────────────────────
        _cancelRequested = false;
        bool cancelled = false;

        // try-finally guarantees the progress bar is ALWAYS dismissed — even if an
        // exception is thrown during generation or if Unity's internal cancel flag
        // fires while we are blocked inside EvaluateGenome.
        try
        {
            for (int gen = 0; gen < generations; gen++)
            {
                // Evaluate each individual — pass generation as training level for level-scaled complexity
                for (int i = 0; i < populationSize; i++)
                {
                    fitnesses[i] = EvaluateGenome(pop[i], seedsPerIndividual, rng, gen + 1);

                    // EvaluateGenome sets _cancelRequested early-exit between seeds;
                    // check it here too so the outer loop breaks immediately.
                    if (_cancelRequested) { cancelled = true; break; }

#if UNITY_EDITOR
                    // Check for cancel after each individual (every seedsPerIndividual layouts)
                    // rather than waiting until the entire generation finishes.
                    float innerProgress = ((float)gen + (float)(i + 1) / populationSize) / generations;
                    string innerInfo = string.Format(
                        "Generation {0}/{1} — Individual {2}/{3} — Best Fitness: {4:F3}",
                        gen + 1, generations, i + 1, populationSize,
                        float.IsNegativeInfinity(bestEver) ? 0f : bestEver);
                    if (EditorUtility.DisplayCancelableProgressBar("Training Layout AI", innerInfo, innerProgress))
                    {
                        _cancelRequested = true;
                        Debug.LogWarning(string.Format(
                            "[ProcGen:Train] <color=#ff9900>Training cancelled by user at generation {0}/{1}, individual {2}/{3}.</color>  Best fitness so far: {4:F1}",
                            gen + 1, generations, i + 1, populationSize,
                            float.IsNegativeInfinity(bestEver) ? 0f : bestEver));
                        cancelled = true;
                        break;
                    }
#endif
                }
                if (cancelled) break;

                // Apply fitness sharing to prevent premature convergence.
                // Individuals that produce similar genomes share fitness, encouraging
                // the population to maintain diversity in the parameter space.
                ApplyFitnessSharing(fitnesses, pop);

                // Sort descending by fitness
                System.Array.Sort(fitnesses, pop,
                    System.Collections.Generic.Comparer<float>.Create((a, b) => b.CompareTo(a)));

                float best = fitnesses[0];
                float avg  = Average(fitnesses);
                float worst = fitnesses[populationSize - 1];

                if (best > bestEver)
                {
                    bestEver   = best;
                    bestGenome = (float[])pop[0].Clone();
                    stagnationCount = 0;  // reset stagnation on improvement
                    string scoreColor = bestEver >= 80f ? "#00ff88" : bestEver >= 40f ? "#ffcc00" : "#ff9900";
                    Debug.Log(string.Format(
                        "[ProcGen:Train] Gen {0,4}/{1}: <color={2}><b>★ NEW BEST {3:F1}</b></color>  avg={4:F1}  worst={5:F1}",
                        gen + 1, generations, scoreColor, bestEver, avg, worst));
                }
                else
                {
                    stagnationCount++;
                    if ((gen + 1) % 10 == 0)
                    {
                        Debug.Log(string.Format(
                            "[ProcGen:Train] Gen {0,4}/{1}:  best={2:F1}  avg={3:F1}  worst={4:F1}  stagnant={5}",
                            gen + 1, generations, best, avg, worst, stagnationCount));
                    }
                }

                // Checkpoint save every 50 generations so progress is not lost on a crash
                if ((gen + 1) % 50 == 0)
                {
                    Debug.Log(string.Format(
                        "[ProcGen:Train] <color=#aaddff>Checkpoint save at generation {0} — best fitness {1:F1}</color>",
                        gen + 1, bestEver));
                    SaveBestParams(bestGenome, bestEver, (gen + 1) * populationSize * seedsPerIndividual, gen + 1);
                }

                // ── Adaptive mutation ──────────────────────────────────────────────
                // When fitness has been stagnant for kStagnationLimit generations,
                // temporarily apply a boosted mutation to escape local optima.
                // The boost decays back to normal once a new best is found.
                float effectiveMutation = stagnationCount >= kStagnationLimit
                    ? Mathf.Min(kMaxMutationStrength, mutationStrength * kMutationBoost)
                    : mutationStrength;
                if (stagnationCount >= kStagnationLimit && (gen + 1) % 5 == 0)
                    Debug.Log(string.Format(
                        "[ProcGen:Train] <color=#ff9900>⚡ Adaptive mutation boost active — stagnant for {0} gens, mutation={1:F3}</color>",
                        stagnationCount, effectiveMutation));

                // Selection + reproduction
                int survivors = Mathf.Max(2, populationSize / 2);
                // Elites (top 10%) survive unchanged
                int elites = Mathf.Max(1, populationSize / 10);
                for (int i = elites; i < populationSize; i++)
                {
                    // Crossover two random survivors then mutate
                    int a = rng.Next(survivors);
                    int b = rng.Next(survivors);
                    float[] child = Crossover(pop[a], pop[b], rng);
                    pop[i] = Mutate(child, effectiveMutation, rng);
                }
            }
        }
        finally
        {
            // Always clear the progress bar — even on exception or Unity crash recovery.
            // This is the only reliable way to guarantee it disappears.
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        Debug.Log("[ProcGen:Train] " + separator);
        string completionLabel = cancelled ? "TRAINING CANCELLED" : "TRAINING COMPLETE";
        Debug.Log(string.Format(
            "[ProcGen:Train] <b>{0}</b>  best fitness={1:F1}  ({2} gens × {3} individuals × {4} seeds = {5} total layouts evaluated)",
            completionLabel, bestEver, generations, populationSize, seedsPerIndividual,
            generations * populationSize * seedsPerIndividual));
        Debug.Log("[ProcGen:Train] " + separator);

        SaveBestParams(bestGenome, bestEver, generations * populationSize * seedsPerIndividual, generations);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Evaluate current params across N seeds
    // ══════════════════════════════════════════════════════════════════════

    private void EvaluateCurrentParams(int seeds)
    {
        _gen = GetComponent<ShipLayoutGenerator>();
        if (_gen == null) { Debug.LogError("[ProcGen:Train] No ShipLayoutGenerator found."); return; }

        float[] genome = outputParams != null ? EncodeParams(outputParams) : DefaultGenome();
        var     rng    = new System.Random(System.Environment.TickCount);

        Debug.Log(string.Format(
            "[ProcGen:Train] <b>Evaluating {0} seeds...</b>", seeds));
        float fitness = EvaluateGenome(genome, seeds, rng);
        Debug.Log(string.Format(
            "[ProcGen:Train] <b>Evaluation result</b>: average fitness = <color=#00ff88>{0:F1}</color>  ({1} seeds)",
            fitness, seeds));
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Core: evaluate one genome across N seeds
    // ══════════════════════════════════════════════════════════════════════

    private float EvaluateGenome(float[] genome, int seeds, System.Random rng, int trainingLevel = 0)
    {
        ApplyGenome(genome);
        _gen.currentTrainingLevel = trainingLevel;
        _gen.scoringOnly = true;   // skip ProBuilder geometry — only placement stats needed
        float total = 0f;
        int evaluated = 0;
        for (int s = 0; s < seeds; s++)
        {
            // Honour early-cancel between seeds so the outer loop can detect it promptly.
            // We cannot abort mid-generation (ProBuilder operations are not interruptible),
            // but we skip remaining seeds to minimise blocking time.
            if (_cancelRequested) break;

            _gen.seed = rng.Next(1, int.MaxValue);
            _gen.GenerateShipLayout();
            var score = ShipLayoutScorer.Evaluate(_gen, trainingLevel);
            total += score.Total;
            evaluated++;
            CleanupChildren();
        }
        _gen.scoringOnly = false;  // restore geometry creation for non-training use
        _gen.currentTrainingLevel = 0;
        // Return 0 when no seeds were evaluated (cancel triggered before first seed).
        // 0 is a sensible neutral score here: it won't pollute the best-ever tracker
        // (which only improves) and won't cause NaN in the average calculation.
        return evaluated > 0 ? total / evaluated : 0f;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Genome encode / decode / mutate / crossover
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Encode ShipLayoutTrainedParams into a normalised [0,1] genome.</summary>
    private float[] EncodeParams(ShipLayoutTrainedParams p)
    {
        float[] g = new float[GENOME_LEN];
        // ── Original 22 ───────────────────────────────────────────
        g[G_COR_W_MIN]    = Norm(p.corridorWidthRange.x,  2f, 4f);
        g[G_COR_W_MAX]    = Norm(p.corridorWidthRange.y,  2f, 4f);
        g[G_COR_H_MIN]    = Norm(p.corridorHeightRange.x, 2f, 4f);
        g[G_COR_H_MAX]    = Norm(p.corridorHeightRange.y, 2f, 4f);
        g[G_ROOM_H_MIN]   = Norm(p.roomHeightRange.x,     2.5f, 4f);
        g[G_ROOM_H_MAX]   = Norm(p.roomHeightRange.y,     2.5f, 4f);
        g[G_ENG_W_MIN]    = Norm(p.engWRange.x,           8f, 16f);
        g[G_ENG_W_MAX]    = Norm(p.engWRange.y,           8f, 16f);
        g[G_ENG_D_MIN]    = Norm(p.engDRange.x,           7f, 14f);
        g[G_ENG_D_MAX]    = Norm(p.engDRange.y,           7f, 14f);
        g[G_ENG_H_MIN]    = Norm(p.engHRange.x,           3f,  6f);
        g[G_ENG_H_MAX]    = Norm(p.engHRange.y,           3f,  6f);
        g[G_STR_MIN]      = Norm(p.zStrLenRange.x,        3f, 20f);
        g[G_STR_MAX]      = Norm(p.zStrLenRange.y,        3f, 20f);
        g[G_SIDE_MIN]     = Norm(p.zSideLenRange.x,       3f, 28f);
        g[G_SIDE_MAX]     = Norm(p.zSideLenRange.y,       3f, 28f);
        g[G_FIN_MIN]      = Norm(p.zFinLenRange.x,        3f, 24f);
        g[G_FIN_MAX]      = Norm(p.zFinLenRange.y,        3f, 24f);
        g[G_SPINE_BIAS]   = p.spineRoomBias;
        g[G_ENG_BIAS]     = p.engSideRoomBias;
        g[G_ZSHAPE_BIAS]  = p.zShapeBias;
        g[G_BR_SIDE_BIAS] = p.branchSideRoomBias;
        // ── New structural parameters ──────────────────────────────
        g[G_BASE_BUDGET]    = Norm(p.baseRoomBudget,       3f, 20f);
        g[G_BUDGET_PER_LVL] = Norm(p.roomBudgetPerLevel,  0.05f, 1.50f);
        g[G_W_CORRIDOR]     = Norm(p.weightCorridor,       0f,  2f);
        g[G_W_HUB]          = Norm(p.weightHub,            0f,  2f);
        g[G_W_UTILITY]      = Norm(p.weightUtility,        0f,  2f);
        g[G_W_TERMINAL]     = Norm(p.weightTerminal,       0f,  2f);
        g[G_BRANCH_CHANCE]  = p.branchChance;
        g[G_MAX_BRANCH]     = Norm(p.maxBranchDepth,       1f, 15f);
        g[G_SIDE_ROOM_CH]   = p.sideRoomChance;
        g[G_DOUBLE_SIDE_CH] = p.doubleSideChance;
        g[G_EXTRA_ENG]      = p.extraEngChance;
        g[G_EXTRA_CARGO]    = p.extraCargoChance;
        g[G_LOOP_CHANCE]    = p.loopChance;
        g[G_LSHAPE_BIAS]    = p.lShapeBias;
        g[G_SPINE_LEN_MIN]  = Norm(p.spineLenRange.x,     4f, 80f);
        g[G_SPINE_LEN_MAX]  = Norm(p.spineLenRange.y,     4f, 80f);
        g[G_SIDE_SPAWN_BIAS]= p.sideSpawnBias;
        return g;
    }

    /// <summary>Decode a normalised genome into a ShipLayoutTrainedParams.</summary>
    private ShipLayoutTrainedParams DecodeGenome(float[] g)
    {
        var p = ScriptableObject.CreateInstance<ShipLayoutTrainedParams>();
        // ── Original 22 ───────────────────────────────────────────
        p.corridorWidthRange  = new Vector2(Denorm(g[G_COR_W_MIN],  2f,  4f), Denorm(g[G_COR_W_MAX],  2f,  4f));
        p.corridorHeightRange = new Vector2(Denorm(g[G_COR_H_MIN],  2f,  4f), Denorm(g[G_COR_H_MAX],  2f,  4f));
        p.roomHeightRange     = new Vector2(Denorm(g[G_ROOM_H_MIN], 2.5f,4f), Denorm(g[G_ROOM_H_MAX], 2.5f,4f));
        p.engWRange           = new Vector2(Denorm(g[G_ENG_W_MIN],  8f, 16f), Denorm(g[G_ENG_W_MAX],  8f, 16f));
        p.engDRange           = new Vector2(Denorm(g[G_ENG_D_MIN],  7f, 14f), Denorm(g[G_ENG_D_MAX],  7f, 14f));
        p.engHRange           = new Vector2(Denorm(g[G_ENG_H_MIN],  3f,  6f), Denorm(g[G_ENG_H_MAX],  3f,  6f));
        p.zStrLenRange        = new Vector2(Denorm(g[G_STR_MIN],    3f, 20f), Denorm(g[G_STR_MAX],    3f, 20f));
        p.zSideLenRange       = new Vector2(Denorm(g[G_SIDE_MIN],   3f, 28f), Denorm(g[G_SIDE_MAX],   3f, 28f));
        p.zFinLenRange        = new Vector2(Denorm(g[G_FIN_MIN],    3f, 24f), Denorm(g[G_FIN_MAX],    3f, 24f));
        p.spineRoomBias       = Mathf.Clamp01(g[G_SPINE_BIAS]);
        p.engSideRoomBias     = Mathf.Clamp01(g[G_ENG_BIAS]);
        p.zShapeBias          = Mathf.Clamp01(g[G_ZSHAPE_BIAS]);
        p.branchSideRoomBias  = Mathf.Clamp01(g[G_BR_SIDE_BIAS]);
        // ── New structural parameters ──────────────────────────────
        p.baseRoomBudget    = Mathf.RoundToInt(Denorm(g[G_BASE_BUDGET],    3f, 20f));
        p.roomBudgetPerLevel= Denorm(g[G_BUDGET_PER_LVL], 0.05f, 1.50f);
        p.weightCorridor    = Denorm(g[G_W_CORRIDOR], 0f, 2f);
        p.weightHub         = Denorm(g[G_W_HUB],      0f, 2f);
        p.weightUtility     = Denorm(g[G_W_UTILITY],  0f, 2f);
        p.weightTerminal    = Denorm(g[G_W_TERMINAL], 0f, 2f);
        p.branchChance      = Mathf.Clamp01(g[G_BRANCH_CHANCE]);
        p.maxBranchDepth    = Mathf.Clamp(Mathf.RoundToInt(Denorm(g[G_MAX_BRANCH], 1f, 15f)), 1, 15);
        p.sideRoomChance    = Mathf.Clamp01(g[G_SIDE_ROOM_CH]);
        p.doubleSideChance  = Mathf.Clamp01(g[G_DOUBLE_SIDE_CH]);
        p.extraEngChance    = Mathf.Clamp01(g[G_EXTRA_ENG]);
        p.extraCargoChance  = Mathf.Clamp01(g[G_EXTRA_CARGO]);
        p.loopChance        = Mathf.Clamp01(g[G_LOOP_CHANCE]);
        p.lShapeBias        = Mathf.Clamp01(g[G_LSHAPE_BIAS]);
        p.spineLenRange     = new Vector2(Denorm(g[G_SPINE_LEN_MIN], 4f, 80f), Denorm(g[G_SPINE_LEN_MAX], 4f, 80f));
        p.sideSpawnBias     = Mathf.Clamp01(g[G_SIDE_SPAWN_BIAS]);
        // Enforce min ≤ max for all ranges
        p.corridorWidthRange  = Ordered(p.corridorWidthRange);
        p.corridorHeightRange = Ordered(p.corridorHeightRange);
        p.roomHeightRange     = Ordered(p.roomHeightRange);
        p.engWRange           = Ordered(p.engWRange);
        p.engDRange           = Ordered(p.engDRange);
        p.engHRange           = Ordered(p.engHRange);
        p.zStrLenRange        = Ordered(p.zStrLenRange);
        p.zSideLenRange       = Ordered(p.zSideLenRange);
        p.zFinLenRange        = Ordered(p.zFinLenRange);
        p.spineLenRange       = Ordered(p.spineLenRange);
        return p;
    }

    /// <summary>Apply a genome to the generator (topology-only — room/vent sizes stay fixed).</summary>
    private void ApplyGenome(float[] g)
    {
        var p = DecodeGenome(g);
        // Room, corridor, and vent sizes are intentionally NOT applied from the genome.
        // The AI only controls map topology (branch count, corridor lengths, biases, etc.).
        // Keeping corridorWidth, corridorHeight, and roomHeight at their inspector defaults
        // prevents vent cap sizing glitches caused by mismatched procedural dimensions.
        if (_gen.trainedParams == null)
            _gen.trainedParams = p;
        else
        {
            // Copy fields into the existing asset to avoid creating garbage every eval.
            // ── Topology-only: skip dimension ranges (sizes stay fixed at defaults) ──
            _gen.trainedParams.zStrLenRange        = p.zStrLenRange;
            _gen.trainedParams.zSideLenRange       = p.zSideLenRange;
            _gen.trainedParams.zFinLenRange        = p.zFinLenRange;
            // ── Original biases ──────────────────────────────────────────────
            _gen.trainedParams.spineRoomBias       = p.spineRoomBias;
            _gen.trainedParams.engSideRoomBias     = p.engSideRoomBias;
            _gen.trainedParams.zShapeBias          = p.zShapeBias;
            _gen.trainedParams.branchSideRoomBias  = p.branchSideRoomBias;
            // ── New structural parameters ────────────────────────────────────
            _gen.trainedParams.baseRoomBudget      = p.baseRoomBudget;
            _gen.trainedParams.roomBudgetPerLevel  = p.roomBudgetPerLevel;
            _gen.trainedParams.weightCorridor      = p.weightCorridor;
            _gen.trainedParams.weightHub           = p.weightHub;
            _gen.trainedParams.weightUtility       = p.weightUtility;
            _gen.trainedParams.weightTerminal      = p.weightTerminal;
            _gen.trainedParams.branchChance        = p.branchChance;
            _gen.trainedParams.maxBranchDepth      = p.maxBranchDepth;
            _gen.trainedParams.sideRoomChance      = p.sideRoomChance;
            _gen.trainedParams.doubleSideChance    = p.doubleSideChance;
            _gen.trainedParams.extraEngChance      = p.extraEngChance;
            _gen.trainedParams.extraCargoChance    = p.extraCargoChance;
            _gen.trainedParams.loopChance          = p.loopChance;
            _gen.trainedParams.lShapeBias          = p.lShapeBias;
            _gen.trainedParams.spineLenRange       = p.spineLenRange;
            _gen.trainedParams.sideSpawnBias       = p.sideSpawnBias;
        }
        if (_gen.trainedParams != p) DestroyImmediate(p);
    }

    private float[] DefaultGenome()
    {
        var def = ScriptableObject.CreateInstance<ShipLayoutTrainedParams>(); // all defaults
        var g   = EncodeParams(def);
        DestroyImmediate(def);
        return g;
    }

    private float[] Mutate(float[] parent, float strength, System.Random rng)
    {
        float[] child = (float[])parent.Clone();
        for (int i = 0; i < GENOME_LEN; i++)
        {
            if (rng.NextDouble() < 0.5) // mutate ~50% of genes
                child[i] = Mathf.Clamp01(child[i] + (float)(rng.NextDouble() * 2.0 - 1.0) * strength);
        }
        return child;
    }

    private float[] Crossover(float[] a, float[] b, System.Random rng)
    {
        float[] child = new float[GENOME_LEN];
        // Uniform crossover
        for (int i = 0; i < GENOME_LEN; i++)
            child[i] = rng.NextDouble() < 0.5 ? a[i] : b[i];
        return child;
    }

    // ── Utility ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fitness sharing (Goldberg and Richardson, 1987).
    /// Divides each individual's raw fitness by a niche count based on genome similarity.
    /// Individuals with very similar genomes share their fitness, reducing the reward
    /// for converging to a single solution and preserving population diversity.
    /// nicheRadius=0.35 chosen to group individuals within ~35% of the parameter space;
    /// alpha=1.0 gives linear sharing within the niche (standard Goldberg formulation).
    /// </summary>
    private static void ApplyFitnessSharing(float[] fitnesses, float[][] pop)
    {
        const float nicheRadius = 0.35f;  // genome distance threshold for niche membership (~35% of space)
        const float alpha       = 1.0f;   // linear sharing (standard Goldberg & Richardson formulation)
        int n = fitnesses.Length;
        float[] shared = new float[n];
        for (int i = 0; i < n; i++)
        {
            float nicheCount = 0f;
            for (int j = 0; j < n; j++)
            {
                float d = GenomeDistance(pop[i], pop[j]);
                if (d < nicheRadius)
                    nicheCount += 1f - Mathf.Pow(d / nicheRadius, alpha);
            }
            // Never divide by less than 1 (ensures raw fitness is never inflated)
            shared[i] = fitnesses[i] / Mathf.Max(1f, nicheCount);
        }
        System.Array.Copy(shared, fitnesses, n);
    }

    /// <summary>Normalised Euclidean distance between two genomes in [0,1] space.</summary>
    private static float GenomeDistance(float[] a, float[] b)
    {
        float sum = 0f;
        int len = Mathf.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
        {
            float diff = a[i] - b[i];
            sum += diff * diff;
        }
        return Mathf.Sqrt(sum / len);
    }

    private static float Norm(float v, float lo, float hi) =>
        (hi > lo) ? Mathf.Clamp01((v - lo) / (hi - lo)) : 0f;

    private static float Denorm(float n, float lo, float hi) =>
        lo + Mathf.Clamp01(n) * (hi - lo);

    private static Vector2 Ordered(Vector2 v) =>
        v.x <= v.y ? v : new Vector2(v.y, v.x);

    private static float Average(float[] arr)
    {
        double sum = 0; foreach (float f in arr) sum += f;
        return (float)(sum / arr.Length);
    }

    /// <summary>
    /// Remove all generated geometry children (uses DestroyImmediate so it works in Edit Mode).
    /// In scoringOnly mode no children were created, so this is a safe no-op.
    /// </summary>
    private void CleanupChildren()
    {
        if (_gen == null) return;
        if (_gen.scoringOnly) return;  // no geometry was created; nothing to destroy
        Transform t = _gen.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
            DestroyImmediate(t.GetChild(i).gameObject);
    }

    // ── Save result ────────────────────────────────────────────────────────

    private void SaveBestParams(float[] genome, float fitness, int totalLayouts, int gens)
    {
        var best = DecodeGenome(genome);
        best.trainedFitnessScore = fitness;
        best.evaluationSeedCount = totalLayouts;
        best.trainedGeneration   = gens;

#if UNITY_EDITOR
        // If outputParams is assigned, update it in-place
        if (outputParams != null)
        {
            EditorUtility.CopySerializedIfDifferent(best, outputParams);
            DestroyImmediate(best);
            EditorUtility.SetDirty(outputParams);
            AssetDatabase.SaveAssets();
            Debug.Log(string.Format(
                "[ProcGen:Train] <color=#00ff88><b>✓ Best params saved</b></color> → '{0}'  fitness={1:F1}",
                AssetDatabase.GetAssetPath(outputParams), fitness));
        }
        else
        {
            // Create new asset
            string assetPath = "Assets/" + outputAssetPath;
            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(best, assetPath);
            AssetDatabase.SaveAssets();
            outputParams = best;
            Debug.Log(string.Format(
                "[ProcGen:Train] <color=#00ff88><b>✓ New params asset created</b></color> at '{0}'  fitness={1:F1}",
                assetPath, fitness));
        }
#else
        DestroyImmediate(best);
        Debug.LogWarning("[ProcGen:Train] Asset saving only available in the Unity Editor. Best params not persisted.");
#endif
    }
}
