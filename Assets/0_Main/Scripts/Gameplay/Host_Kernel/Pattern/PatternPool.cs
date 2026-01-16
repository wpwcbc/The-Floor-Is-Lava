using System.Collections.Generic;
using UnityEngine;

public static class PatternPool
{
    // Optional cache: avoids rebuilding identical immutable PatternDefinitions.
    // Key is the generated id (which includes dimensions + role + color).
    private static readonly Dictionary<string, PatternDefinition> _cache =
        new Dictionary<string, PatternDefinition>();

    // =========================================================
    // PRIMITIVES (parameterized builders)
    // =========================================================

    public static PatternDefinition CreateSolidRect(
        string idPrefix,
        int width,
        int height,
        CellRole role,
        CellColor color)
    {
        int w = ClampAtLeast(width, 1);
        int h = ClampAtLeast(height, 1);

        string prefix = NormalizePrefix(idPrefix, "SolidRect");
        string id = BuildId_Rect(prefix, w, h, role, color);

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        PatternDefinition definition = PatternFactory.CreateSolidRectPattern(
            id,
            w,
            h,
            role,
            color);

        _cache[id] = definition;
        return definition;
    }

    public static PatternDefinition CreateHollowRect(
        string idPrefix,
        int width,
        int height,
        int ringWidth,
        CellRole role,
        CellColor color)
    {
        int w = ClampAtLeast(width, 1);
        int h = ClampAtLeast(height, 1);
        int rw = ClampAtLeast(ringWidth, 1);

        string prefix = NormalizePrefix(idPrefix, "HollowRect");
        string id = BuildId_HollowRect(prefix, w, h, rw, role, color);

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        PatternDefinition definition = PatternFactory.CreateHollowRectPattern(
            id,
            w,
            h,
            rw,
            role,
            color);

        _cache[id] = definition;
        return definition;
    }

    public static PatternDefinition CreateSingleCell(
        string idPrefix,
        CellRole role,
        CellColor color)
    {
        string prefix = NormalizePrefix(idPrefix, "SingleCell");
        string id = BuildId_SingleCell(prefix, role, color);

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        List<LocalPatternCell> cells = new List<LocalPatternCell>
        {
            new LocalPatternCell(
                new CellOffset(0, 0),
                role,
                color)
        };

        PatternFrame frame = new PatternFrame(cells);

        PatternDefinition definition = PatternFactory.CreateSingleFramePattern(
            id,
            frame);

        _cache[id] = definition;
        return definition;
    }

    // =========================================================
    // GRID-SIZED PRIMITIVES (still generic: role/color passed in)
    // =========================================================

    public static PatternDefinition CreateFullGrid(
        string idPrefix,
        int gridWidth,
        int gridHeight,
        CellRole role,
        CellColor color)
    {
        return CreateSolidRect(idPrefix, gridWidth, gridHeight, role, color);
    }

    public static PatternDefinition CreateGridRing(
        string idPrefix,
        int gridWidth,
        int gridHeight,
        int ringWidth,
        CellRole role,
        CellColor color)
    {
        return CreateHollowRect(idPrefix, gridWidth, gridHeight, ringWidth, role, color);
    }

    public static PatternDefinition CreateVerticalBar(
        string idPrefix,
        int gridHeight,
        int barWidth,
        CellRole role,
        CellColor color)
    {
        int h = ClampAtLeast(gridHeight, 1);
        int w = ClampAtLeast(barWidth, 1);

        return CreateSolidRect(idPrefix, w, h, role, color);
    }

    public static PatternDefinition CreateHorizontalBar(
        string idPrefix,
        int gridWidth,
        int barHeight,
        CellRole role,
        CellColor color)
    {
        int w = ClampAtLeast(gridWidth, 1);
        int h = ClampAtLeast(barHeight, 1);

        return CreateSolidRect(idPrefix, w, h, role, color);
    }

    // =========================================================
    // ICONIC / COMPLEX PATTERNS (keep as dedicated methods)
    // =========================================================

    public static PatternDefinition CreateWeaknessPointPattern()
    {
        // Keep as an "iconic" convenience (stable ID, readable usage)
        return CreateSingleCell("Point", CellRole.Weakness, CellColor.Blue);
    }

