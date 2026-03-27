using UnityEngine;

/// <summary>
/// Stateless reward/scoring function for generated ship layouts.
/// Evaluates a ShipLayoutGenerator after it has run and returns a
/// LayoutScore describing how good the layout is.
///
/// Higher scores = better layouts.  Used by ShipLayoutTrainer to
/// drive the evolutionary optimisation loop.
/// </summary>
public static class ShipLayoutScorer
{
    // ── Reward weights ────────────────────────────────────────────────────────
    private const float W_ROOM_PLACED        = +5f;   // each room successfully placed
    private const float W_ROOM_SKIPPED       = -3f;   // each room that had to be skipped
    private const float W_Z_SHAPE            = +8f;   // each Z-shaped branch (most interesting)
    private const float W_L_SHAPE            = +4f;   // each L-shaped branch (intermediate)
    // W_STRAIGHT_FALLBACK removed — straight branches are valid, not failures
    // W_ALL_Z_SHAPE removed    — directly incentivised Z-spam
    private const float W_TERMINAL_PLACED    = +4f;   // each terminal room placed
    private const float W_TERMINAL_CAPPED    = -5f;   // each corridor end-cap (no terminal room)
    private const float W_OVERLAP            = -15f;  // each geometry overlap detected (harsher to discourage intersections)
    private const float W_GAP               = -5f;   // each wall gap detected
    private const float W_CORRIDOR_OVERLAP  = -25f;  // each corridor-vs-corridor intersection (severe)
    private const float W_VENT_OVERLAP      = -8f;   // each vent-branch clipping through a room (visual artifact, moderate penalty)
    private const float W_ALL_CLEAN         = +15f;  // bonus for zero diagnostics
    private const float W_FULL_ROOMS        = +10f;  // bonus when no rooms are skipped

    // Level-scaled overlap penalty escalation constants
    // At high levels the AI is more strongly penalised for geometry intersections
    // to discourage it from sacrificing correctness for extra rooms/branches.
    private const float OVERLAP_PENALTY_PER_LEVEL          = 0.10f;  // added to W_OVERLAP magnitude per level
    private const float CORRIDOR_OVERLAP_PENALTY_PER_LEVEL = 0.15f;  // added to W_CORRIDOR_OVERLAP magnitude per level

    private const float W_MIXED_PATTERNS    = +5f;   // bonus for having at least one each of Z + non-Z
    private const float W_PATTERN_MONOTONY  = -8f;   // penalty when ALL branches share the same pattern

    // Structural common-sense rewards
    private const float W_ENG_BOTH_SIDES    = +6f;   // engineering has both reactor (R) and lab (L) rooms
    private const float W_ENG_ONE_SIDE      = +3f;   // engineering has at least one side room
    private const float W_BALANCED_SIDES    = +4f;   // rooms placed on both left and right of corridors
    private const float W_TERMINAL_RATIO    = +5f;   // at least 50% of branches have a terminal room
    private const float W_BRANCH_VARIETY    = +3f;   // at least 2 different patterns among 3 branches

    // Scale-aware rewards (new)
    private const float W_ROOM_RATIO        = +20f;  // multiplier: (placed / (placed+skipped)) × this
    private const float W_DEAD_END          = -2f;   // mild penalty per capped corridor (dead-end)

    // ── Level-proportional size scaling ──────────────────────────────────────
    // levelScale = (trainingLevel / 50)² — quadratic growth so level 200 = 16×, level 100 = 4×
    // Minimum viable rooms: max(3, level × 0.15) — at level 200 that is 30 rooms
    // Under-minimum penalty: -(shortfall)² × 5  (quadratic, gets devastating fast)
    // Above-minimum reward:  +roomsPlaced × level × 0.3  (each room worth more at high levels)
    private const float MIN_ROOMS_PER_LEVEL = 0.15f;   // slope: rooms expected = level × this
    private const float UNDER_MIN_PENALTY   = 5f;      // multiplier for quadratic under-minimum penalty
    private const float ABOVE_MIN_REWARD    = 0.3f;    // per-room × level reward when at/above minimum

