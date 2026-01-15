using UnityEngine;

public sealed class FrameLoopLogic : IPatternUpdateLogic
{
    private const int MaxStepsPerTick = 32;

    private readonly float _frameCooldownSeconds;
    private readonly int _startFrameIndex;

    private float _timer;
    private bool _initialized;

    private bool _loggedNullInstance;
    private bool _loggedInvalidDefinition;

    public FrameLoopLogic(float frameCooldownSeconds)
        : this(frameCooldownSeconds, 0)
    {
    }

    public FrameLoopLogic(float frameCooldownSeconds, int startFrameIndex)
    {
        _frameCooldownSeconds = Mathf.Max(0.0f, frameCooldownSeconds);
        _startFrameIndex = startFrameIndex;

        _timer = 0.0f;
        _initialized = false;

        _loggedNullInstance = false;
        _loggedInvalidDefinition = false;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            if (!_loggedNullInstance)
            {
                _loggedNullInstance = true;
                Debug.LogError("[FrameLoopLogic] Tick received null PatternInstance.");
            }
            return;
        }

        if (_frameCooldownSeconds <= 0.0f)
        {
            return;
        }

        if (instance.Definition == null || instance.Definition.Frames == null)
        {
            if (!_loggedInvalidDefinition)
            {
                _loggedInvalidDefinition = true;
                Debug.LogError("[FrameLoopLogic] PatternInstance has null Definition or Frames.");
            }
            return;
        }

        int frameCount = instance.Definition.Frames.Count;
        if (frameCount <= 1)
        {
            return;
        }

        if (!_initialized)
        {
            _initialized = true;

            int start = _startFrameIndex;
            if (start < 0)
            {
                start = 0;
            }
            else if (start >= frameCount)
            {
                start = frameCount - 1;
            }

            if (start != instance.CurrentFrameIndex)
            {
                instance.SetFrame(start);
            }
        }

        _timer += deltaTime;

        int safetySteps = 0;
        while (_timer >= _frameCooldownSeconds && safetySteps < MaxStepsPerTick)
        {
            _timer -= _frameCooldownSeconds;
            instance.NextFrame();
            safetySteps++;
        }
    }
}
