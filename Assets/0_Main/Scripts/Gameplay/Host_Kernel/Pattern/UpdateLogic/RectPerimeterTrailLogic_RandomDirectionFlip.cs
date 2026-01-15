using UnityEngine;

public sealed class RectPerimeterTrailLogic_RandomDirectionFlip : IPatternUpdateLogic
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

    private readonly float _minFlipDuration;
    private readonly float _maxFlipDuration;

    private readonly int _pathLength;

    private int _index;
    private int _direction;

    // Time accumulation
    private float _stepTimer;
    private float _flipTimer;

    // Current duration until the next flip
    private float _currentFlipDuration;

    private bool _loggedBadConfig;

    public RectPerimeterTrailLogic_RandomDirectionFlip(
        GridIndex trailOrigin,
        int trailWidth,
        int trailHeight,
        float stepCooldownSeconds,
        int startIndex,
        Direction startDirection,
        float minFlipDurationSeconds,
        float maxFlipDurationSeconds)
    {
        _trailOrigin = trailOrigin;
        _trailWidth = trailWidth;
        _trailHeight = trailHeight;

        if (stepCooldownSeconds < 0.0f)
        {
            stepCooldownSeconds = 0.0f;
        }
        _stepCooldown = stepCooldownSeconds;

        float minFlip = minFlipDurationSeconds;
        float maxFlip = maxFlipDurationSeconds;

        if (minFlip < 0.0f)
        {
            minFlip = 0.0f;
        }
        if (maxFlip < 0.0f)
        {
            maxFlip = 0.0f;
        }
        if (maxFlip < minFlip)
        {
            float tmp = minFlip;
            minFlip = maxFlip;
            maxFlip = tmp;
        }

        // Prevent zero-length flip durations (can cause infinite flip loops in a single tick).
        if (maxFlip <= 0.0f)
        {
            minFlip = 0.1f;
            maxFlip = 0.1f;
        }
        else if (minFlip <= 0.0f)
        {
            minFlip = Mathf.Min(0.1f, maxFlip);
        }

        _minFlipDuration = minFlip;
        _maxFlipDuration = maxFlip;

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

        _direction = (int)startDirection;

        _stepTimer = 0.0f;
        _flipTimer = 0.0f;
        _currentFlipDuration = SampleNextFlipDuration();

        _loggedBadConfig = false;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            Debug.LogError("[RectPerimeterTrailLogic_RandomDirectionFlip] Tick called with null PatternInstance.");
            return;
        }

        if (_pathLength <= 0)
        {
            if (!_loggedBadConfig)
            {
                _loggedBadConfig = true;
                Debug.LogError(
                    "[RectPerimeterTrailLogic_RandomDirectionFlip] Invalid trail size. " +
                    "trailWidth=" + _trailWidth + ", trailHeight=" + _trailHeight);
            }
            return;
        }

        if (deltaTime < 0.0f)
        {
            Debug.LogError("[RectPerimeterTrailLogic_RandomDirectionFlip] Tick called with negative deltaTime.");
            return;
        }

        // If stepCooldown is 0: same semantics as your original logic (advance once per Tick).
        // Direction flipping still happens over time; whichever direction is active at the end of Tick is used for this move.
        if (_stepCooldown <= 0.0f)
        {
            AdvanceFlipTimers(deltaTime);
            Advance(instance);
            return;
        }

        // Event-style simulation: within this deltaTime, perform flips and steps in correct chronological order.
        float remaining = deltaTime;

        while (remaining > 0.0f)
        {
            float timeToNextStep = _stepCooldown - _stepTimer;
            if (timeToNextStep < 0.0f)
            {
                timeToNextStep = 0.0f;
            }

            float timeToNextFlip = _currentFlipDuration - _flipTimer;
            if (timeToNextFlip < 0.0f)
            {
                timeToNextFlip = 0.0f;
            }

            float dt = remaining;

            if (timeToNextStep < dt)
            {
                dt = timeToNextStep;
            }

            if (timeToNextFlip < dt)
            {
                dt = timeToNextFlip;
            }

            // Progress time
            _stepTimer += dt;
            _flipTimer += dt;
            remaining -= dt;

            // Resolve flip first if both happen "now"
            if (_flipTimer >= _currentFlipDuration)
            {
                // Consume exactly one flip event
                _flipTimer -= _currentFlipDuration;
                FlipDirection();
                _currentFlipDuration = SampleNextFlipDuration();
            }

            if (_stepTimer >= _stepCooldown)
            {
                // Consume exactly one step event
                _stepTimer -= _stepCooldown;
                Advance(instance);
            }

            // Safety: if dt got stuck at 0 due to float issues, break to avoid infinite loops
            if (dt <= 0.0f)
            {
                // Nudge by consuming at least one event if possible
                if (_flipTimer >= _currentFlipDuration)
                {
                    _flipTimer -= _currentFlipDuration;
                    FlipDirection();
                    _currentFlipDuration = SampleNextFlipDuration();
                }
                else if (_stepTimer >= _stepCooldown)
                {
                    _stepTimer -= _stepCooldown;
                    Advance(instance);
                }
                else
                {
                    break;
                }
            }
        }
    }

    private void AdvanceFlipTimers(float deltaTime)
    {
        _flipTimer += deltaTime;

        while (_flipTimer >= _currentFlipDuration)
        {
            _flipTimer -= _currentFlipDuration;
            FlipDirection();
            _currentFlipDuration = SampleNextFlipDuration();
        }
    }

    private void FlipDirection()
    {
        _direction = -_direction;
        if (_direction == 0)
        {
            _direction = 1;
        }
    }

    private float SampleNextFlipDuration()
    {
        if (_maxFlipDuration <= _minFlipDuration)
        {
            return _minFlipDuration;
        }

        // Range is inclusive-exclusive for floats; fine for durations.
        return UnityEngine.Random.Range(_minFlipDuration, _maxFlipDuration);
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
