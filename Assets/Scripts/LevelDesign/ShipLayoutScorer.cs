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
    private const float W_STRAIGHT_FALLBACK  = -6f;   // each branch that fell back to straight
    private const float W_TERMINAL_PLACED    = +4f;   // each terminal room placed
    private const float W_TERMINAL_CAPPED    = -5f;   // each corridor end-cap (no terminal room)
    private const float W_OVERLAP            = -10f;  // each geometry overlap detected
    private const float W_GAP               = -5f;   // each wall gap detected
    private const float W_CORRIDOR_OVERLAP  = -25f;  // each corridor-vs-corridor intersection (severe)
    private const float W_ALL_CLEAN         = +15f;  // bonus for zero diagnostics
    private const float W_FULL_ROOMS        = +10f;  // bonus when no rooms are skipped
    private const float W_ALL_Z_SHAPE       = +12f;  // bonus when every branch is Z-shaped
    private const float W_MIXED_PATTERNS    = +5f;   // bonus for having at least one each of Z + L

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Computes and returns a score for the most-recently generated layout.</summary>
    public static LayoutScore Evaluate(ShipLayoutGenerator gen)
    {
        var s = new LayoutScore();
        s.RoomsPlaced       = gen.LastRoomsPlaced;
        s.RoomsSkipped      = gen.LastRoomsSkipped;
        s.OverlapCount      = gen.LastOverlapCount;
        s.GapCount          = gen.LastGapCount;
        s.CorridorOverlaps  = gen.LastCorridorOverlaps;
        s.ZShapeCount       = gen.LastZShapeCount;
        s.LShapeCount       = gen.LastLShapeCount;
        s.StraightCount     = gen.LastStraightCount;
        s.BranchCount       = gen.LastBranchCount;
        s.VentCutsMade      = gen.LastVentCutsMade;
        s.TerminalsCapped   = gen.LastTerminalsCapped;
        s.ActualSeed        = gen.LastActualSeed;

        // Base score
        float score = 0f;
        score += s.RoomsPlaced      * W_ROOM_PLACED;
        score += s.RoomsSkipped     * W_ROOM_SKIPPED;
        score += s.ZShapeCount      * W_Z_SHAPE;
        score += s.LShapeCount      * W_L_SHAPE;
        score += s.StraightCount    * W_STRAIGHT_FALLBACK;
        score += s.TerminalsCapped  * W_TERMINAL_CAPPED;
        score += (s.BranchCount - s.TerminalsCapped) * W_TERMINAL_PLACED;
        score += s.OverlapCount     * W_OVERLAP;
        score += s.GapCount         * W_GAP;
        score += s.CorridorOverlaps * W_CORRIDOR_OVERLAP;

        // Bonus awards
        bool clean = (s.OverlapCount == 0 && s.GapCount == 0 && s.CorridorOverlaps == 0);
        if (clean)                                        score += W_ALL_CLEAN;
        if (s.RoomsSkipped == 0)                          score += W_FULL_ROOMS;
        if (s.BranchCount > 0 && s.StraightCount == 0 && s.ZShapeCount == s.BranchCount)
                                                          score += W_ALL_Z_SHAPE;
        if (s.ZShapeCount > 0 && s.LShapeCount > 0)      score += W_MIXED_PATTERNS;

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
            "rooms:{3}+/{4}-  branches:{5}Z/{6}L/{7}S  caps:{8}  diag:{9}ov/{10}gap/{11}cor",
            tag, color, s.Total,
            s.RoomsPlaced, s.RoomsSkipped,
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
        public int   ZShapeCount;
        public int   LShapeCount;
        public int   StraightCount;
        public int   BranchCount;
        public int   VentCutsMade;
        public int   TerminalsCapped;
        public int   ActualSeed;

        public override string ToString() =>
            string.Format("Score={0:F1} rooms={1}/{2} Z={3} L={4} S={5} caps={6} diag={7}/{8}/{9}",
                Total, RoomsPlaced, RoomsPlaced + RoomsSkipped,
                ZShapeCount, LShapeCount, StraightCount, TerminalsCapped,
                OverlapCount, GapCount, CorridorOverlaps);
    }
}
