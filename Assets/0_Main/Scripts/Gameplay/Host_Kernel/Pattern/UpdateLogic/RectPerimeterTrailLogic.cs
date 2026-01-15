using UnityEngine;

public sealed class RectPerimeterTrailLogic : IPatternUpdateLogic
{
    public enum Direction
    {
        Clockwise = 1,
        CounterClockwise = -1
    }

    private readonly int _trailWidth;
    private readonly int _trailHeight;
    private readonly float _stepCooldown;
    private readonly int _direction;
    private readonly int _startIndex;

    private bool _initialized;
    private float _timer;

    private GridIndex _trailOrigin;
    private int _pathLength;
    private int _index;

    private bool _loggedBadTrail;
    private bool _loggedNullInstance;

    public RectPerimeterTrailLogic(
        int trailWidth,
        int trailHeight,
        float stepCooldownSeconds,
        int startIndex,
        Direction direction)
    {
        _trailWidth = trailWidth;
        _trailHeight = trailHeight;

        if (stepCooldownSeconds < 0.0f)
        {
            stepCooldownSeconds = 0.0f;
        }

        _stepCooldown = stepCooldownSeconds;

        _startIndex = startIndex;
        _direction = (int)direction;

        _initialized = false;
        _timer = 0.0f;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            if (!_loggedNullInstance)
            {
                _loggedNullInstance = true;
                Debug.LogError("[RectPerimeterTrailLogic] Tick called with null PatternInstance.");
            }
            return;
        }

        if (!_initialized)
        {
            _initialized = true;

            if (_trailWidth < 2 || _trailHeight < 2)
            {
                if (!_loggedBadTrail)
                {
                    _loggedBadTrail = true;
                    Debug.LogError(
                        "[RectPerimeterTrailLogic] trailWidth and trailHeight must be >= 2 for a perimeter loop. " +
                        "Got " + _trailWidth + "x" + _trailHeight);
                }
                return;
            }

            _trailOrigin = instance.Origin;
            _pathLength = (2 * (_trailWidth + _trailHeight)) - 4;

            _index = Mod(_startIndex, _pathLength);

            CellOffset startOffset = GetOffsetAtIndex(_index);
            instance.SetOrigin(_trailOrigin + startOffset);

            return;
        }

        if (_pathLength <= 0)
        {
            return;
        }

        if (_stepCooldown <= 0.0f)
        {
            Advance(instance);
            return;
        }

        _timer += deltaTime;

        while (_timer >= _stepCooldown)
        {
            _timer -= _stepCooldown;
            Advance(instance);
        }
    }

    private void Advance(PatternInstance instance)
    {
        _index = Mod(_index + _direction, _pathLength);

        CellOffset offset = GetOffsetAtIndex(_index);
        instance.SetOrigin(_trailOrigin + offset);
    }

    private CellOffset GetOffsetAtIndex(int index)
    {
        int rightLen = _trailWidth - 1;
        int downLen = _trailHeight - 1;
        int leftLen = _trailWidth - 1;
        int upLen = _trailHeight - 1;

        int i = index;

        // Segment 1: top edge (0,0) -> (rightLen,0)
        if (i < rightLen)
        {
            return new CellOffset(i, 0);
        }
        i -= rightLen;

        // Segment 2: right edge (rightLen,0) -> (rightLen,downLen)
        if (i < downLen)
        {
            return new CellOffset(rightLen, i);
        }
        i -= downLen;

        // Segment 3: bottom edge (rightLen,downLen) -> (0,downLen)
        if (i < leftLen)
        {
            return new CellOffset(rightLen - i, downLen);
        }
        i -= leftLen;

        // Segment 4: left edge (0,downLen) -> (0,0)
        // i < upLen by construction
        return new CellOffset(0, downLen - i);
    }

    private static int Mod(int value, int modulus)
    {
        if (modulus <= 0)
        {
            return 0;
        }

        int r = value % modulus;
        if (r < 0)
        {
            r += modulus;
        }

        return r;
    }
}