    public static PatternDefinition CreateSafeCenterWithForbiddenRingPattern()
    {
        const string id = "SafeCenter_ForbiddenRing_3Arms";

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        List<LocalPatternCell> cells = new List<LocalPatternCell>
        {
            new LocalPatternCell(new CellOffset(0, 0), CellRole.Safe, CellColor.Green),
            new LocalPatternCell(new CellOffset(1, 0), CellRole.Forbidden, CellColor.Red),
            new LocalPatternCell(new CellOffset(0, 1), CellRole.Forbidden, CellColor.Red),
            new LocalPatternCell(new CellOffset(-1, 0), CellRole.Forbidden, CellColor.Red),
        };

        PatternFrame frame = new PatternFrame(cells);

        PatternDefinition definition = PatternFactory.CreateSingleFramePattern(
            id,
            frame);

        _cache[id] = definition;
        return definition;
    }

    public static PatternDefinition LavaRipple()
    {
        const string id = "LavaRipple";

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        List<PatternFrame> frames = new List<PatternFrame>();

        List<LocalPatternCell> frame0Cells = new List<LocalPatternCell>
        {
            new LocalPatternCell(new CellOffset(0, 0), CellRole.Forbidden, CellColor.Red)
        };
        frames.Add(new PatternFrame(frame0Cells));

        frames.Add(PatternFactory.CreateRingFrame(1, CellRole.Forbidden, CellColor.Red));
        frames.Add(PatternFactory.CreateRingFrame(2, CellRole.Forbidden, CellColor.Red));
        frames.Add(PatternFactory.CreateRingFrame(3, CellRole.Forbidden, CellColor.Red));

        List<LocalPatternCell> emptyCells = new List<LocalPatternCell>();
        frames.Add(new PatternFrame(emptyCells));

        PatternDefinition definition = new PatternDefinition(id, frames);

        _cache[id] = definition;
        return definition;
    }

