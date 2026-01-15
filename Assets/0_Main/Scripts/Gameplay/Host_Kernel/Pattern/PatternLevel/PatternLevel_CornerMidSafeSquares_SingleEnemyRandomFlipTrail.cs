using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail : PatternLevelSetupBase
{
    [Header("Safe Squares - Corners")]
    [SerializeField]
    private bool showCornerSafeSquares = true;

    [SerializeField, Min(1)]
    private int cornerSafeSquareSize = 2;

    [Header("Safe Squares - Side Midpoints")]
    [SerializeField]
    private bool showSideMidSafeSquares = true;

    [SerializeField, Min(1)]
    private int sideMidSafeSquareSize = 2;

    [Header("Enemy (Forbidden) - Single Trail Block")]
    [SerializeField, Min(1)]
    private int enemySquareSize = 4;

    [SerializeField]
    private float enemyStepCooldown = 0.25f;

    [Header("Enemy Random Direction Flip")]
    [SerializeField]
    private bool randomizeStartDirection = true;

    [SerializeField]
    private bool randomizeStartIndex = true;

    [SerializeField, Min(0.05f)]
    private float minFlipDurationSeconds = 2.0f;

    [SerializeField, Min(0.05f)]
    private float maxFlipDurationSeconds = 10.0f;

    [Header("Weakness Points")]
    [SerializeField]
    private bool spawnWeakPoints = true;

    [SerializeField]
    private bool spawnWeakPointsInStandby = true;

    // Layers: higher wins
    private const int ForbiddenLayer = 0;
    private const int WeakPointLayer = -100; // keep below Forbidden so enemies + center forbidden still win
    private const int SafeTopLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] Invalid grid size.", this);
            return;
        }

        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        // 1) Safe squares on top layer (corners)
        if (showCornerSafeSquares)
        {
            int s = ClampAtLeast(cornerSafeSquareSize, 1);
            s = Mathf.Min(s, gridWidth, gridHeight);

            PatternDefinition safeDef = PatternPool.CreateSolidRect(
                "SafeSquare_Corner",
                s,
                s,
                CellRole.Safe,
                CellColor.Green);

            List<Vector2Int> origins = PatternLevelHelpers.BuildCornerSquareOrigins(gridWidth, gridHeight, s);

            for (int i = 0; i < origins.Count; i++)
            {
                Vector2Int o = origins[i];
                AddSquareAndBlock(buffer, safeDef, o.x, o.y, s, s, SafeTopLayer, gridWidth, gridHeight, blockedBySafe);
            }
        }

        // 2) Safe squares on top layer (side midpoints)
        if (showSideMidSafeSquares)
        {
            int s = ClampAtLeast(sideMidSafeSquareSize, 1);
            s = Mathf.Min(s, gridWidth, gridHeight);

            PatternDefinition safeDef = PatternPool.CreateSolidRect(
                "SafeSquare_SideMid",
                s,
                s,
                CellRole.Safe,
                CellColor.Green);

            List<Vector2Int> origins = PatternLevelHelpers.BuildSideMidSquareOrigins(gridWidth, gridHeight, s);

            for (int i = 0; i < origins.Count; i++)
            {
                Vector2Int o = origins[i];
                AddSquareAndBlock(buffer, safeDef, o.x, o.y, s, s, SafeTopLayer, gridWidth, gridHeight, blockedBySafe);
            }
        }

        // 3) Single enemy on a rectangle perimeter trail (touching grid boundaries)
        int enemyS = ClampAtLeast(enemySquareSize, 1);
        enemyS = Mathf.Min(enemyS, gridWidth, gridHeight);

        int trailWidth = (gridWidth - enemyS) + 1;
        int trailHeight = (gridHeight - enemyS) + 1;

        if (trailWidth < 2 || trailHeight < 2)
        {
            Debug.LogError(
                "[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] Enemy square too large to form a moving perimeter. " +
                "Grid=" + gridWidth + "x" + gridHeight + ", enemySize=" + enemyS +
                ", trail=" + trailWidth + "x" + trailHeight,
                this);
            return;
        }

        int pathLength = RectPerimeterTrailLogic_ExplicitOrigin.GetPathLength(trailWidth, trailHeight);
        if (pathLength <= 0)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] Invalid perimeter path length.", this);
            return;
        }

        PatternDefinition enemyDef = PatternPool.CreateSolidRect(
            "TrailEnemy_Single",
            enemyS,
            enemyS,
            CellRole.Forbidden,
            CellColor.Red);

        GridIndex trailOrigin = new GridIndex(0, 0);

        int startIndex = 0;
        if (randomizeStartIndex)
        {
            startIndex = UnityEngine.Random.Range(0, pathLength);
        }

        RectPerimeterTrailLogic_RandomDirectionFlip.Direction startDir =
            RectPerimeterTrailLogic_RandomDirectionFlip.Direction.Clockwise;

        if (randomizeStartDirection)
        {
            int r = UnityEngine.Random.Range(0, 2);
            startDir =
                (r == 0)
                    ? RectPerimeterTrailLogic_RandomDirectionFlip.Direction.Clockwise
                    : RectPerimeterTrailLogic_RandomDirectionFlip.Direction.CounterClockwise;
        }

        float minFlip = minFlipDurationSeconds;
        float maxFlip = maxFlipDurationSeconds;

        if (minFlip < 0.05f)
        {
            minFlip = 0.05f;
        }
        if (maxFlip < 0.05f)
        {
            maxFlip = 0.05f;
        }
        if (maxFlip < minFlip)
        {
            float tmp = minFlip;
            minFlip = maxFlip;
            maxFlip = tmp;
        }

        IPatternUpdateLogic logic = new RectPerimeterTrailLogic_RandomDirectionFlip(
            trailOrigin,
            trailWidth,
            trailHeight,
            enemyStepCooldown,
            startIndex,
            startDir,
            minFlip,
            maxFlip);

        CellOffset startOffset = RectPerimeterTrailLogic_ExplicitOrigin.GetOffsetAtIndex(
            trailWidth,
            trailHeight,
            startIndex);

        GridIndex startOrigin = trailOrigin + startOffset;

        PatternInstance enemy = new PatternInstance(
            enemyDef,
            startOrigin,
            ForbiddenLayer,
            logic);

        buffer.Add(enemy);

        // 4) Center forbidden rectangle (exact untouched space)
        CenterRect centerForbidden;
        bool hasCenterForbidden = TryBuildCenterForbiddenRect(gridWidth, gridHeight, enemyS, out centerForbidden);

        if (hasCenterForbidden)
        {
            PatternDefinition innerForbidden = PatternPool.CreateSolidRect(
                "CenterForbidden",
                centerForbidden.Width,
                centerForbidden.Height,
                CellRole.Forbidden,
                CellColor.Red);

            buffer.Add(new PatternInstance(
                innerForbidden,
                new GridIndex(centerForbidden.MinX, centerForbidden.MinY),
                ForbiddenLayer,
                null));
        }

        // 5) Weakness points on ALL cells except safe cells and the center forbidden rect
        if (spawnWeakPoints)
        {
            PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();
            AddWeakPointsEverywhereExceptSafeAndCenterForbidden(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe,
                hasCenterForbidden,
                centerForbidden);
        }
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        if (showCornerSafeSquares)
        {
            int s = ClampAtLeast(cornerSafeSquareSize, 1);
            s = Mathf.Min(s, gridWidth, gridHeight);

            PatternDefinition safeDef = PatternPool.CreateSolidRect(
                "SafeSquare_Corner",
                s,
                s,
                CellRole.Safe,
                CellColor.Green);

            List<Vector2Int> origins = PatternLevelHelpers.BuildCornerSquareOrigins(gridWidth, gridHeight, s);
            for (int i = 0; i < origins.Count; i++)
            {
                Vector2Int o = origins[i];
                AddSquareAndBlock(buffer, safeDef, o.x, o.y, s, s, SafeTopLayer, gridWidth, gridHeight, blockedBySafe);
            }
        }

        if (showSideMidSafeSquares)
        {
            int s = ClampAtLeast(sideMidSafeSquareSize, 1);
            s = Mathf.Min(s, gridWidth, gridHeight);

            PatternDefinition safeDef = PatternPool.CreateSolidRect(
                "SafeSquare_SideMid",
                s,
                s,
                CellRole.Safe,
                CellColor.Green);

            List<Vector2Int> origins = PatternLevelHelpers.BuildSideMidSquareOrigins(gridWidth, gridHeight, s);
            for (int i = 0; i < origins.Count; i++)
            {
                Vector2Int o = origins[i];
                AddSquareAndBlock(buffer, safeDef, o.x, o.y, s, s, SafeTopLayer, gridWidth, gridHeight, blockedBySafe);
            }
        }

        if (spawnWeakPoints && spawnWeakPointsInStandby)
        {
            int enemyS = ClampAtLeast(enemySquareSize, 1);
            enemyS = Mathf.Min(enemyS, gridWidth, gridHeight);

            CenterRect centerForbidden;
            bool hasCenterForbidden = TryBuildCenterForbiddenRect(gridWidth, gridHeight, enemyS, out centerForbidden);

            PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();
            AddWeakPointsEverywhereExceptSafeAndCenterForbidden(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe,
                hasCenterForbidden,
                centerForbidden);
        }
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------

    private struct CenterRect
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;

        public int Width
        {
            get { return (MaxX - MinX) + 1; }
        }

        public int Height
        {
            get { return (MaxY - MinY) + 1; }
        }
    }

    private static bool TryBuildCenterForbiddenRect(
        int gridWidth,
        int gridHeight,
        int enemySize,
        out CenterRect rect)
    {
        rect = new CenterRect();

        int shortSide = Mathf.Min(gridWidth, gridHeight);
        if ((enemySize * 2) >= shortSide)
        {
            return false;
        }

        int innerW = gridWidth - (2 * enemySize);
        int innerH = gridHeight - (2 * enemySize);

        if (innerW <= 0 || innerH <= 0)
        {
            return false;
        }

        rect.MinX = enemySize;
        rect.MinY = enemySize;
        rect.MaxX = enemySize + innerW - 1;
        rect.MaxY = enemySize + innerH - 1;

        return true;
    }

    private static bool IsInsideCenterForbidden(CenterRect rect, int x, int y)
    {
        return x >= rect.MinX && x <= rect.MaxX && y >= rect.MinY && y <= rect.MaxY;
    }

    private static void AddWeakPointsEverywhereExceptSafeAndCenterForbidden(
        List<PatternInstance> buffer,
        PatternDefinition weakPointDef,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe,
        bool hasCenterForbidden,
        CenterRect centerForbidden)
    {
        if (weakPointDef == null)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] Weak point definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_CornerMidSafeSquares_SingleEnemyRandomFlipTrail] blockedBySafe is null.");
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

                if (hasCenterForbidden && IsInsideCenterForbidden(centerForbidden, x, y))
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

    private static int ClampAtLeast(int value, int min)
    {
        if (value < min)
        {
            return min;
        }

        return value;
    }

    private static void AddSquareAndBlock(
        List<PatternInstance> buffer,
        PatternDefinition def,
        int originX,
        int originY,
        int width,
        int height,
        int layer,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blocked)
    {
        buffer.Add(new PatternInstance(def, new GridIndex(originX, originY), layer, null));

        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int x = originX + dx;
                int y = originY + dy;

                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    continue;
                }

                blocked.Add(new Vector2Int(x, y));
            }
        }
    }
}
