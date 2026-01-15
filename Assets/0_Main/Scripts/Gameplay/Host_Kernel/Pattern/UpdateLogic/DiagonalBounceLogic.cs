public sealed class DiagonalBounceLogic : IPatternUpdateLogic
{
    private readonly int _minOriginX;
    private readonly int _maxOriginX;
    private readonly int _minOriginY;
    private readonly int _maxOriginY;
    private readonly float _stepCooldown;

    private float _timer;

    private int _dirX;
    private int _dirY;

    public DiagonalBounceLogic(
        int minOriginX,
        int maxOriginX,
        int minOriginY,
        int maxOriginY,
        float stepCooldown,
        int initialDirX,
        int initialDirY)
    {
        _minOriginX = minOriginX;
        _maxOriginX = maxOriginX;
        _minOriginY = minOriginY;
        _maxOriginY = maxOriginY;
        _stepCooldown = stepCooldown;

        _dirX = initialDirX >= 0 ? 1 : -1;
        _dirY = initialDirY >= 0 ? 1 : -1;

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

        int nextX = origin.X + _dirX;
        int nextY = origin.Y + _dirY;

        // Bounce X
        if (nextX < _minOriginX)
        {
            nextX = _minOriginX;
            _dirX = 1;
        }
        else if (nextX > _maxOriginX)
        {
            nextX = _maxOriginX;
            _dirX = -1;
        }

        // Bounce Y
        if (nextY < _minOriginY)
        {
            nextY = _minOriginY;
            _dirY = 1;
        }
        else if (nextY > _maxOriginY)
        {
            nextY = _maxOriginY;
            _dirY = -1;
        }

        int deltaX = nextX - origin.X;
        int deltaY = nextY - origin.Y;

        if (deltaX != 0 || deltaY != 0)
        {
            CellOffset step = new CellOffset(deltaX, deltaY);
            instance.MoveBy(step);
        }
    }
}