    // Legacy level-scaled map size rewards (kept, but now multiplied by levelScale)
    private const float W_MAP_SCALE_PER_ROOM    = +0.5f;  // rooms × level / LEVEL_SCALE_DIV × this
    private const float W_MANY_BRANCHES         = +10f;   // bonus when branchCount >= 4 at high levels
    private const float W_LONG_SPINE            = +8f;    // bonus when spineCount >= 4 at high levels
    private const float W_ROOM_COUNT_VERY_HIGH  = -15f;   // penalty for small rooms at trainingLevel > 160
    private const float LEVEL_SCALE_DIV         =  50f;   // divisor for level-scaled map size reward
    // Per-level bonus weights for spine and branch counts at high levels
    private const float SPINE_BONUS_PER_LEVEL   = 0.05f;  // LastSpineCount  × trainingLevel × this when trainingLevel > 100
    private const float BRANCH_BONUS_PER_LEVEL  = 0.08f;  // LastBranchCount × trainingLevel × this when trainingLevel > 100

    // Spawn-diversity reward — branches from spine corridors give the AI more
    // creative layout freedom and should be rewarded at mid/high levels.
    private const float W_SPAWN_DIVERSITY   = +6f;   // bonus when any branches spawn from spine corridor sides

    // ── Branch lateral-direction diversity ───────────────────────────────────
    // Maps should spread in BOTH left and right directions, not just straight forward.
    // Penalise when all lateral branches go the same way (all-left or all-right);
    // reward when the map genuinely extends in both X directions.
    private const float W_DIRECTION_BALANCE  = +12f;  // bonus when branches go both left AND right
    private const float W_NO_DIRECTION_MIX   = -30f;  // penalty when all lateral branches go same direction
    private const float W_ALL_STRAIGHT_BASE  = -10f;  // base all-straight penalty (scales with level)

    // ── Aspect ratio / map spread ─────────────────────────────────────────────
    // Reward maps that extend in both X and Z, producing a web-like footprint.
    // A ratio of 1.0 (square) is ideal; very elongated maps (all Z, no X spread) are penalised.
    private const float W_ASPECT_RATIO        = +15f;  // scales with aspect closeness to 1.0
    private const float W_ASPECT_EXTREME      = -30f;  // penalty for very elongated maps
    private const float ASPECT_EXTREME_LO     = 0.2f;  // width/depth ratio below this = extreme elongation
    private const float ASPECT_EXTREME_HI     = 5.0f;  // width/depth ratio above this = extreme elongation


