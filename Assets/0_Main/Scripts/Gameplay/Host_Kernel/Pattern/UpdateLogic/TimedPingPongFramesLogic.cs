using UnityEngine;

public sealed class TimedPingPongFramesLogic : IPatternUpdateLogic
{
    private readonly int _frameA;
    private readonly int _frameB;
    private readonly float _frameDurationSeconds;
    private readonly float _initialPhaseSeconds;

    private bool _initialized;
    private float _timer;
    private int _currentFrame;

    private bool _loggedBadFrames;
    private bool _loggedNullInstance;

    public TimedPingPongFramesLogic(
        int frameA,
        int frameB,
        float frameDurationSeconds,
        float initialPhaseSeconds)
    {
        _frameA = frameA;
        _frameB = frameB;

        if (frameDurationSeconds < 0.0f)
        {
            frameDurationSeconds = 0.0f;
        }

        _frameDurationSeconds = frameDurationSeconds;

        if (initialPhaseSeconds < 0.0f)
        {
            initialPhaseSeconds = 0.0f;
        }

        _initialPhaseSeconds = initialPhaseSeconds;

        _initialized = false;
        _timer = 0.0f;
        _currentFrame = frameA;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            if (!_loggedNullInstance)
            {
                _loggedNullInstance = true;
                Debug.LogError("[TimedPingPongFramesLogic] Tick called with null PatternInstance.");
            }
            return;
        }

        if (!_initialized)
        {
            _initialized = true;

            int frameCount = instance.Definition != null && instance.Definition.Frames != null
                ? instance.Definition.Frames.Count
                : 0;

            int maxIndex = _frameA > _frameB ? _frameA : _frameB;

            if (frameCount <= maxIndex || frameCount <= 0)
            {
                if (!_loggedBadFrames)
                {
                    _loggedBadFrames = true;
                    Debug.LogError(
                        "[TimedPingPongFramesLogic] PatternDefinition does not contain required frames. " +
                        "Requested: " + _frameA + " and " + _frameB + ", frameCount=" + frameCount);
                }
                return;
            }

            _currentFrame = _frameA;
            instance.SetFrame(_currentFrame);

            _timer = _initialPhaseSeconds;

            if (_frameDurationSeconds > 0.0f)
            {
                // Apply phase by advancing the state as many times as needed.
                while (_timer >= _frameDurationSeconds)
                {
                    _timer -= _frameDurationSeconds;
                    Toggle(instance);
                }
            }

            return;
        }

        if (_frameDurationSeconds <= 0.0f)
        {
            // If duration is 0, flip every tick. (Not recommended, but defined.)
            Toggle(instance);
            return;
        }

        _timer += deltaTime;

        while (_timer >= _frameDurationSeconds)
        {
            _timer -= _frameDurationSeconds;
            Toggle(instance);
        }
    }

    private void Toggle(PatternInstance instance)
    {
        _currentFrame = (_currentFrame == _frameA) ? _frameB : _frameA;
        instance.SetFrame(_currentFrame);
    }
}
