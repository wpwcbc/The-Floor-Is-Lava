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

    // Layering:
    // - Ring and weakness points share the same base layer.
    // - Lava bar sits on a much higher layer so it always overrides them.
    private const int BaseLayer = 0;
    private const int BarLayerOffset = 100;

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

        // Origin at (0,0) so the ring covers the full grid extents.
        PatternInstance ringInstance = new PatternInstance(
            ringDefinition,
            new GridIndex(0, 0),
            BaseLayer,
            null); // no movement / no animation

        buffer.Add(ringInstance);

        // ------------------------------------------------------------
        // 2. Fill the interior of the ring with Weakness "Point" cells
        // ------------------------------------------------------------

        PatternDefinition pointDefinition = PatternPool.CreateWeaknessPointPattern();

        int minX = ringWidth;
        int maxX = gridWidth - ringWidth - 1;
        int minY = ringWidth;
        int maxY = gridHeight - ringWidth - 1;

        if (minX <= maxX && minY <= maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    PatternInstance pointInstance = new PatternInstance(
                        pointDefinition,
                        new GridIndex(x, y),
                        BaseLayer,   // same layer as ring
                        null);       // no movement / no animation

                    buffer.Add(pointInstance);
                }
            }
        }

        // ------------------------------------------------------------
        // 3. Vertical red forbidden bar, moving left-right and cycling
        // ------------------------------------------------------------

        PatternDefinition barDefinition = PatternPool.CreateVerticalLavaBarPattern(gridHeight);

        int barWidth = 3; // Must match CreateVerticalLavaBarPattern

        int minOriginX = 0;
        int maxOriginX = gridWidth - barWidth;
        if (maxOriginX < minOriginX)
        {
            maxOriginX = minOriginX;
        }

        // Start in the middle of the allowed range.
        int startOriginX = (minOriginX + maxOriginX) / 2;

        IPatternUpdateLogic barLogic = new HorizontalBounceLogic(
            minOriginX,
            maxOriginX,
            barStepCooldown);

        // Origin Y = 0 so bar spans from bottom to top (height = gridHeight).
        PatternInstance barInstance = new PatternInstance(
            barDefinition,
            new GridIndex(startOriginX, 0),
            BaseLayer + BarLayerOffset,  // always above ring + points
            barLogic);

        buffer.Add(barInstance);
    }
}
