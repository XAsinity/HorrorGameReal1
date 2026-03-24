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
    private const float W_OVERLAP            = -10f;  // each geometry overlap detected
    private const float W_GAP               = -5f;   // each wall gap detected
    private const float W_CORRIDOR_OVERLAP  = -25f;  // each corridor-vs-corridor intersection (severe)
    private const float W_VENT_OVERLAP      = -8f;   // each vent-branch clipping through a room (visual artifact, moderate penalty)
    private const float W_ALL_CLEAN         = +15f;  // bonus for zero diagnostics
    private const float W_FULL_ROOMS        = +10f;  // bonus when no rooms are skipped

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
    private const float W_BUDGET_MET        = +10f;  // bonus when rooms placed ≥ 80 % of target budget
    private const float W_DEAD_END          = -2f;   // mild penalty per capped corridor (dead-end)

    // ── Public API ────────────────────────────────────────────────────────────

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
        s.VentCutsMade      = gen.LastVentCutsMade;
        s.TerminalsCapped   = gen.LastTerminalsCapped;
        s.ActualSeed        = gen.LastActualSeed;
        s.TargetRoomCount   = gen.LastTargetRoomCount;
        s.DeadEndCount      = gen.LastDeadEndCount;

        // Base score
        float score = 0f;
        score += s.RoomsPlaced      * W_ROOM_PLACED;
        score += s.RoomsSkipped     * W_ROOM_SKIPPED;
        score += s.ZShapeCount      * W_Z_SHAPE;
        score += s.LShapeCount      * W_L_SHAPE;
        // No penalty for straight branches — they are valid fallbacks
        score += s.TerminalsCapped  * W_TERMINAL_CAPPED;
        score += (s.BranchCount - s.TerminalsCapped) * W_TERMINAL_PLACED;
        score += s.OverlapCount     * W_OVERLAP;
        score += s.GapCount         * W_GAP;
        score += s.CorridorOverlaps * W_CORRIDOR_OVERLAP;
        score += s.VentRoomOverlaps * W_VENT_OVERLAP;

        // ── Scale-aware rewards ──────────────────────────────────────────────
        // Room placement ratio — reward high success rate regardless of map size
        int totalAttempted = s.RoomsPlaced + s.RoomsSkipped;
        if (totalAttempted > 0)
        {
            float roomRatio = (float)s.RoomsPlaced / totalAttempted;
            score += roomRatio * W_ROOM_RATIO;
        }

        // Budget ratio — reward actually meeting the level's target room count
        if (s.TargetRoomCount > 0)
        {
            float budgetRatio = (float)s.RoomsPlaced / s.TargetRoomCount;
            if (budgetRatio >= 0.8f) score += W_BUDGET_MET;
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
        // Use TargetRoomCount to scale with the current level's room budget rather than
        // fixed magic numbers (previously hardcoded as 3/5 regardless of map size).
        int minRoomsForMediumTier = Mathf.Max(3, s.TargetRoomCount / 5);
        int minRoomsForLargeTier  = Mathf.Max(5, s.TargetRoomCount / 3);
        // Thresholds align with budget-based tiers in the generator:
        //   lvl > 80  → medium complexity,  lvl > 120 → large,  lvl > 160 → full
        if (trainingLevel > 80  && s.RoomsPlaced < minRoomsForMediumTier) score -= 5f;
        if (trainingLevel > 120 && s.RoomsPlaced < minRoomsForLargeTier)  score -= 8f;
        if (trainingLevel > 160 && s.BranchCount < 2)                     score -= 5f;

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
            "rooms:{3}+/{4}-  target:{5}  branches:{6}Z/{7}L/{8}S  caps:{9}  diag:{10}ov/{11}gap/{12}cor",
            tag, color, s.Total,
            s.RoomsPlaced, s.RoomsSkipped, s.TargetRoomCount,
            s.ZShapeCount, s.LShapeCount, s.StraightCount,
            s.TerminalsCapped,
            s.OverlapCount, s.GapCount, s.CorridorOverlaps));
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
        public int   VentCutsMade;
        public int   TerminalsCapped;
        public int   ActualSeed;
        // Scale-aware fields
        public int   TargetRoomCount;
        public int   DeadEndCount;

        public override string ToString() =>
            string.Format("Score={0:F1} rooms={1}/{2} target={3} Z={4} L={5} S={6} caps={7} diag={8}/{9}/{10} vent={11}",
                Total, RoomsPlaced, RoomsPlaced + RoomsSkipped, TargetRoomCount,
                ZShapeCount, LShapeCount, StraightCount, TerminalsCapped,
                OverlapCount, GapCount, CorridorOverlaps, VentRoomOverlaps);
    }
}
