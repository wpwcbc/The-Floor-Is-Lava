using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_NineSafeSquares_DualBars_Weakness : PatternLevelSetupBase
{
    private const int SafeSquareSize = 2;

    [Header("Bars")]
    [SerializeField]
    private float barStepCooldown = 0.25f;

    [SerializeField, Range(1, 6)]
    private int barThickness = 2;

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

    [Header("Vertical Bars (move left/right)")]
    [SerializeField]
    private HorizontalStartDirection verticalBarAStart = HorizontalStartDirection.Left;

    [SerializeField]
    private HorizontalStartDirection verticalBarBStart = HorizontalStartDirection.Right;

    [Header("Horizontal Bars (move up/down)")]
    [SerializeField]
    private VerticalStartDirection horizontalBarAStart = VerticalStartDirection.Up;

    [SerializeField]
    private VerticalStartDirection horizontalBarBStart = VerticalStartDirection.Down;

    [SerializeField, Min(0f)]
    private float weaknessDensityPer144Cells = 8.0f;

    [SerializeField, Min(1)]
    private int weaknessDensityReferenceCells = 144; // keep 144 default, but not hard-coded

    [SerializeField]
    private bool distributeEvenlyAcrossFourAreas = true;

    [SerializeField, Min(1)]
    private int weaknessSpawnAttemptsPerPoint = 20;

    // Layer rule assumed: higher layer wins.
    private const int BarsLayer = 0;
    private const int WeaknessLayer = -100;
    private const int SafeLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_NineSafeSquares_DualBars_Weakness] buffer is null.", this);
            return;
        }

        if (gridWidth < 2 || gridHeight < 2)
        {
            Debug.LogError(
                "[PatternLevel_NineSafeSquares_DualBars_Weakness] Grid too small for 2x2 safe squares. Grid: " +
                gridWidth + "x" + gridHeight,
                this);
            return;
        }

        int thickness = barThickness;
        if (thickness < 1)
        {
            thickness = 1;
        }

        if (thickness > gridWidth)
        {
            thickness = gridWidth;
        }

        if (thickness > gridHeight)
        {
            thickness = gridHeight;
        }

        // NEW API: primitives with role/color passed in
        PatternDefinition safe2x2 = PatternPool.CreateSolidRect(
            "SafeSquare",
            SafeSquareSize,
            SafeSquareSize,
            CellRole.Safe,
            CellColor.Green);

        PatternDefinition verticalBar = PatternPool.CreateVerticalBar(
            "VerticalLavaBar",
            gridHeight,
            thickness,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition horizontalBar = PatternPool.CreateHorizontalBar(
            "HorizontalLavaBar",
            gridWidth,
            thickness,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition weaknessPoint = PatternPool.CreateWeaknessPointPattern();

        // 1) Place 9 safe squares
        List<Vector2Int> safeOrigins = BuildNineSafeSquareOrigins(gridWidth, gridHeight);

        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        for (int i = 0; i < safeOrigins.Count; i++)
        {
            Vector2Int o = safeOrigins[i];

            PatternInstance safeInstance = new PatternInstance(
                safe2x2,
                new GridIndex(o.x, o.y),
                SafeLayer,
                null);

            buffer.Add(safeInstance);

            for (int dx = 0; dx < SafeSquareSize; dx++)
            {
                for (int dy = 0; dy < SafeSquareSize; dy++)
                {
                    int cx = o.x + dx;
                    int cy = o.y + dy;

                    if (cx < 0 || cx >= gridWidth || cy < 0 || cy >= gridHeight)
                    {
                        continue;
                    }

                    blockedBySafe.Add(new Vector2Int(cx, cy));
                }
            }
        }

        // 2) Two vertical bars (move left/right), start centered X
        int minBarOriginX = 0;
        int maxBarOriginX = gridWidth - thickness;

        if (maxBarOriginX < minBarOriginX)
        {
            Debug.LogError(
                "[PatternLevel_NineSafeSquares_DualBars_Weakness] Vertical bar thickness larger than grid width. " +
                "Grid: " + gridWidth + "x" + gridHeight + ", thickness: " + thickness,
                this);
            return;
        }

        int startBarOriginX = (minBarOriginX + maxBarOriginX) / 2;

        IPatternUpdateLogic vBarLogicA = new HorizontalBounceLogic(
            minBarOriginX,
            maxBarOriginX,
            barStepCooldown,
            (int)verticalBarAStart);

        IPatternUpdateLogic vBarLogicB = new HorizontalBounceLogic(
            minBarOriginX,
            maxBarOriginX,
            barStepCooldown,
            (int)verticalBarBStart);

        buffer.Add(new PatternInstance(
            verticalBar,
            new GridIndex(startBarOriginX, 0),
            BarsLayer,
            vBarLogicA));

        buffer.Add(new PatternInstance(
            verticalBar,
            new GridIndex(startBarOriginX, 0),
            BarsLayer,
            vBarLogicB));

        // 3) Two horizontal bars (move up/down), start centered Y
        int minBarOriginY = 0;
        int maxBarOriginY = gridHeight - thickness;

        if (maxBarOriginY < minBarOriginY)
        {
            Debug.LogError(
                "[PatternLevel_NineSafeSquares_DualBars_Weakness] Horizontal bar thickness larger than grid height. " +
                "Grid: " + gridWidth + "x" + gridHeight + ", thickness: " + thickness,
                this);
            return;
        }

        int startBarOriginY = (minBarOriginY + maxBarOriginY) / 2;

        IPatternUpdateLogic hBarLogicA = new VerticalBounceLogic(
            minBarOriginY,
            maxBarOriginY,
            barStepCooldown,
            (int)horizontalBarAStart);

        IPatternUpdateLogic hBarLogicB = new VerticalBounceLogic(
            minBarOriginY,
            maxBarOriginY,
            barStepCooldown,
            (int)horizontalBarBStart);

        buffer.Add(new PatternInstance(
            horizontalBar,
            new GridIndex(0, startBarOriginY),
            BarsLayer,
            hBarLogicA));

        buffer.Add(new PatternInstance(
            horizontalBar,
            new GridIndex(0, startBarOriginY),
            BarsLayer,
            hBarLogicB));

        int weaknessCount = ComputeCountFromDensity(
            gridWidth,
            gridHeight,
            weaknessDensityPer144Cells,
            weaknessDensityReferenceCells);


        // 4) Weakness points (<= 10), symmetric, responsive, not overlapping safe squares
        AddWeaknessPointsInFourAreas(
            buffer,
            weaknessPoint,
            gridWidth,
            gridHeight,
            blockedBySafe,
            weaknessCount,
            distributeEvenlyAcrossFourAreas,
            weaknessSpawnAttemptsPerPoint
        );

    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_NineSafeSquares_DualBars_Weakness] standby buffer is null.", this);
            return;
        }

        if (gridWidth < 2 || gridHeight < 2)
        {
            return;
        }

        PatternDefinition safe2x2 = PatternPool.CreateSolidRect(
            "SafeSquare",
            SafeSquareSize,
            SafeSquareSize,
            CellRole.Safe,
            CellColor.Green);

        List<Vector2Int> safeOrigins = BuildNineSafeSquareOrigins(gridWidth, gridHeight);

        for (int i = 0; i < safeOrigins.Count; i++)
        {
            Vector2Int o = safeOrigins[i];

            buffer.Add(new PatternInstance(
                safe2x2,
                new GridIndex(o.x, o.y),
                SafeLayer,
                null));
        }
    }

    private static List<Vector2Int> BuildNineSafeSquareOrigins(int gridWidth, int gridHeight)
    {
        int maxOriginX = gridWidth - SafeSquareSize;
        int maxOriginY = gridHeight - SafeSquareSize;

        int midOriginX = Mathf.Clamp((gridWidth / 2) - 1, 0, maxOriginX);
        int midOriginY = Mathf.Clamp((gridHeight / 2) - 1, 0, maxOriginY);

        List<Vector2Int> origins = new List<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, maxOriginY),
            new Vector2Int(maxOriginX, 0),
            new Vector2Int(maxOriginX, maxOriginY),

            new Vector2Int(midOriginX, midOriginY),

            new Vector2Int(0, midOriginY),
            new Vector2Int(maxOriginX, midOriginY),
            new Vector2Int(midOriginX, 0),
            new Vector2Int(midOriginX, maxOriginY),
        };

        HashSet<Vector2Int> unique = new HashSet<Vector2Int>();
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int o = origins[i];
            if (unique.Add(o))
            {
                result.Add(o);
            }
        }

        return result;
    }

    private struct IntRect
    {
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;

        public IntRect(int minX, int maxX, int minY, int maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public bool IsValid
        {
            get { return MinX <= MaxX && MinY <= MaxY; }
        }

        public int Area
        {
            get
            {
                if (!IsValid)
                {
                    return 0;
                }

                int w = (MaxX - MinX) + 1;
                int h = (MaxY - MinY) + 1;
                return w * h;
            }
        }
    }

    private static void AddWeaknessPointsInFourAreas(
        List<PatternInstance> buffer,
        PatternDefinition weaknessPointDefinition,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe,
        int totalCount,
        bool distributeEvenly,
        int attemptsPerPoint)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_NineSafeSquares_DualBars_Weakness] Weakness buffer is null.");
            return;
        }

        if (weaknessPointDefinition == null)
        {
            Debug.LogError("[PatternLevel_NineSafeSquares_DualBars_Weakness] Weakness definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_NineSafeSquares_DualBars_Weakness] blockedBySafe is null.");
            return;
        }

        int count = totalCount;
        if (count <= 0)
        {
            return;
        }

        int tries = Mathf.Max(1, attemptsPerPoint);

        // Same center origin logic as BuildNineSafeSquareOrigins (keep consistent)
        int maxOriginX = gridWidth - SafeSquareSize;
        int maxOriginY = gridHeight - SafeSquareSize;

        int midOriginX = Mathf.Clamp((gridWidth / 2) - 1, 0, maxOriginX);
        int midOriginY = Mathf.Clamp((gridHeight / 2) - 1, 0, maxOriginY);

        // Define quadrant bounds EXCLUDING the center safe square footprint:
        // Left  = [0 .. midOriginX-1]
        // Right = [midOriginX+SafeSquareSize .. gridWidth-1]
        // Bottom= [0 .. midOriginY-1]
        // Top   = [midOriginY+SafeSquareSize .. gridHeight-1]
        int leftMaxX = midOriginX - 1;
        int rightMinX = midOriginX + SafeSquareSize;

        int bottomMaxY = midOriginY - 1;
        int topMinY = midOriginY + SafeSquareSize;

        List<IntRect> regions = new List<IntRect>();

        // BL
        regions.Add(new IntRect(0, leftMaxX, 0, bottomMaxY));
        // BR
        regions.Add(new IntRect(rightMinX, gridWidth - 1, 0, bottomMaxY));
        // TL
        regions.Add(new IntRect(0, leftMaxX, topMinY, gridHeight - 1));
        // TR
        regions.Add(new IntRect(rightMinX, gridWidth - 1, topMinY, gridHeight - 1));

        // Filter invalid/empty regions
        List<IntRect> validRegions = new List<IntRect>();
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].IsValid && regions[i].Area > 0)
            {
                validRegions.Add(regions[i]);
            }
        }

        if (validRegions.Count == 0)
        {
            Debug.LogWarning(
                "[PatternLevel_NineSafeSquares_DualBars_Weakness] No valid quadrants available for weakness placement.");
            return;
        }

        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        int added = 0;

        if (distributeEvenly)
        {
            int basePerRegion = count / validRegions.Count;
            int remainder = count - (basePerRegion * validRegions.Count);

            int[] alloc = new int[validRegions.Count];
            for (int i = 0; i < alloc.Length; i++)
            {
                alloc[i] = basePerRegion;
            }

            // Give remainder to larger regions first (more likely to succeed)
            List<int> order = BuildRegionIndexOrderByAreaDesc(validRegions);
            for (int r = 0; r < remainder; r++)
            {
                int idx = order[r % order.Count];
                alloc[idx]++;
            }

            for (int i = 0; i < validRegions.Count; i++)
            {
                IntRect rect = validRegions[i];
                for (int k = 0; k < alloc[i]; k++)
                {
                    Vector2Int p;
                    if (!TryPickFreePointInRect(rect, gridWidth, gridHeight, blockedBySafe, used, tries, out p))
                    {
                        continue;
                    }

                    used.Add(p);

                    buffer.Add(new PatternInstance(
                        weaknessPointDefinition,
                        new GridIndex(p.x, p.y),
                        WeaknessLayer,
                        null));

                    added++;
                }
            }
        }
        else
        {
            // Weighted random by region area
            for (int i = 0; i < count; i++)
            {
                int idx = PickWeightedRegionIndex(validRegions);
                IntRect rect = validRegions[idx];

                Vector2Int p;
                if (!TryPickFreePointInRect(rect, gridWidth, gridHeight, blockedBySafe, used, tries, out p))
                {
                    continue;
                }

                used.Add(p);

                buffer.Add(new PatternInstance(
                    weaknessPointDefinition,
                    new GridIndex(p.x, p.y),
                    WeaknessLayer,
                    null));

                added++;
            }
        }

        if (added < count)
        {
            Debug.LogWarning(
                "[PatternLevel_NineSafeSquares_DualBars_Weakness] Requested " + count +
                " weakness points, but only placed " + added + " (likely due to small grid / safe blocks).");
        }
    }

    private static List<int> BuildRegionIndexOrderByAreaDesc(List<IntRect> rects)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < rects.Count; i++)
        {
            indices.Add(i);
        }

        indices.Sort(delegate (int a, int b)
        {
            int areaA = rects[a].Area;
            int areaB = rects[b].Area;
            return areaB.CompareTo(areaA);
        });

        return indices;
    }

    private static int PickWeightedRegionIndex(List<IntRect> rects)
    {
        int total = 0;
        for (int i = 0; i < rects.Count; i++)
        {
            total += Mathf.Max(0, rects[i].Area);
        }

        if (total <= 0)
        {
            return 0;
        }

        int roll = UnityEngine.Random.Range(0, total);

        int accum = 0;
        for (int i = 0; i < rects.Count; i++)
        {
            int a = Mathf.Max(0, rects[i].Area);
            accum += a;
            if (roll < accum)
            {
                return i;
            }
        }

        return rects.Count - 1;
    }

    private static bool TryPickFreePointInRect(
        IntRect rect,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe,
        HashSet<Vector2Int> used,
        int attempts,
        out Vector2Int point)
    {
        point = Vector2Int.zero;

        // Random attempts
        for (int i = 0; i < attempts; i++)
        {
            int x = UnityEngine.Random.Range(rect.MinX, rect.MaxX + 1);
            int y = UnityEngine.Random.Range(rect.MinY, rect.MaxY + 1);

            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                continue;
            }

            Vector2Int p = new Vector2Int(x, y);

            if (blockedBySafe.Contains(p))
            {
                continue;
            }

            if (used.Contains(p))
            {
                continue;
            }

            point = p;
            return true;
        }

        // Deterministic fallback scan (if random failed but space exists)
        for (int x = rect.MinX; x <= rect.MaxX; x++)
        {
            for (int y = rect.MinY; y <= rect.MaxY; y++)
            {
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    continue;
                }

                Vector2Int p = new Vector2Int(x, y);

                if (blockedBySafe.Contains(p))
                {
                    continue;
                }

                if (used.Contains(p))
                {
                    continue;
                }

                point = p;
                return true;
            }
        }

        return false;
    }

    private static int ComputeCountFromDensity(
    int gridWidth,
    int gridHeight,
    float densityPerRefCells,
    int referenceCells)
    {
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return 0;
        }

        int refCells = Mathf.Max(1, referenceCells);

        int area = gridWidth * gridHeight;

        float expected = (densityPerRefCells * (float)area) / (float)refCells;

        int count;
        count = Mathf.RoundToInt(expected);

        return count;
    }
}
