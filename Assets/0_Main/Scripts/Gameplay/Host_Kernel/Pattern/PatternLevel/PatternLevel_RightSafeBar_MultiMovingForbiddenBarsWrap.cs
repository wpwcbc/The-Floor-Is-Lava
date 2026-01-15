using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap : PatternLevelSetupBase
{
    [Header("Right Safe Bar (Green)")]
    [SerializeField, Min(1)]
    private int safeBarWidth = 2;

    [Header("Forbidden Bars (Red)")]
    [SerializeField, Min(1)]
    private int forbiddenBarWidth = 2;

    [SerializeField, Min(1)]
    private int forbiddenBarCount = 3;

    [Tooltip("Gap (empty columns) between forbidden bars.")]
    [SerializeField, Min(0)]
    private int forbiddenBarGapCells = 2;

    [SerializeField, Min(0.0f)]
    private float forbiddenBarStepCooldownSeconds = 0.25f;

    [Header("Weakness Fill (Blue)")]
    [SerializeField]
    private bool fillWeakPoints = true;

    // Layers: higher wins
    private const int ForbiddenLayer = 0;
    private const int WeakPointLayer = -100; // under forbidden bars
    private const int SafeLayer = 1000;

    protected override void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] Invalid grid size.", this);
            return;
        }

        int safeW = Mathf.Max(1, safeBarWidth);
        if (safeW > gridWidth)
        {
            Debug.LogWarning(
                "[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] safeBarWidth > gridWidth, clamping. " +
                "safeW=" + safeW + ", gridWidth=" + gridWidth,
                this);
            safeW = gridWidth;
        }

        int barW = Mathf.Max(1, forbiddenBarWidth);
        if (barW > gridWidth)
        {
            Debug.LogWarning(
                "[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] forbiddenBarWidth > gridWidth, clamping. " +
                "barW=" + barW + ", gridWidth=" + gridWidth,
                this);
            barW = gridWidth;
        }

        int count = Mathf.Max(1, forbiddenBarCount);
        int gap = Mathf.Max(0, forbiddenBarGapCells);
        float cd = Mathf.Max(0.0f, forbiddenBarStepCooldownSeconds);

        // --- Definitions ---
        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "RightSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        PatternDefinition forbiddenBarDef = PatternPool.CreateVerticalBar(
            "MovingForbiddenBar",
            gridHeight,
            barW,
            CellRole.Forbidden,
            CellColor.Red);

        PatternDefinition weakPointDef = PatternPool.CreateWeaknessPointPattern();

        // Track safe cells so weakness doesn't overlap them.
        HashSet<Vector2Int> blockedBySafe = new HashSet<Vector2Int>();

        // 1) Right safe bar
        int safeOriginX = gridWidth - safeW;
        if (safeOriginX < 0)
        {
            safeOriginX = 0;
        }

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));

        for (int x = safeOriginX; x < safeOriginX + safeW; x++)
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

        // 2) Weakness fill (NO standby spawn in new levels)
        if (fillWeakPoints)
        {
            AddWeakPointsEverywhereExceptSafe(
                buffer,
                weakPointDef,
                gridWidth,
                gridHeight,
                blockedBySafe);
        }

        // 3) One or multiple vertical forbidden bars moving RIGHT, axis-wrapped, fixed spacing.
        int stepBetweenBars = barW + gap;

        // We want a "train" of bars entering from the left.
        // Bar 0 starts at x=0, next ones start further left (negative) to preserve spacing.
        // Using AxisWrapLogic_Continuous below keeps spacing stable across wraps.
        int minOffsetX = 0;
        int maxOffsetX = barW - 1;

        for (int i = 0; i < count; i++)
        {
            int originX = 0 - (i * stepBetweenBars);

            IPatternUpdateLogic logic = new AxisWrapLogic_Continuous(
                AxisWrapLogic_Continuous.Axis.Horizontal,
                gridWidth,
                cd,
                direction: +1,
                minOffset: minOffsetX,
                maxOffset: maxOffsetX);

            buffer.Add(new PatternInstance(
                forbiddenBarDef,
                new GridIndex(originX, 0),
                ForbiddenLayer,
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
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] standby buffer is null.", this);
            return;
        }

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        int safeW = Mathf.Max(1, safeBarWidth);
        if (safeW > gridWidth)
        {
            safeW = gridWidth;
        }

        PatternDefinition safeBarDef = PatternPool.CreateVerticalBar(
            "RightSafeBar",
            gridHeight,
            safeW,
            CellRole.Safe,
            CellColor.Green);

        int safeOriginX = gridWidth - safeW;
        if (safeOriginX < 0)
        {
            safeOriginX = 0;
        }

        buffer.Add(new PatternInstance(
            safeBarDef,
            new GridIndex(safeOriginX, 0),
            SafeLayer,
            null));
    }

    private static void AddWeakPointsEverywhereExceptSafe(
        List<PatternInstance> buffer,
        PatternDefinition weakPointDef,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> blockedBySafe)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] Weakness buffer is null.");
            return;
        }

        if (weakPointDef == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] Weak point definition is null.");
            return;
        }

        if (blockedBySafe == null)
        {
            Debug.LogError("[PatternLevel_RightSafeBar_MultiMovingForbiddenBarsWrap] blockedBySafe is null.");
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

    // ---------------------------------------------------------------------
    // Wrap logic that preserves spacing across multiple instances.
    // (AxisWrapLogic you pasted snaps all instances to the same wrap origin,
    //  which destroys fixed spacing once they wrap.)
    // ---------------------------------------------------------------------
    private sealed class AxisWrapLogic_Continuous : IPatternUpdateLogic
    {
        public enum Axis
        {
            Horizontal,
            Vertical
        }

        private readonly Axis _axis;
        private readonly int _gridSize;
        private readonly float _stepCooldown;
        private readonly int _direction;

        private readonly int _minOffset;
        private readonly int _maxOffset;
        private readonly int _patternSpan;

        private float _timer;
        private bool _loggedNull;

        public AxisWrapLogic_Continuous(
            Axis axis,
            int gridSize,
            float stepCooldown,
            int direction,
            int minOffset,
            int maxOffset)
        {
            _axis = axis;

            int size = gridSize;
            if (size < 1)
            {
                size = 1;
            }

            _gridSize = size;
            _stepCooldown = Mathf.Max(0.0f, stepCooldown);
            _direction = direction >= 0 ? 1 : -1;

            if (minOffset <= maxOffset)
            {
                _minOffset = minOffset;
                _maxOffset = maxOffset;
            }
            else
            {
                _minOffset = maxOffset;
                _maxOffset = minOffset;
            }

            _patternSpan = (_maxOffset - _minOffset) + 1;

            _timer = 0.0f;
            _loggedNull = false;
        }

        public void Tick(PatternInstance instance, float deltaTime)
        {
            if (instance == null)
            {
                if (!_loggedNull)
                {
                    Debug.LogError("[AxisWrapLogic_Continuous] Tick received null PatternInstance.");
                    _loggedNull = true;
                }
                return;
            }

            if (_stepCooldown <= 0.0f)
            {
                return;
            }

            _timer += deltaTime;

            int safetySteps = 0;
            while (_timer >= _stepCooldown && safetySteps < 32)
            {
                _timer -= _stepCooldown;
                StepOnce(instance);
                safetySteps++;
            }
        }

        private void StepOnce(PatternInstance instance)
        {
            GridIndex origin = instance.Origin;

            if (_axis == Axis.Horizontal)
            {
                int nextX = origin.X + _direction;

                int leftMost = nextX + _minOffset;
                int rightMost = nextX + _maxOffset;

                int wrapSpan = _gridSize + _patternSpan;

                if (_direction > 0)
                {
                    if (leftMost >= _gridSize)
                    {
                        nextX -= wrapSpan;
                    }
                }
                else
                {
                    if (rightMost < 0)
                    {
                        nextX += wrapSpan;
                    }
                }

                instance.SetOrigin(new GridIndex(nextX, origin.Y));
            }
            else
            {
                int nextY = origin.Y + _direction;

                int bottomMost = nextY + _minOffset;
                int topMost = nextY + _maxOffset;

                int wrapSpan = _gridSize + _patternSpan;

                if (_direction > 0)
                {
                    if (bottomMost >= _gridSize)
                    {
                        nextY -= wrapSpan;
                    }
                }
                else
                {
                    if (topMost < 0)
                    {
                        nextY += wrapSpan;
                    }
                }

                instance.SetOrigin(new GridIndex(origin.X, nextY));
            }
        }
    }
}
