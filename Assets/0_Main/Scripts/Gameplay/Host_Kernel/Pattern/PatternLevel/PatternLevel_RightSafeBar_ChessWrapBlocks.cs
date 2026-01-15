using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_RightSafeBar_ChessWrapBlocks : PatternLevelSetupBase
{
    [Header("Right Safe Bar (Green)")]
    [SerializeField, Min(1)]
    private int safeBarWidth = 2;

    [Header("Chess Blocks (Red Forbidden vs Black None)")]
    [SerializeField, Min(1)]
    private int blockSize = 2;

    [SerializeField, Min(0.0f)]
    private float blockStepCooldownSeconds = 0.25f;

    [Header("Weakness Fill (Blue)")]
    [SerializeField]
    private bool fillWeakPoints = true;

    // Layers: higher wins
    private const int ForbiddenLayer = 0;
    private const int WeakPointLayer = -100; // under forbidden blocks
    private const int SafeLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] Invalid grid size.", this);
            return;
        }

        int safeW = Mathf.Max(1, safeBarWidth);
        if (safeW > gridWidth)
        {
            Debug.LogWarning(
                "[PatternLevel_RightSafeBar_ChessWrapBlocks] safeBarWidth > gridWidth, clamping. " +
                "safeW=" + safeW + ", gridWidth=" + gridWidth,
                this);
            safeW = gridWidth;
        }

        int s = Mathf.Max(1, blockSize);
        s = Mathf.Min(s, gridWidth, gridHeight);

        int maxOriginX = gridWidth - s;
        int maxOriginY = gridHeight - s;

        if (maxOriginX < 0 || maxOriginY < 0)
        {
            Debug.LogError(
                "[PatternLevel_RightSafeBar_ChessWrapBlocks] blockSize too large for grid. " +
                "grid=" + gridWidth + "x" + gridHeight + ", blockSize=" + s,
                this);
            return;
        }

        // --- Definitions ---
        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "RightSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        PatternDefinition redBlockDef = PatternPool.CreateSolidRect(
            "ChessRedBlock",
            s,
            s,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();

        // Track safe cells to exclude weakness
        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        // 1) Right safe bar (green)
        int safeOriginX = gridWidth - safeW;
        if (safeOriginX < 0)
        {
            safeOriginX = 0;
        }

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));

        for (int x = safeOriginX; x < safeOriginX + safeW; x++)
        {
            if (x < 0 || x >= gridWidth)
            {
                continue;
            }

            for (int y = 0; y < gridHeight; y++)
            {
                blockedBySafe.Add(new Vector2Int(x, y));
            }
        }

        // 2) Weakness fill everywhere except safe cells (blue, under forbidden)
        if (fillWeakPoints)
        {
            AddWeakPointsEverywhereExceptSafe(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe);
        }

        // 3) Chessboard red blocks (start lower-left as red), moving RIGHT with AxisWrapLogic
        // Adjustment #1: add ONE extra row above the top (aligned to blockSize), even if it exceeds the grid.
        if ((gridWidth % s) != 0 || (gridHeight % s) != 0)
        {
            Debug.LogWarning(
                "[PatternLevel_RightSafeBar_ChessWrapBlocks] Grid not divisible by blockSize. " +
                "Right edge remainder shows as None; Top remainder will also get a partially-outside row. " +
                "grid=" + gridWidth + "x" + gridHeight + ", blockSize=" + s,
                this);
        }

        float cd = Mathf.Max(0.0f, blockStepCooldownSeconds);

        // For a solid rect of width s: offsets along X are [0 .. s-1]
        int minOffsetX = 0;
        int maxOffsetX = s - 1;

        // This is the FIRST row origin that is >= gridHeight when height is divisible by s,
        // or the partial-cover row when not divisible.
        int extraTopRowOriginY = (gridHeight / s) * s;

        for (int originY = 0; originY <= extraTopRowOriginY; originY += s)
        {
            int blockY = originY / s;

            for (int originX = 0; originX <= maxOriginX; originX += s)
            {
                int blockX = originX / s;

                // Lower-left starts red => (0,0) is red => even parity is red.
                bool isRed = ((blockX + blockY) % 2) == 0;
                if (!isRed)
                {
                    continue;
                }

                IPatternUpdateLogic logic = new AxisWrapLogic(
                    AxisWrapLogic.Axis.Horizontal,
                    gridWidth,
                    cd,
                    direction: +1,
                    minOffset: minOffsetX,
                    maxOffset: maxOffsetX);

                buffer.Add(new PatternInstance(
                    redBlockDef,
                    new GridIndex(originX, originY),
                    ForbiddenLayer,
                    logic));
            }
        }
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        int safeW = Mathf.Max(1, safeBarWidth);
        if (safeW > gridWidth)
        {
            safeW = gridWidth;
        }

        // Adjustment #2: never spawn weakness in standby (for new levels).
        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "RightSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        int safeOriginX = gridWidth - safeW;
        if (safeOriginX < 0)
        {
            safeOriginX = 0;
        }

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));
    }

    private static void AddWeakPointsEverywhereExceptSafe(
        List<PatternInstance> buffer,
        PatternDefinition weakPointDef,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] Weakness buffer is null.");
            return;
        }

        if (weakPointDef == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] Weak point definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_ChessWrapBlocks] blockedBySafe is null.");
            return;
        }

        // WARNING: one PatternInstance per cell
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int p = new Vector2Int(x, y);

                if (blockedBySafe.Contains(p))
                {
                    continue;
                }

                buffer.Add(new PatternInstance(
                    weakPointDef,
                    new GridIndex(x, y),
                    WeakPointLayer,
                    null));
            }
        }
    }
}
