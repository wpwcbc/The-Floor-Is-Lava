using UnityEngine;

public sealed class RectPerimeterTrailLogic_ExplicitOrigin : IPatternUpdateLogic
{
    public enum Direction
    {
        Clockwise = 1,
        CounterClockwise = -1
    }

    private readonly GridIndex _trailOrigin;
    private readonly int _trailWidth;
    private readonly int _trailHeight;
    private readonly float _stepCooldown;
    private readonly int _direction;

    private readonly int _pathLength;

    private int _index;
    private float _timer;

    private bool _loggedBadConfig;

    public RectPerimeterTrailLogic_ExplicitOrigin(
        GridIndex trailOrigin,
        int trailWidth,
        int trailHeight,
        float stepCooldownSeconds,
        int startIndex,
        Direction direction)
    {
        _trailOrigin = trailOrigin;
        _trailWidth = trailWidth;
        _trailHeight = trailHeight;

        if (stepCooldownSeconds < 0.0f)
        {
            stepCooldownSeconds = 0.0f;
        }

        _stepCooldown = stepCooldownSeconds;
        _direction = (int)direction;

        if (_trailWidth < 2 || _trailHeight < 2)
        {
            _pathLength = 0;
            _index = 0;
        }
        else
        {
            _pathLength = (2 * (_trailWidth + _trailHeight)) - 4;
            _index = Mod(startIndex, _pathLength);
        }

        _timer = 0.0f;
        _loggedBadConfig = false;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            Debug.LogError("[RectPerimeterTrailLogic_ExplicitOrigin] Tick called with null PatternInstance.");
            return;
        }

        if (_pathLength <= 0)
        {
            if (!_loggedBadConfig)
            {
                _loggedBadConfig = true;
                Debug.LogError(
                    "[RectPerimeterTrailLogic_ExplicitOrigin] Invalid trail size. " +
                    "trailWidth=" + _trailWidth + ", trailHeight=" + _trailHeight);
            }
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
        CellOffset offset = GetOffsetAtIndex(_trailWidth, _trailHeight, _index);
        instance.SetOrigin(_trailOrigin + offset);

        _index = Mod(_index + _direction, _pathLength);
    }

    public static int GetPathLength(int trailWidth, int trailHeight)
    {
        if (trailWidth < 2 || trailHeight < 2)
        {
            return 0;
        }

        return (2 * (trailWidth + trailHeight)) - 4;
    }

    public static CellOffset GetOffsetAtIndex(int trailWidth, int trailHeight, int index)
    {
        int rightLen = trailWidth - 1;
        int downLen = trailHeight - 1;
        int leftLen = trailWidth - 1;
        int upLen = trailHeight - 1;

        int pathLength = GetPathLength(trailWidth, trailHeight);
        int i = Mod(index, pathLength);

        // Top edge: (0,0) -> (rightLen,0)
        if (i < rightLen)
        {
            return new CellOffset(i, 0);
        }
        i -= rightLen;

        // Right edge: (rightLen,0) -> (rightLen,downLen)
        if (i < downLen)
        {
            return new CellOffset(rightLen, i);
        }
        i -= downLen;

        // Bottom edge: (rightLen,downLen) -> (0,downLen)
        if (i < leftLen)
        {
            return new CellOffset(rightLen - i, downLen);
        }
        i -= leftLen;

        // Left edge: (0,downLen) -> (0,0)
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
