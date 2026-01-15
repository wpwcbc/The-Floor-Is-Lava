using UnityEngine;

public sealed class VerticalBounceLogic : IPatternUpdateLogic
{
    private readonly int _minOriginY;
    private readonly int _maxOriginY;
    private readonly float _stepCooldown;

    private float _timer;
    private int _direction;

    public VerticalBounceLogic(
        int minOriginY,
        int maxOriginY,
        float stepCooldown,
        int initialDirection)
    {
        _minOriginY = minOriginY;
        _maxOriginY = maxOriginY;
        _stepCooldown = stepCooldown;

        _direction = initialDirection >= 0 ? 1 : -1;
        _timer = 0.0f;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            return;
        }

        _timer += deltaTime;
        if (_timer < _stepCooldown)
        {
            return;
        }

        _timer -= _stepCooldown;

        GridIndex origin = instance.Origin;
        int nextY = origin.Y + _direction;

        if (nextY < _minOriginY)
        {
            nextY = _minOriginY;
            _direction = 1;
        }
        else if (nextY > _maxOriginY)
        {
            nextY = _maxOriginY;
            _direction = -1;
        }

        int deltaY = nextY - origin.Y;
        if (deltaY != 0)
        {
            CellOffset step = new CellOffset(0, deltaY);
            instance.MoveBy(step);
        }
    }
}
