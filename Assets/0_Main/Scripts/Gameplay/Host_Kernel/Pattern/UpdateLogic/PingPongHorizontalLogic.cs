using System.Collections.Generic;

public sealed class PingPongHorizontalLogic : IPatternUpdateLogic
{
    private readonly List<CellOffset> _steps;
    private readonly List<float> _stepDurations;

    private int _currentStepIndex;
    private float _stepTimer;

    public PingPongHorizontalLogic()
    {
        // Sequence: +1, +1, -1, -1
        _steps = new List<CellOffset>
        {
            new CellOffset(1, 0),
            new CellOffset(1, 0),
            new CellOffset(-1, 0),
            new CellOffset(-1, 0)
        };

        // Per-step cooldowns (seconds) – here all 5s, but could be different
        _stepDurations = new List<float>
        {
            0.5f,
            0.6f,
            0.7f,
            0.8f
        };

        _currentStepIndex = 0;
        _stepTimer = 0.0f;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        _stepTimer += deltaTime;

        float currentDuration = _stepDurations[_currentStepIndex];
        if (_stepTimer < currentDuration)
        {
            return;
        }

        _stepTimer -= currentDuration;

        // Apply movement
        CellOffset step = _steps[_currentStepIndex];
        instance.MoveBy(step);

        // Optionally advance animation frame with each step
        instance.NextFrame();

        // Move to next step (loop)
        _currentStepIndex++;
        if (_currentStepIndex >= _steps.Count)
        {
            _currentStepIndex = 0;
        }
    }
}