    public static PatternDefinition CreateMovingEnemy_4x4_InnerWeakness()
    {
        const string id = "Enemy_4x4_RedRing_BlueCore";

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        int width = 4;
        int height = 4;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isOuter =
                    x == 0 || x == width - 1 ||
                    y == 0 || y == height - 1;

                CellRole role = isOuter ? CellRole.Forbidden : CellRole.Weakness;
                CellColor color = isOuter ? CellColor.Red : CellColor.Blue;

                cells.Add(new LocalPatternCell(new CellOffset(x, y), role, color));
            }
        }

        PatternFrame frame = new PatternFrame(cells);

        PatternDefinition definition = PatternFactory.CreateSingleFramePattern(
            id,
            frame);

        _cache[id] = definition;
        return definition;
    }

    public static PatternDefinition CreateBreathingSquareEnemyPattern(int size)
    {
        int s = size;
        if (s < 3)
        {
            s = 3;
        }

        string id = "BreathingSquareEnemy_" + s + "x" + s;

        // Frame A: full solid square (s x s)
        PatternFrame frameA = PatternFactory.CreateSolidRectFrame(
            s,
            s,
            CellRole.Forbidden,
            CellColor.Red);

        // Frame B: inner solid square (s-2 x s-2) positioned at (+1,+1) to keep same center
        int inner = s - 2;

        List<LocalPatternCell> innerCells = new List<LocalPatternCell>();
        for (int x = 0; x < inner; x++)
        {
            for (int y = 0; y < inner; y++)
            {
                innerCells.Add(new LocalPatternCell(
                    new CellOffset(x + 1, y + 1),
                    CellRole.Forbidden,
                    CellColor.Red));
            }
        }

        PatternFrame frameB = new PatternFrame(innerCells);

        List<PatternFrame> frames = new List<PatternFrame>
        {
            frameA,
            frameB
        };

        PatternDefinition definition = new PatternDefinition(id, frames);
        return definition;
    }

    /// <summary>
    /// A 2-cells-thick forbidden red bar that "rotates" around its center by cycling frames.
    /// - Thickness is exactly 2 cells when perfectly horizontal/vertical.
    /// - Length is roughly preserved between intermediate angles (grid raster approximation).
    /// - FramesPerQuarterTurn controls how many frames exist between 0° (horizontal) and 90° (vertical).
    ///   Total frames returned = 4 * (framesPerQuarterTurn - 1)  (a full 0..360 loop, without duplicate 0/360).
    /// </summary>
    public static PatternDefinition CreateRotatingForbiddenBar(
        int lengthCells,
        int framesPerQuarterTurn)
    {
        int length = ClampAtLeast(lengthCells, 2);
        int fq = ClampAtLeast(framesPerQuarterTurn, 2);

        // Optional sanity cap to avoid accidental huge allocations
        if (fq > 64)
        {
            fq = 64;
        }

        string id = "RotatingForbiddenBar_L" + length + "_FQ" + fq + "_Th2_" + CellRole.Forbidden + "_" + CellColor.Red;

        PatternDefinition cached;
        if (_cache.TryGetValue(id, out cached))
        {
            return cached;
        }

        float stepDeg = 90.0f / (float)(fq - 1);
        int totalFrames = 4 * (fq - 1);

        List<PatternFrame> frames = new List<PatternFrame>(totalFrames);

        for (int f = 0; f < totalFrames; f++)
        {
            float angleDeg = (float)f * stepDeg;
            PatternFrame frame = BuildRotatingBarFrame(
                length,
                angleDeg,
                thicknessCells: 2,
                CellRole.Forbidden,
                CellColor.Red);

            frames.Add(frame);
        }

        PatternDefinition definition = new PatternDefinition(id, frames);
        _cache[id] = definition;
        return definition;
    }

    // =========================================================
    // INTERNAL HELPERS
    // =========================================================

    private static int ClampAtLeast(int value, int min)
    {
        if (value < min)
        {
            return min;
        }

        return value;
    }

    private static string NormalizePrefix(string idPrefix, string fallback)
    {
        if (string.IsNullOrEmpty(idPrefix))
        {
            return fallback;
        }

        return idPrefix;
    }

    private static string BuildId_Rect(string prefix, int width, int height, CellRole role, CellColor color)
    {
        return prefix + "_" + width + "x" + height + "_" + role + "_" + color;
    }

    private static string BuildId_HollowRect(string prefix, int width, int height, int ringWidth, CellRole role, CellColor color)
    {
        return prefix + "_" + width + "x" + height + "_rw" + ringWidth + "_" + role + "_" + color;
    }

    private static string BuildId_SingleCell(string prefix, CellRole role, CellColor color)
    {
        return prefix + "_1x1_" + role + "_" + color;
    }

    private static PatternFrame BuildRotatingBarFrame(
    int lengthCells,
    float angleDegrees,
    int thicknessCells,
    CellRole role,
    CellColor color)
    {
        int length = Mathf.Max(2, lengthCells);
        int thickness = Mathf.Max(1, thicknessCells);

        float rad = angleDegrees * Mathf.Deg2Rad;

        float ux = Mathf.Cos(rad);
        float uy = Mathf.Sin(rad);

        // Unit direction and normal
        Vector2 u = new Vector2(ux, uy);
        Vector2 n = new Vector2(-uy, ux);

        // Keep even thickness symmetric across two columns/rows when axis-aligned.
        // If you only ever use thickness=2, keeping 0.5 is fine.
        float centerCoord = (thickness % 2 == 0) ? 0.5f : 0.0f;
        Vector2 center = new Vector2(centerCoord, centerCoord);

        // Bar extents in "cell-center space" (matches your old length stability)
        float halfLen = 0.5f * (float)(length - 1);
        float halfThick = 0.5f * (float)(thickness - 1);

        // Inflate by the projection of a cell square (half-size 0.5) onto u/n.
        // This is what prevents diagonal thinning and holes.
        float inflateU = 0.5f * (Mathf.Abs(u.x) + Mathf.Abs(u.y));
        float inflateN = 0.5f * (Mathf.Abs(n.x) + Mathf.Abs(n.y));

        float extentU = halfLen + inflateU;
        float extentN = halfThick + inflateN;

        // Conservative world-space AABB for iteration
        float ex = Mathf.Abs(u.x) * extentU + Mathf.Abs(n.x) * extentN;
        float ey = Mathf.Abs(u.y) * extentU + Mathf.Abs(n.y) * extentN;

        int minX = Mathf.FloorToInt(center.x - ex) - 1;
        int maxX = Mathf.CeilToInt(center.x + ex) + 1;
        int minY = Mathf.FloorToInt(center.y - ey) - 1;
        int maxY = Mathf.CeilToInt(center.y + ey) + 1;

        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2((float)x, (float)y);
                Vector2 d = p - center;

                float along = (d.x * u.x) + (d.y * u.y);
                float across = (d.x * n.x) + (d.y * n.y);

                if (Mathf.Abs(along) <= extentU && Mathf.Abs(across) <= extentN)
                {
                    cells.Add(new LocalPatternCell(
                        new CellOffset(x, y),
                        role,
                        color));
                }
            }
        }

        return new PatternFrame(cells);
    }
}