    /// <summary>Computes and returns a score for the most-recently generated layout.</summary>
    /// <param name="trainingLevel">Current training generation/level (0 = no level-scaled rules).</param>
    public static LayoutScore Evaluate(ShipLayoutGenerator gen, int trainingLevel = 0)
    {
        var s = new LayoutScore();
        s.RoomsPlaced       = gen.LastRoomsPlaced;
        s.RoomsSkipped      = gen.LastRoomsSkipped;
        s.OverlapCount      = gen.LastOverlapCount;
        s.GapCount          = gen.LastGapCount;
        s.CorridorOverlaps  = gen.LastCorridorOverlaps;
        s.VentRoomOverlaps  = gen.LastVentRoomOverlaps;
        s.ZShapeCount       = gen.LastZShapeCount;
        s.LShapeCount       = gen.LastLShapeCount;
        s.StraightCount     = gen.LastStraightCount;
        s.BranchCount       = gen.LastBranchCount;
        s.SpineCount        = gen.LastSpineCount;
        s.VentCutsMade      = gen.LastVentCutsMade;
        s.TerminalsCapped   = gen.LastTerminalsCapped;
        s.ActualSeed        = gen.LastActualSeed;
        s.TargetRoomCount   = gen.LastTargetRoomCount;
        s.DeadEndCount      = gen.LastDeadEndCount;
        s.QualityRetries    = gen.LastQualityRetries;
        s.SideBranchCount   = gen.LastSideBranchCount;
        s.BranchesLeft      = gen.LastBranchesLeft;
        s.BranchesRight     = gen.LastBranchesRight;
        s.MapArea           = gen.LastMapArea;
        s.MapWidth          = gen.LastMapWidth;
        s.MapDepth          = gen.LastMapDepth;

        // Base score
        float score = 0f;
        score += s.RoomsPlaced      * W_ROOM_PLACED;
        score += s.RoomsSkipped     * W_ROOM_SKIPPED;
        score += s.ZShapeCount      * W_Z_SHAPE;
        score += s.LShapeCount      * W_L_SHAPE;
        // No penalty for straight branches — they are valid fallbacks
        score += s.TerminalsCapped  * W_TERMINAL_CAPPED;
        score += (s.BranchCount - s.TerminalsCapped) * W_TERMINAL_PLACED;
        // Level-scaled overlap penalties: base flat weight plus a per-level escalation
        // so the AI is progressively more punished for intersections at higher levels.
        score += s.OverlapCount     * (W_OVERLAP          - trainingLevel * OVERLAP_PENALTY_PER_LEVEL);
        score += s.GapCount         * W_GAP;
        score += s.CorridorOverlaps * (W_CORRIDOR_OVERLAP - trainingLevel * CORRIDOR_OVERLAP_PENALTY_PER_LEVEL);
        score += s.VentRoomOverlaps * W_VENT_OVERLAP;

        // ── Scale-aware rewards ──────────────────────────────────────────────
        // Room placement ratio — reward high success rate regardless of map size
        int totalAttempted = s.RoomsPlaced + s.RoomsSkipped;
        if (totalAttempted > 0)
        {
            float roomRatio = (float)s.RoomsPlaced / totalAttempted;
            score += roomRatio * W_ROOM_RATIO;
        }

        // Dead-end penalty — corridors capped without terminal rooms reduce score mildly
        score += s.DeadEndCount * W_DEAD_END;

        // Bonus awards
        bool clean = (s.OverlapCount == 0 && s.GapCount == 0 && s.CorridorOverlaps == 0 && s.VentRoomOverlaps == 0);
        if (clean)                                        score += W_ALL_CLEAN;
        if (s.RoomsSkipped == 0)                          score += W_FULL_ROOMS;

        // Pattern diversity — reward variety, penalise monotony
        bool hasZ   = s.ZShapeCount > 0;
        bool hasNonZ = (s.LShapeCount + s.StraightCount) > 0;
        if (hasZ && hasNonZ)
            score += W_MIXED_PATTERNS;

        // Monotony penalty: all branches share the same single pattern
        if (s.BranchCount > 1)
        {
            bool allZ        = s.ZShapeCount  == s.BranchCount;
            bool allL        = s.LShapeCount  == s.BranchCount;
            bool allStraight = s.StraightCount == s.BranchCount;
            if (allZ || allL || allStraight)
                score += W_PATTERN_MONOTONY;
        }

        // Branch variety reward: at least 2 different patterns among 3 branches
        if (s.BranchCount == 3)
        {
            int distinctPatterns = (s.ZShapeCount > 0 ? 1 : 0)
                                 + (s.LShapeCount > 0 ? 1 : 0)
                                 + (s.StraightCount > 0 ? 1 : 0);
            if (distinctPatterns >= 2)
                score += W_BRANCH_VARIETY;
        }

        // Specific all-straight penalty (beyond general monotony penalty) — boring layout.
        // Scale with training level so the AI is increasingly forced to use Z/L shapes.
        if (s.BranchCount > 1 && s.ZShapeCount == 0 && s.LShapeCount == 0)
        {
            float lvlScale = trainingLevel > 0 ? (1f + trainingLevel / 50f) : 1f;
            score += W_ALL_STRAIGHT_BASE * lvlScale;
        }

        // Branch lateral-direction diversity — penalise maps where all lateral branches
        // go the same direction (extending the map only left or only right).
        // This forces the AI to produce wider, more varied layouts.
        {
            bool hasLeft  = s.BranchesLeft  > 0;
            bool hasRight = s.BranchesRight > 0;
            int  lateralBranches = s.BranchesLeft + s.BranchesRight;
            if (lateralBranches >= 2)
            {
                if (hasLeft && hasRight)
                {
                    // Reward balanced left-right spread; scale with training level.
                    float dirBonus = W_DIRECTION_BALANCE;
                    if (trainingLevel > 0) dirBonus *= (1f + trainingLevel / 100f);
                    score += dirBonus;
                }
                else
                {
                    // Penalise when all lateral branches go the same direction.
                    float dirPenalty = W_NO_DIRECTION_MIX;
                    if (trainingLevel > 0) dirPenalty *= (1f + trainingLevel / 100f);
                    score += dirPenalty;
                }
            }
        }

        // Reward diverse Z and L shapes (capped at 2 each to prevent gaming)
        score += Mathf.Min(s.ZShapeCount, 2) * 3f;
        score += Mathf.Min(s.LShapeCount, 2) * 3f;

        // ── Aspect ratio / spread reward ─────────────────────────────────────
        // Reward maps that extend in both X and Z dimensions (web-like layout).
        // Very elongated maps (narrow along X = all Z, no branches going sideways) are penalised.
        if (s.MapWidth > 0.5f && s.MapDepth > 0.5f)
        {
            float ratio = s.MapWidth / s.MapDepth;  // > 1 = wider than deep, < 1 = narrower
            float aspectCloseness = ratio <= 1f ? ratio : 1f / ratio;  // 0..1, higher = more square
            float lvlMult = trainingLevel > 0 ? (1f + trainingLevel / 100f) : 1f;
            score += aspectCloseness * W_ASPECT_RATIO * lvlMult;
            // Extra penalty for extremely elongated maps
            if (ratio < ASPECT_EXTREME_LO || ratio > ASPECT_EXTREME_HI)
                score += W_ASPECT_EXTREME * lvlMult;
        }

        // Left-right balance penalty — penalise lopsided room distribution
        int roomBalance = Mathf.Abs(gen.LastRoomsLeftCount - gen.LastRoomsRightCount);
        score -= roomBalance * 2f;

        // Structural common-sense rewards
        // Engineering side rooms (proxy: check via gen fields if available,
        // otherwise approximate from placed/skipped counts)
        bool engBothSides = gen.LastEngReactorPlaced && gen.LastEngLabPlaced;
        bool engOneSide   = gen.LastEngReactorPlaced || gen.LastEngLabPlaced;
        if (engBothSides)     score += W_ENG_BOTH_SIDES;
        else if (engOneSide)  score += W_ENG_ONE_SIDE;

        // Balanced room distribution on both sides of corridors
        if (gen.LastRoomsLeftCount > 0 && gen.LastRoomsRightCount > 0)
            score += W_BALANCED_SIDES;

        // Terminal room ratio — at least 50% of branches have terminal rooms placed
        if (s.BranchCount > 0)
        {
            int terminalsPlaced = s.BranchCount - s.TerminalsCapped;
            if (terminalsPlaced >= Mathf.CeilToInt(s.BranchCount * 0.5f))
                score += W_TERMINAL_RATIO;
        }

        // ── Level-scaled minimum requirements (budget-relative thresholds) ──
        // These are LEGACY soft penalties — the primary gate is now the quadratic
        // under-minimum penalty in the size-gate block above.  Kept for continuity.
        int minRoomsForMediumTier = Mathf.Max(3, s.TargetRoomCount / 5);
        int minRoomsForLargeTier  = Mathf.Max(5, s.TargetRoomCount / 3);
        if (trainingLevel > 80  && s.RoomsPlaced < minRoomsForMediumTier) score -= 5f;
        if (trainingLevel > 120 && s.RoomsPlaced < minRoomsForLargeTier)  score -= 8f;
        if (trainingLevel > 160 && s.RoomsPlaced < minRoomsForLargeTier)  score += W_ROOM_COUNT_VERY_HIGH;  // harsher at very high level

        // Branch count minimum penalties — strongly penalise linear maps at high levels
        if (trainingLevel > 100 && s.BranchCount < 3) score -= 20f;
        if (trainingLevel > 150 && s.BranchCount < 4) score -= 15f;
        if (trainingLevel > 160 && s.BranchCount < 2) score -= 5f;    // keep existing mild penalty
        if (trainingLevel > 180 && s.BranchCount < 3) score -= 10f;   // escalating penalty at very high levels
        // Tightened branch floors: at level 150+ require 5 branches; at 190+ require 7 (-30 each missing)
        if (trainingLevel >= 150)
        {
            int minBranches150 = 5;
            int shortfall150 = minBranches150 - s.BranchCount;
            if (shortfall150 > 0)
                score -= shortfall150 * 30f;
        }
        if (trainingLevel >= 190)
        {
            int minBranches190 = 7;
            int shortfall190 = minBranches190 - s.BranchCount;
            if (shortfall190 > 0)
                score -= shortfall190 * 30f;
        }

        // ── Level-proportional minimum size gate + exponential scaling ──────────
        // levelScale = (level / 50)²  → level 50=1×, level 100=4×, level 200=16×
        // minExpected = max(3, level × 0.15)  → level 200 expects 30 rooms minimum
        if (trainingLevel > 0)
        {
            float levelScale    = (trainingLevel / 50f) * (trainingLevel / 50f);
            float minExpected   = Mathf.Max(3f, trainingLevel * MIN_ROOMS_PER_LEVEL);
            float actual        = s.RoomsPlaced;

            if (actual < minExpected)
            {
                // Quadratic penalty — gets devastating as the shortfall grows
                float shortfall = minExpected - actual;
                score -= shortfall * shortfall * UNDER_MIN_PENALTY;
            }
            else
            {
                // Per-room reward scales with level AND levelScale — each room at level 200
                // is worth 16× more than at level 50, strongly incentivising large maps.
                score += actual * trainingLevel * ABOVE_MIN_REWARD * levelScale;
            }

            // All legacy size rewards now multiplied by levelScale for exponential growth
            score += (s.RoomsPlaced * trainingLevel / LEVEL_SCALE_DIV) * W_MAP_SCALE_PER_ROOM * levelScale;

            // Branch / spine count bonuses also scale exponentially
            score += s.BranchCount * (trainingLevel / 40f) * 3f * levelScale;
            score += (s.RoomsPlaced + s.BranchCount * 2 + s.SpineCount) * (trainingLevel / 100f) * 2f * levelScale;

            // ── Harsh small-map penalty at high levels ──────────────────────────
            // At level 100+, maps with fewer than level/10 rooms are deeply penalised.
            if (trainingLevel > 100 && s.RoomsPlaced < trainingLevel / 10)
                score -= 50f * (trainingLevel / 50f);

            // Additional escalating floor: if rooms < level/20, apply extra penalty.
            float floorRooms = trainingLevel / 20f;
            if (s.RoomsPlaced < floorRooms)
            {
                float floorShortfall = floorRooms - s.RoomsPlaced;
                score -= floorShortfall * floorShortfall * 3f;
            }

            // ── Map area reward — proportional to level ─────────────────────────
            // Rewards layouts that physically occupy a larger footprint.
            score += s.MapArea * (trainingLevel / 100f) * 5.0f;

            // ── Multiplicative room reward — each additional room more valuable ──
            // At high levels every room placed gives a stacking bonus so the AI is
            // continuously rewarded for squeezing in extra rooms.
            if (trainingLevel > 50)
                score += s.RoomsPlaced * (trainingLevel / 25f);
        }
        else
        {
            // No training level — apply a small base size reward
            score += s.RoomsPlaced * W_MAP_SCALE_PER_ROOM;
        }

        // Explicit map scale bonus at high levels — exponentially scaled
        if (trainingLevel > 100)
        {
            float levelScale = (trainingLevel / 50f) * (trainingLevel / 50f);
            score += gen.LastSpineCount  * trainingLevel * SPINE_BONUS_PER_LEVEL  * levelScale;
            score += gen.LastBranchCount * trainingLevel * BRANCH_BONUS_PER_LEVEL * levelScale;
        }

        // Reward many branches at high levels
        if (trainingLevel > 120 && s.BranchCount >= 4) score += W_MANY_BRANCHES;
        if (trainingLevel > 120 && s.BranchCount >= 5) score += W_MANY_BRANCHES; // extra for 5+

        // Spine diversity reward — reward having many spine corridors at high levels
        if (trainingLevel > 80  && s.SpineCount >= 4) score += 10f;
        if (trainingLevel > 150 && s.SpineCount >= 6) score += 15f;
        // Legacy spine rewards (kept for compatibility with older checkpoints)
        if (trainingLevel > 100 && s.SpineCount >= 4) score += W_LONG_SPINE;
        if (trainingLevel > 100 && s.SpineCount >= 6) score += W_LONG_SPINE; // extra for 6+

        // Left/right balance bonus at high levels — reward rooms on both sides of corridors
        if (trainingLevel > 100 && gen.LastRoomsLeftCount > 0 && gen.LastRoomsRightCount > 0)
            score += 8f;  // extra incentive for balanced placement at high levels

        // Spawn diversity — reward layouts where some branches spawn from spine corridor sides
        // This gives the AI credit for creative exploration of the new spawn system.
        if (trainingLevel > 50 && s.SideBranchCount > 0)
            score += W_SPAWN_DIVERSITY;
        if (trainingLevel > 120 && s.SideBranchCount >= 2)
            score += W_SPAWN_DIVERSITY * 0.5f;  // extra bonus for multiple spine-side branches

        s.Total = score;
        return s;
    }

