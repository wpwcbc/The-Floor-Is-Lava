using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_SafeRingWithDiagonalEnemies : PatternLevelSetupBase
{
    [Header("Ring")]
    [SerializeField]
    private int ringWidth = 3;

    [Header("Enemies")]
    [SerializeField]
    private float enemyStepCooldown = 0.25f;

    [SerializeField, Range(0, 6)]
    private int enemyCount = 6;

    // Enemy footprint (matches pattern)
    private const int EnemyWidth = 4;
    private const int EnemyHeight = 4;

    private const int EnemyLayer = 0;
    private const int RingLayer = 1000;

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

    private int GetClampedEnemyCountLocal()
    {
        int count = enemyCount;
        if (count < 0)
        {
            count = 0;
        }
        else if (count > 6)
        {
            count = 6;
        }

        return count;
    }

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SafeRingWithDiagonalEnemies] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError(
                "[PatternLevel_SafeRingWithDiagonalEnemies] Invalid grid size: " +
                gridWidth + "x" + gridHeight,
                this);
            return;
        }

        int rw = GetClampedRingWidthLocal(gridWidth, gridHeight);
        int spawnCount = GetClampedEnemyCountLocal();

        // 1) Safe ring (new API: generic grid ring with role/color)
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

        if (spawnCount <= 0)
        {
            return;
        }

        // 2) Enemy pattern (iconic/complex stays as-is)
        PatternDefinition enemyDefinition = PatternPool.CreateMovingEnemy_4x4_InnerWeakness();

        int innerMinX = rw;
        int innerMinY = rw;

        int innerMaxX = gridWidth - rw - 1;
        int innerMaxY = gridHeight - rw - 1;

        int minOriginX = innerMinX;
        int minOriginY = innerMinY;

        int maxOriginX = innerMaxX - (EnemyWidth - 1);
        int maxOriginY = innerMaxY - (EnemyHeight - 1);

        if (maxOriginX < minOriginX || maxOriginY < minOriginY)
        {
            Debug.LogError(
                "[PatternLevel_SafeRingWithDiagonalEnemies] Ring interior too small for 4x4 enemies. " +
                "Grid: " + gridWidth + "x" + gridHeight + ", ringWidth: " + rw,
                this);
            return;
        }

        // 3) Candidate spawn positions (up to 6)
        int spanX = maxOriginX - minOriginX;

        int xA = minOriginX;
        int xB = minOriginX + spanX / 2;
        int xC = maxOriginX;

        int yLow = minOriginY;
        int yHigh = maxOriginY;

        List<Vector2Int> starts = new List<Vector2Int>
        {
            new Vector2Int(xA, yLow),
            new Vector2Int(xB, yLow),
            new Vector2Int(xC, yLow),
            new Vector2Int(xA, yHigh),
            new Vector2Int(xB, yHigh),
            new Vector2Int(xC, yHigh),
        };

        List<Vector2Int> dirs = new List<Vector2Int>
        {
            new Vector2Int( 1,  1),
            new Vector2Int( 1, -1),
            new Vector2Int(-1,  1),
            new Vector2Int(-1, -1),
            new Vector2Int( 1,  1),
            new Vector2Int(-1, -1),
        };

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2Int start = starts[i];
            Vector2Int dir = dirs[i];

            IPatternUpdateLogic logic = new DiagonalBounceLogic(
                minOriginX,
                maxOriginX,
                minOriginY,
                maxOriginY,
                enemyStepCooldown,
                dir.x,
                dir.y);

            buffer.Add(new PatternInstance(
                enemyDefinition,
                new GridIndex(start.x, start.y),
                EnemyLayer,
                logic));
        }
    }

    protected override void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_SafeRingWithDiagonalEnemies] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
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
