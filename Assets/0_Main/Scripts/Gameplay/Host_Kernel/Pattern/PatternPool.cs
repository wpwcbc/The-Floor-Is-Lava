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
                samplesPerCell: 2,
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
        int samplesPerCell,
        CellRole role,
        CellColor color)
    {
        int length = ClampAtLeast(lengthCells, 2);

        // This implementation is intentionally centered at (0.5, 0.5) in "cell-center space".
        // That makes a 2-cell thickness land exactly on two rows/cols when axis-aligned.
        Vector2 center = new Vector2(0.5f, 0.5f);

        float rad = angleDegrees * Mathf.Deg2Rad;
        float ux = Mathf.Cos(rad);
        float uy = Mathf.Sin(rad);

        // Perpendicular (normal)
        float nx = -uy;
        float ny = ux;

        // Two-cell thickness -> sample two parallel centerlines separated by 1 cell.
        // Offsets are ±0.5 along the normal.
        float halfThicknessOffset = 0.5f;

        // Keep the "length" stable: we march along the bar in cell units.
        float halfLen = 0.5f * (float)(length - 1);

        int spc = ClampAtLeast(samplesPerCell, 1);
        float step = 1.0f / (float)spc;

        HashSet<Vector2Int> unique = new HashSet<Vector2Int>();

        // March along the centerline segment, oversampling slightly to reduce holes.
        // NOTE: the +0.0001f is to ensure the last sample is included.
        for (float t = -halfLen; t <= halfLen + 0.0001f; t += step)
        {
            float px = center.x + (t * ux);
            float py = center.y + (t * uy);

            // Two parallel lines (thickness = 2 when axis-aligned)
            float ax = px + (nx * halfThicknessOffset);
            float ay = py + (ny * halfThicknessOffset);

            float bx = px - (nx * halfThicknessOffset);
            float by = py - (ny * halfThicknessOffset);

            int xA = Mathf.RoundToInt(ax);
            int yA = Mathf.RoundToInt(ay);

            int xB = Mathf.RoundToInt(bx);
            int yB = Mathf.RoundToInt(by);

            unique.Add(new Vector2Int(xA, yA));
            unique.Add(new Vector2Int(xB, yB));
        }

        // Convert to PatternFrame cells
        List<LocalPatternCell> cells = new List<LocalPatternCell>(unique.Count);

        foreach (Vector2Int p in unique)
        {
            cells.Add(new LocalPatternCell(
                new CellOffset(p.x, p.y),
                role,
                color));
        }

        return new PatternFrame(cells);
    }
}
