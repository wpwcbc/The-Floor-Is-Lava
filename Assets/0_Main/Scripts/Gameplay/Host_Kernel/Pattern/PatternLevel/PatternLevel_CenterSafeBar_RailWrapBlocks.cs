using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_CenterSafeBar_RailWrapBlocks : PatternLevelSetupBase
{
    [Header("Center Safe Bar")]
    [SerializeField]
    private int safeBarWidth = 2;

    [Header("Rails")]
    [SerializeField]
    private int railWidth = 2;

    [SerializeField]
    private int railGapFromSafeBar = 2;

    [SerializeField]
    private int railGapBetweenRails = 1;

    [Header("Moving Block (on each rail)")]
    [SerializeField]
    private int movingBlockWidth = 2;

    [SerializeField]
    private int movingBlockHeight = 3;

    [SerializeField]
    private int gapBetweenBlocksOnRail = 1;

    [SerializeField]
    private float moveStepCooldown = 0.25f;

    // Layer rule assumed: higher wins.
    private const int SafeLayer = 1000;
    private const int WeaknessLayer = 200;
    private const int MovingNoneLayer = 100;
    private const int BackgroundForbiddenLayer = -1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_CenterSafeBar_RailWrapBlocks] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError(
                "[PatternLevel_CenterSafeBar_RailWrapBlocks] Invalid grid size: " +
                gridWidth + "x" + gridHeight,
                this);
            return;
        }

        int safeW = safeBarWidth;
        if (safeW < 1) safeW = 1;
        if (safeW > gridWidth) safeW = gridWidth;

        int rw = railWidth;
        if (rw < 1) rw = 1;
        if (rw > gridWidth) rw = gridWidth;

        int gapFromSafe = railGapFromSafeBar;
        if (gapFromSafe < 0) gapFromSafe = 0;

        int gapBetweenRails = railGapBetweenRails;
        if (gapBetweenRails < 0) gapBetweenRails = 0;

        int blockW = movingBlockWidth;
        int blockH = movingBlockHeight;

        if (blockW < 1) blockW = 1;
        if (blockH < 1) blockH = 1;

        if (blockW != rw)
        {
            blockW = rw;
        }

        if (blockH > gridHeight)
        {
            blockH = gridHeight;
        }

        int blockGap = gapBetweenBlocksOnRail;
        if (blockGap < 0) blockGap = 0;

        // 0) Bottom forbidden full grid (generic primitive)
        PatternDefinition forbiddenBgDef = PatternPool.CreateFullGrid(
            "ForbiddenFullGrid",
            gridWidth,
            gridHeight,
            CellRole.Forbidden,
            CellColor.Red);

        buffer.Add(new PatternInstance(
            forbiddenBgDef,
            new GridIndex(0, 0),
            BackgroundForbiddenLayer,
            null));

        // 1) Center safe vertical bar (generic primitive)
        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "CenterSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        int safeOriginX = (gridWidth - safeW) / 2;

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));

        // 2) Compute rail X origins outward from safe bar
        int safeMinX = safeOriginX;
        int safeMaxX = safeOriginX + safeW - 1;

        List<int> railOriginsX = new List<int>();

        int leftStartX = safeMinX - gapFromSafe - rw;
        while (leftStartX >= 0)
        {
            railOriginsX.Add(leftStartX);
            leftStartX -= (rw + gapBetweenRails);
        }

        int rightStartX = safeMaxX + 1 + gapFromSafe;
        while (rightStartX + rw - 1 < gridWidth)
        {
            railOriginsX.Add(rightStartX);
            rightStartX += (rw + gapBetweenRails);
        }

        if (railOriginsX.Count == 0)
        {
            return;
        }

        railOriginsX.Sort();

        // 3) Pattern defs for gray (None) + blue (Weakness), same size (generic primitive)
        PatternDefinition grayNoneDef = PatternPool.CreateSolidRect(
            "RailBlock",
            blockW,
            blockH,
            CellRole.None,
            CellColor.Gray);

        PatternDefinition weaknessBlueDef = PatternPool.CreateSolidRect(
            "RailWeakness",
            blockW,
            blockH,
            CellRole.Weakness,
            CellColor.Blue);

        int minOffsetX;
        int maxOffsetX;
        int minOffsetY;
        int maxOffsetY;

        PatternExtents.GetOffsetExtents(grayNoneDef, out minOffsetX, out maxOffsetX, out minOffsetY, out maxOffsetY);

        // 4) Spawn filled blocks on each rail (alternate direction)
        for (int railIndex = 0; railIndex < railOriginsX.Count; railIndex++)
        {
            int railX = railOriginsX[railIndex];

            int direction = (railIndex % 2 == 0) ? 1 : -1;

            List<int> originsY = BuildFilledRailBlockOrigins(gridHeight, blockH, blockGap, direction);

            for (int j = 0; j < originsY.Count; j++)
            {
                int originY = originsY[j];

                IPatternUpdateLogic grayMoveLogic = new AxisWrapLogic(
                    AxisWrapLogic.Axis.Vertical,
                    gridHeight,
                    moveStepCooldown,
                    direction,
                    minOffsetY,
                    maxOffsetY);

                PatternInstance grayMover = new PatternInstance(
                    grayNoneDef,
                    new GridIndex(railX, originY),
                    MovingNoneLayer,
                    grayMoveLogic);

                buffer.Add(grayMover);

                IPatternUpdateLogic followLogic = new FollowOriginLogic(grayMover, false);

                PatternInstance weaknessMover = new PatternInstance(
                    weaknessBlueDef,
                    new GridIndex(railX, originY),
                    WeaknessLayer,
                    followLogic);

                buffer.Add(weaknessMover);
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
            Debug.LogError("[PatternLevel_CenterSafeBar_RailWrapBlocks] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        int safeW = safeBarWidth;
        if (safeW < 1) safeW = 1;
        if (safeW > gridWidth) safeW = gridWidth;

        PatternDefinition forbiddenBgDef = PatternPool.CreateFullGrid(
            "ForbiddenFullGrid",
            gridWidth,
            gridHeight,
            CellRole.Forbidden,
            CellColor.Red);

        buffer.Add(new PatternInstance(
            forbiddenBgDef,
            new GridIndex(0, 0),
            BackgroundForbiddenLayer,
            null));

        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "CenterSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        int safeOriginX = (gridWidth - safeW) / 2;

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));
    }

    private static List<int> BuildFilledRailBlockOrigins(int gridHeight, int blockHeight, int gap, int direction)
    {
        List<int> ys = new List<int>();

        int h = blockHeight;
        if (h < 1)
        {
            return ys;
        }

        int spacing = h + gap;

        if (direction >= 0)
        {
            int y = 0;
            while (y + h <= gridHeight)
            {
                ys.Add(y);
                y += spacing;
            }
        }
        else
        {
            int y = gridHeight - h;
            while (y >= 0)
            {
                ys.Add(y);
                y -= spacing;
            }
        }

        return ys;
    }
}
