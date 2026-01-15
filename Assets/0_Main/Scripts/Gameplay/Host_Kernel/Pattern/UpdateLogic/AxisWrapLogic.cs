using UnityEngine;

public sealed class AxisWrapLogic : IPatternUpdateLogic
{
    public enum Axis
    {
        Horizontal,
        Vertical
    }

    private readonly Axis _axis;
    private readonly int _gridSize;
    private readonly float _stepCooldown;

    // Direction: +1 or -1, never flips.
    private readonly int _direction;

    // Pattern extents along the moving axis.
    // If Horizontal: offsets in X, if Vertical: offsets in Y.
    private readonly int _minOffset;
    private readonly int _maxOffset;

    private float _timer;
    private bool _loggedNull;

    /// <summary>
    /// gridSize: gridWidth if Horizontal, gridHeight if Vertical.
    /// minOffset/maxOffset: pattern offset extents along the moving axis (can be negative).
    /// direction: +1 or -1.
    /// </summary>
    public AxisWrapLogic(
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

        // Normalize extents just in case
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

        _timer = 0.0f;
        _loggedNull = false;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            if (!_loggedNull)
            {
                Debug.LogError("[AxisWrapLogic] Tick received null PatternInstance.");
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
        while (_timer >= _stepCooldown && safetySteps < 16)
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
            instance.SetOrigin(new GridIndex(nextX, origin.Y));

            int leftMost = nextX + _minOffset;
            int rightMost = nextX + _maxOffset;

            // Moving right: once entire pattern is beyond right edge -> wrap to left outside
            if (_direction > 0)
            {
                if (leftMost >= _gridSize)
                {
                    int wrapOriginX = -_maxOffset - 1; // fully outside left
                    instance.SetOrigin(new GridIndex(wrapOriginX, origin.Y));
                }
            }
            // Moving left: once entire pattern is beyond left edge -> wrap to right outside
            else
            {
                if (rightMost < 0)
                {
                    int wrapOriginX = _gridSize - _minOffset; // fully outside right
                    instance.SetOrigin(new GridIndex(wrapOriginX, origin.Y));
                }
            }
        }
        else
        {
            int nextY = origin.Y + _direction;
            instance.SetOrigin(new GridIndex(origin.X, nextY));

            int bottomMost = nextY + _minOffset;
            int topMost = nextY + _maxOffset;

            // Moving up
            if (_direction > 0)
            {
                if (bottomMost >= _gridSize)
                {
                    int wrapOriginY = -_maxOffset - 1; // fully outside bottom
                    instance.SetOrigin(new GridIndex(origin.X, wrapOriginY));
                }
            }
            // Moving down
            else
            {
                if (topMost < 0)
                {
                    int wrapOriginY = _gridSize - _minOffset; // fully outside top
                    instance.SetOrigin(new GridIndex(origin.X, wrapOriginY));
                }
            }
        }
    }
}