    /// <summary>Logs the score breakdown to the Unity console with color highlights.</summary>
    public static void LogScore(LayoutScore s, string label = "")
    {
        string tag   = string.IsNullOrEmpty(label) ? "" : " [" + label + "]";
        string color = s.Total >= 50f ? "#00ff88" : s.Total >= 0f ? "#ffcc00" : "#ff4444";
        Debug.Log(string.Format(
            "[ProcGen:Score]{0} <color={1}><b>Score={2:F1}</b></color>  " +
            "rooms:{3}+/{4}-  target:{5}  branches:{6}Z/{7}L/{8}S  caps:{9}  diag:{10}ov/{11}gap/{12}cor  area:{13:F0}",
            tag, color, s.Total,
            s.RoomsPlaced, s.RoomsSkipped, s.TargetRoomCount,
            s.ZShapeCount, s.LShapeCount, s.StraightCount,
            s.TerminalsCapped,
            s.OverlapCount, s.GapCount, s.CorridorOverlaps, s.MapArea));
    }

    // ── Data structures ───────────────────────────────────────────────────────

    /// <summary>Result of scoring one generated layout.</summary>
    public struct LayoutScore
    {
        public float Total;
        public int   RoomsPlaced;
        public int   RoomsSkipped;
        public int   OverlapCount;
        public int   GapCount;
        public int   CorridorOverlaps;
        public int   VentRoomOverlaps;
        public int   ZShapeCount;
        public int   LShapeCount;
        public int   StraightCount;
        public int   BranchCount;
        public int   SpineCount;
        public int   VentCutsMade;
        public int   TerminalsCapped;
        public int   ActualSeed;
        // Scale-aware fields
        public int   TargetRoomCount;
        public int   DeadEndCount;
        public int   QualityRetries; // number of AI quality/broken-layout retries performed
        // Spawn diversity
        public int   SideBranchCount;  // branches that spawned from spine corridor sides
        // Branch lateral-direction diversity
        public int   BranchesLeft;     // branches extending toward −X
        public int   BranchesRight;    // branches extending toward +X
        // Map footprint
        public float MapArea;          // bounding-box area (world units²) of all placed rooms/corridors
        public float MapWidth;         // X span of the bounding box
        public float MapDepth;         // Z span of the bounding box

        public override string ToString() =>
            string.Format("Score={0:F1} rooms={1}/{2} target={3} Z={4} L={5} S={6} caps={7} diag={8}/{9}/{10} vent={11} area={12:F0}",
                Total, RoomsPlaced, RoomsPlaced + RoomsSkipped, TargetRoomCount,
                ZShapeCount, LShapeCount, StraightCount, TerminalsCapped,
                OverlapCount, GapCount, CorridorOverlaps, VentRoomOverlaps, MapArea);
    }
}
