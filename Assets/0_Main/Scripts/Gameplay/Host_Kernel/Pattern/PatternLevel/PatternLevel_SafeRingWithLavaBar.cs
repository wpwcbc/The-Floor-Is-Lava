using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_SafeRingWithLavaBar : PatternLevelSetupBase
{
    [Header("Ring")]
    [SerializeField]
    private int ringWidth = 3;

    [Header("Lava Bar")]
    [SerializeField]
    private int lavaBarWidth = 3;

    [Header("Lava Bar Movement")]
    [SerializeField]
    private float barStepCooldown = 0.25f;

    // Layer rule assumed: higher wins.
    // Ring sits on top.
    private const int RingLayer = 1000;
    private const int BarLayer = 0;
    private const int PointsLayer = -100;

    private int GetClampedRingWidthLocal(int gridWidth, int gridHeight)
    {
        int rw = ringWidth;
        if (rw < 0)
        {
            rw = 0;
        }

        int maxAllowed = Mathf.Max(0, (Mathf.Min(gridWidth, gridHeight) / 2));
        if (rw > maxAllowed)
        {
            rw = maxAllowed;
        }

        return rw;
    }

    private int GetClampedBarWidthLocal(int gridWidth)
    {
        int w = lavaBarWidth;
        if (w < 1)
        {
            w = 1;
        }

        if (w > gridWidth)
        {
            w = gridWidth;
        }

        return w;
    }

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SafeRingWithLavaBar] buffer is null.", this);
            return;
        }

        int rw = GetClampedRingWidthLocal(gridWidth, gridHeight);

        // 1) Safe ring (generic grid ring: role/color passed in)
        PatternDefinition ringDefinition = PatternPool.CreateGridRing(
            "SafeGridRing",
            gridWidth,
            gridHeight,
            rw,
            CellRole.Safe,
            CellColor.Green);

        buffer.Add(new PatternInstance(
            ringDefinition,
            new GridIndex(0, 0),
            RingLayer,
            null));

        // 2) Weakness points filling interior (iconic pattern is fine to keep)
        PatternDefinition pointDefinition = PatternPool.CreateWeaknessPointPattern();

        int minX = rw;
        int maxX = gridWidth - rw - 1;
        int minY = rw;
        int maxY = gridHeight - rw - 1;

        if (minX <= maxX && minY <= maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    buffer.Add(new PatternInstance(
                        pointDefinition,
                        new GridIndex(x, y),
                        PointsLayer,
                        null));
                }
            }
        }

        // 3) Lava bar (generic vertical bar: role/color passed in)
        int barWidth = GetClampedBarWidthLocal(gridWidth);

        PatternDefinition barDefinition = PatternPool.CreateVerticalBar(
            "VerticalLavaBar",
            gridHeight,
            barWidth,
            CellRole.Forbidden,
            CellColor.Red);

        int minOriginX = 0;
        int maxOriginX = gridWidth - barWidth;
        if (maxOriginX < minOriginX)
        {
            maxOriginX = minOriginX;
        }

        int startOriginX = (minOriginX + maxOriginX) / 2;

        IPatternUpdateLogic barLogic = new HorizontalBounceLogic(
            minOriginX,
            maxOriginX,
            barStepCooldown);

        buffer.Add(new PatternInstance(
            barDefinition,
            new GridIndex(startOriginX, 0),
            BarLayer,
            barLogic));
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SafeRingWithLavaBar] standby buffer is null.", this);
            return;
        }

        int rw = GetClampedRingWidthLocal(gridWidth, gridHeight);

        PatternDefinition ringDefinition = PatternPool.CreateGridRing(
            "SafeGridRing",
            gridWidth,
            gridHeight,
            rw,
            CellRole.Safe,
            CellColor.Green);

        buffer.Add(new PatternInstance(
            ringDefinition,
            new GridIndex(0, 0),
            RingLayer,
            null));
    }
}
