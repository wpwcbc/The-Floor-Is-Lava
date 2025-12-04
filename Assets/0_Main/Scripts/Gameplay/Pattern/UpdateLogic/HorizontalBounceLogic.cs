public sealed class HorizontalBounceLogic : IPatternUpdateLogic
{
    private readonly int _minOriginX;
    private readonly int _maxOriginX;
    private readonly float _stepCooldown;

    private float _timer;
    private int _direction;

    public HorizontalBounceLogic(
        int minOriginX,
        int maxOriginX,
        float stepCooldown)
    {
        _minOriginX = minOriginX;
        _maxOriginX = maxOriginX;
        _stepCooldown = stepCooldown;

        _direction = 1;
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
        int nextX = origin.X + _direction;

        // Bounce off edges, keeping the origin in [minOriginX, maxOriginX]
        if (nextX < _minOriginX)
        {
            nextX = _minOriginX;
            _direction = 1;
        }
        else if (nextX > _maxOriginX)
        {
            nextX = _maxOriginX;
            _direction = -1;
        }

        int deltaX = nextX - origin.X;
        if (deltaX != 0)
        {
            CellOffset step = new CellOffset(deltaX, 0);
            instance.MoveBy(step);
        }
    }
}
