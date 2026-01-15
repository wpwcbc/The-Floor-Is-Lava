using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_SideSafeBars_DualAxisMovingBars : PatternLevelSetupBase
{
    [Header("Safe Side Bars (Green)")]
    [SerializeField, Min(1)]
    private int safeSideBarWidth = 2;

    [Header("Forbidden Bars (Red) - Thickness")]
    [SerializeField, Range(1, 6)]
    private int forbiddenBarThickness = 2;

    [Header("Forbidden Bars (Red) - Speed (Step Cooldown Seconds)")]
    [SerializeField, Min(0.0f)]
    private float baseStepCooldownSeconds = 0.25f;

    [SerializeField, Range(0.0f, 0.9f)]
    private float stepCooldownJitterFraction = 0.15f;

    [SerializeField, Min(0.0f)]
    private float minStepCooldownSeconds = 0.01f;

    [Header("Weakness Fill (Blue)")]
    [SerializeField]
    private bool fillWeakPoints = true;

    public enum HorizontalStartDirection
    {
        Left = -1,
        Right = 1
    }

    public enum VerticalStartDirection
    {
        Down = -1,
        Up = 1
    }


    [Header("Starting Directions (Opposite by Default)")]
    [SerializeField]
    private HorizontalStartDirection verticalBarAStart = HorizontalStartDirection.Left;

    [SerializeField]
    private HorizontalStartDirection verticalBarBStart = HorizontalStartDirection.Right;

    [SerializeField]
    private VerticalStartDirection horizontalBarAStart = VerticalStartDirection.Up;

    [SerializeField]
    private VerticalStartDirection horizontalBarBStart = VerticalStartDirection.Down;

    // Layers: higher wins
    private const int BarsLayer = 0;
    private const int WeakPointLayer = -100; // under forbidden bars
    private const int SafeLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_DualAxisMovingBars] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_DualAxisMovingBars] Invalid grid size.", this);
            return;
        }

        int safeW = Mathf.Max(1, safeSideBarWidth);
        if (safeW > gridWidth)
        {
            Debug.LogWarning(
                "[PatternLevel_SideSafeBars_DualAxisMovingBars] safeSideBarWidth > gridWidth, clamping. " +
                "safeW=" + safeW + ", gridWidth=" + gridWidth,
                this);
            safeW = gridWidth;
        }

        if ((safeW * 2) >= gridWidth && gridWidth >= 2)
        {
            int clamped = Mathf.Max(1, gridWidth / 2);
            Debug.LogWarning(
                "[PatternLevel_SideSafeBars_DualAxisMovingBars] Side safe bars would overlap; clamping width. " +
                "Requested=" + safeW + ", Clamped=" + clamped + ", gridWidth=" + gridWidth,
                this);
            safeW = clamped;
        }

        int thick = forbiddenBarThickness;
        if (thick < 1)
        {
            thick = 1;
        }
        if (thick > gridWidth)
        {
            thick = gridWidth;
        }
        if (thick > gridHeight)
        {
            thick = gridHeight;
        }

        float jitter = Mathf.Clamp(stepCooldownJitterFraction, 0.0f, 0.9f);

        // --- Definitions ---
        PatternDefinition safeSideBarDef = PatternPool.CreateVerticalBar(
            "SafeSideBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        PatternDefinition verticalForbiddenBarDef = PatternPool.CreateVerticalBar(
            "VerticalForbiddenBar",
            gridHeight,
            thick,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition horizontalForbiddenBarDef = PatternPool.CreateHorizontalBar(
            "HorizontalForbiddenBar",
            gridWidth,
            thick,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern(); // assumed blue in your pool

        // Track safe cells so weakness fill doesn't overlap them
        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        // --- 1) Safe bars on left/right ---
        AddSafeVerticalBarAndBlockCells(buffer, safeSideBarDef, 0, gridWidth, gridHeight, safeW, blockedBySafe);

        int rightSafeX = gridWidth - safeW;
        if (rightSafeX < 0)
        {
            rightSafeX = 0;
        }

        if (rightSafeX != 0)
        {
            AddSafeVerticalBarAndBlockCells(buffer, safeSideBarDef, rightSafeX, gridWidth, gridHeight, safeW, blockedBySafe);
        }

        // --- 2) Fill weakness everywhere except safe cells (under forbidden layer) ---
        if (fillWeakPoints)
        {
            AddWeakPointsEverywhereExceptSafe(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe);
        }

        // --- 3) Two vertical red bars moving left/right (inside interior region between safe bars) ---
        int minVOriginX = safeW;
        int maxVOriginX = (gridWidth - safeW) - thick;

        if (maxVOriginX < minVOriginX)
        {
            Debug.LogError(
                "[PatternLevel_SideSafeBars_DualAxisMovingBars] No interior space for vertical forbidden bars. " +
                "gridWidth=" + gridWidth + ", safeW=" + safeW + ", thick=" + thick,
                this);
            return;
        }

        int startVOriginX = (minVOriginX + maxVOriginX) / 2;

        float vCooldownA = SampleJitteredCooldown(baseStepCooldownSeconds, jitter, minStepCooldownSeconds);
        float vCooldownB = SampleJitteredCooldown(baseStepCooldownSeconds, jitter, minStepCooldownSeconds);

        IPatternUpdateLogic vLogicA = new HorizontalBounceLogic(
            minVOriginX,
            maxVOriginX,
            vCooldownA,
            (int)verticalBarAStart);

        IPatternUpdateLogic vLogicB = new HorizontalBounceLogic(
            minVOriginX,
            maxVOriginX,
            vCooldownB,
            (int)verticalBarBStart);

        buffer.Add(new PatternInstance(
            verticalForbiddenBarDef,
            new GridIndex(startVOriginX, 0),
            BarsLayer,
            vLogicA));

        buffer.Add(new PatternInstance(
            verticalForbiddenBarDef,
            new GridIndex(startVOriginX, 0),
            BarsLayer,
            vLogicB));

        // --- 4) Two horizontal red bars moving up/down (full width) ---
        int minHOriginY = 0;
        int maxHOriginY = gridHeight - thick;

        if (maxHOriginY < minHOriginY)
        {
            Debug.LogError(
                "[PatternLevel_SideSafeBars_DualAxisMovingBars] Horizontal forbidden bar thickness larger than grid height. " +
                "gridHeight=" + gridHeight + ", thick=" + thick,
                this);
            return;
        }

        int startHOriginY = (minHOriginY + maxHOriginY) / 2;

        float hCooldownA = SampleJitteredCooldown(baseStepCooldownSeconds, jitter, minStepCooldownSeconds);
        float hCooldownB = SampleJitteredCooldown(baseStepCooldownSeconds, jitter, minStepCooldownSeconds);

        IPatternUpdateLogic hLogicA = new VerticalBounceLogic(
            minHOriginY,
            maxHOriginY,
            hCooldownA,
            (int)horizontalBarAStart);

        IPatternUpdateLogic hLogicB = new VerticalBounceLogic(
            minHOriginY,
            maxHOriginY,
            hCooldownB,
            (int)horizontalBarBStart);

        buffer.Add(new PatternInstance(
            horizontalForbiddenBarDef,
            new GridIndex(0, startHOriginY),
            BarsLayer,
            hLogicA));

        buffer.Add(new PatternInstance(
            horizontalForbiddenBarDef,
            new GridIndex(0, startHOriginY),
            BarsLayer,
            hLogicB));
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_DualAxisMovingBars] standby buffer is null.", this);
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

        PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();

        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        AddSafeVerticalBarAndBlockCells(buffer, safeSideBarDef, 0, gridWidth, gridHeight, safeW, blockedBySafe);

        int rightSafeX = gridWidth - safeW;
        if (rightSafeX < 0)
        {
            rightSafeX = 0;
        }

        if (rightSafeX != 0)
        {
            AddSafeVerticalBarAndBlockCells(buffer, safeSideBarDef, rightSafeX, gridWidth, gridHeight, safeW, blockedBySafe);
        }
    }

    private static void AddSafeVerticalBarAndBlockCells(
        List<PatternInstance> buffer,
        PatternDefinition safeBarDef,
        int originX,
        int gridWidth,
        int gridHeight,
        int barWidth,
        HashSet<Vector2Int> blockedBySafe)
    {
        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(originX, 0),
            SafeLayer,
            null));

        // Mark safe cells for weakness exclusion
        for (int x = originX; x < originX + barWidth; x++)
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

    private static void AddWeakPointsEverywhereExceptSafe(
        List<PatternInstance> buffer,
        PatternDefinition weakPointDef,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe)
    {
        if (weakPointDef == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_DualAxisMovingBars] Weak point definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_SideSafeBars_DualAxisMovingBars] blockedBySafe is null.");
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

    private static float SampleJitteredCooldown(float baseCooldownSeconds, float jitterFraction, float minCooldownSeconds)
    {
        float baseCd = baseCooldownSeconds;
        if (baseCd <= 0.0f)
        {
            return 0.0f;
        }

        float jitter = Mathf.Clamp(jitterFraction, 0.0f, 0.9f);
        float minMult = 1.0f - jitter;
        float maxMult = 1.0f + jitter;

        float mult = Random.Range(minMult, maxMult);
        float cd = baseCd * mult;

        if (cd < minCooldownSeconds)
        {
            cd = minCooldownSeconds;
        }

        return cd;
    }
}

// Custom Level
// Could be feed by a cust

// Define Safe Layer
// define forbbiden pattern (1 pattern, fixed grid size)
// In level, fill blue points on empty space
// 

// 