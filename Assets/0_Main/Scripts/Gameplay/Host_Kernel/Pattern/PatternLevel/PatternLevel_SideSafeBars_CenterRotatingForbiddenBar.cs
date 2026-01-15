using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class PatternLevel_SideSafeBars_CenterRotatingForbiddenBar : PatternLevelSetupBase
{
    [Header("Safe Side Bars (Green)")]
    [SerializeField, Min(1)]
    private int safeSideBarWidth = 2;

    [Header("Rotating Forbidden Bar (Red)")]
    [SerializeField, Min(2)]
    private int rotatingBarLengthCells = 12;

    [Tooltip("How many frames between horizontal (0°) and vertical (90°). Total loop frames = 4*(N-1).")]
    [SerializeField, Min(2)]
    private int framesPerQuarterTurn = 9;

    [Tooltip("Seconds per frame advance. Smaller = faster rotation.")]
    [SerializeField, Min(0.0f)]
    private float rotateFrameCooldownSeconds = 0.08f;

    [Header("Weakness Fill (Blue)")]
    [SerializeField]
    private bool fillWeakPoints = true;

    [Tooltip("Centered empty square (no weakness) around the grid center. Size in cells. 0 = disabled.")]
    [SerializeField, Min(0)]
    private int centerEmptySquareSize = 4;

    // Layers: higher wins
    private const int ForbiddenLayer = 0;
    private const int WeakPointLayer = -100; // under forbidden
    private const int SafeLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] Invalid grid size.", this);
            return;
        }

        int safeW = Mathf.Max(1, safeSideBarWidth);
        if (safeW > gridWidth)
        {
            Debug.LogWarning(
                "[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] safeSideBarWidth > gridWidth, clamping. " +
                "safeW=" + safeW + ", gridWidth=" + gridWidth,
                this);
            safeW = gridWidth;
        }

        // Avoid full overlap when possible
        if ((safeW * 2) >= gridWidth && gridWidth >= 2)
        {
            int clamped = Mathf.Max(1, gridWidth / 2);
            Debug.LogWarning(
                "[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] Side safe bars would overlap; clamping width. " +
                "Requested=" + safeW + ", Clamped=" + clamped + ", gridWidth=" + gridWidth,
                this);
            safeW = clamped;
        }

        int len = Mathf.Max(2, rotatingBarLengthCells);

        int fq = Mathf.Max(2, framesPerQuarterTurn);
        if (fq > 64)
        {
            fq = 64;
        }

        float frameCd = Mathf.Max(0.0f, rotateFrameCooldownSeconds);

        // --- Definitions ---
        PatternDefinition safeSideBarDef = PatternPool.CreateVerticalBar(
            "SafeSideBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        PatternDefinition rotatingForbiddenBarDef = PatternPool.CreateRotatingForbiddenBar(
            len,
            fq);

        PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();

        // Track safe cells to exclude weakness
        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        // 1) Left safe bar
        buffer.Add(new PatternInstance(
            safeSideBarDef,
            new GridIndex(0, 0),
            SafeLayer,
            null));

        for (int x = 0; x < safeW; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                blockedBySafe.Add(new Vector2Int(x, y));
            }
        }

        // 2) Right safe bar
        int rightSafeX = gridWidth - safeW;
        if (rightSafeX < 0)
        {
            rightSafeX = 0;
        }

        if (rightSafeX != 0)
        {
            buffer.Add(new PatternInstance(
                safeSideBarDef,
                new GridIndex(rightSafeX, 0),
                SafeLayer,
                null));

            for (int x = rightSafeX; x < rightSafeX + safeW; x++)
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
        }

        // Build the centered empty rect FIRST, then derive pivot from it.
        CenterRect emptyRect;
        bool hasEmpty = TryBuildCenteredEmptyRect(gridWidth, gridHeight, centerEmptySquareSize, out emptyRect);

        // 3) Rotating forbidden bar pivot:
        // Use the empty square's center (so visuals align), otherwise use (w-1)/2.
        int pivotX = hasEmpty ? (emptyRect.MinX + emptyRect.MaxX) / 2 : (gridWidth - 1) / 2;
        int pivotY = hasEmpty ? (emptyRect.MinY + emptyRect.MaxY) / 2 : (gridHeight - 1) / 2;

        pivotX = Mathf.Clamp(pivotX, 0, gridWidth - 1);
        pivotY = Mathf.Clamp(pivotY, 0, gridHeight - 1);

        IPatternUpdateLogic rotateLogic = new FrameLoopLogic(frameCd);


        buffer.Add(new PatternInstance(
            rotatingForbiddenBarDef,
            new GridIndex(pivotX, pivotY),
            ForbiddenLayer,
            rotateLogic));

        // 4) Weakness fill (blue) everywhere except safe cells and the centered empty square
        if (fillWeakPoints)
        {
            AddWeakPointsEverywhereExceptSafeAndOptionalRect(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe,
                hasEmpty,
                emptyRect);
        }
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        int safeW = Mathf.Max(1, safeSideBarWidth);
        if (safeW > gridWidth)
        {
            safeW = gridWidth;
        }

        if ((safeW * 2) >= gridWidth && gridWidth >= 2)
        {
            safeW = Mathf.Max(1, gridWidth / 2);
        }

        PatternDefinition safeSideBarDef = PatternPool.CreateVerticalBar(
            "SafeSideBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        buffer.Add(new PatternInstance(
            safeSideBarDef,
            new GridIndex(0, 0),
            SafeLayer,
            null));

        int rightSafeX = gridWidth - safeW;
        if (rightSafeX < 0)
        {
            rightSafeX = 0;
        }

        if (rightSafeX != 0)
        {
            buffer.Add(new PatternInstance(
                safeSideBarDef,
                new GridIndex(rightSafeX, 0),
                SafeLayer,
                null));
        }

        // No weakness in standby. No rotating bar in standby.
    }

    private struct CenterRect
    {
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;
    }

    private static bool TryBuildCenteredEmptyRect(
        int gridWidth,
        int gridHeight,
        int requestedSize,
        out CenterRect rect)
    {
        rect = new CenterRect();

        if (requestedSize <= 0)
        {
            return false;
        }

        int size = requestedSize;

        if (size > gridWidth)
        {
            size = gridWidth;
        }
        if (size > gridHeight)
        {
            size = gridHeight;
        }

        int minX = (gridWidth - size) / 2;
        int minY = (gridHeight - size) / 2;

        rect.MinX = minX;
        rect.MinY = minY;
        rect.MaxX = minX + size - 1;
        rect.MaxY = minY + size - 1;

        return size > 0;
    }

    private static bool IsInsideRect(CenterRect rect, int x, int y)
    {
        return x >= rect.MinX && x <= rect.MaxX && y >= rect.MinY && y <= rect.MaxY;
    }

    private static void AddWeakPointsEverywhereExceptSafeAndOptionalRect(
        List<PatternInstance> buffer,
        PatternDefinition weakPointDef,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe,
        bool hasExcludeRect,
        CenterRect excludeRect)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] Weakness buffer is null.");
            return;
        }

        if (weakPointDef == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] Weak point definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_CenterRotatingForbiddenBar] blockedBySafe is null.");
            return;
        }

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int p = new Vector2Int(x, y);

                if (blockedBySafe.Contains(p))
                {
                    continue;
                }

                if (hasExcludeRect && IsInsideRect(excludeRect, x, y))
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
