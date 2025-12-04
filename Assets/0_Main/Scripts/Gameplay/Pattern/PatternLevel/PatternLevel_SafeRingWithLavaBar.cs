using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_SafeRingWithLavaBar : PatternLevelSetupBase
{
    [Header("Grid Size")]
    [SerializeField]
    private int gridWidth = 32;

    [SerializeField]
    private int gridHeight = 18;

    [Header("Lava Bar Movement")]
    [SerializeField]
    private float barStepCooldown = 0.25f;

    protected override void BuildLevelPatterns(List<PatternInstance> buffer)
    {
        if (buffer == null)
        {
            return;
        }

        // ------------------------------------------------------------
        // 1. Stable green safe ring with grid size, ring width = 3
        // ------------------------------------------------------------

        int ringWidth = 3;

        PatternDefinition ringDefinition = PatternPool.CreateSafeGridRingPattern(
            gridWidth,
            gridHeight,
            ringWidth);

        // Origin at (0,0) so the ring covers the full grid extents
        PatternInstance ringInstance = new PatternInstance(
            ringDefinition,
            new GridIndex(0, 0),
            0,          // base layer
            null);      // no movement / no animation

        buffer.Add(ringInstance);

        // ------------------------------------------------------------
        // 2. Vertical red forbidden bar, moving left-right and cycling
        // ------------------------------------------------------------

        PatternDefinition barDefinition = PatternPool.CreateVerticalLavaBarPattern(gridHeight);

        int barWidth = 3; // Must match CreateVerticalLavaBarPattern

        int minOriginX = 0;
        int maxOriginX = gridWidth - barWidth;
        if (maxOriginX < minOriginX)
        {
            maxOriginX = minOriginX;
        }

        // Start in the middle of the allowed range
        int startOriginX = (minOriginX + maxOriginX) / 2;

        IPatternUpdateLogic barLogic = new HorizontalBounceLogic(
            minOriginX,
            maxOriginX,
            barStepCooldown);

        // Origin Y = 0 so bar spans from bottom to top (height = gridHeight)
        PatternInstance barInstance = new PatternInstance(
            barDefinition,
            new GridIndex(startOriginX, 0),
            1,              // above the safe ring layer
            barLogic);

        buffer.Add(barInstance);
    }
}
