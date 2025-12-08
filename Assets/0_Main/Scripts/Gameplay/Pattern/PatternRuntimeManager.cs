using System.Collections.Generic;
using UnityEngine;

public sealed class PatternRuntimeManager : MonoBehaviour
{
    [SerializeField]
    private PatternToCellsRenderer cellRenderer;

    private readonly List<PatternInstance> _instances = new List<PatternInstance>();

    [SerializeField]
    private float _currentSlowFactor = 1.0f;

    [SerializeField]
    private float _slowMotionTimeRemaining = 0.0f;

    public void RegisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_instances.Contains(instance))
        {
            return;
        }

        _instances.Add(instance);

        if (cellRenderer != null)
        {
            cellRenderer.RegisterPatternInstance(instance);
        }
    }

    public void UnregisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_instances.Remove(instance))
        {
            if (cellRenderer != null)
            {
                cellRenderer.UnregisterPatternInstance(instance);
            }
        }
    }

    public PatternInstance GetPatternInstanceForCell(ITouchCell cell)
    {
        if (cellRenderer == null)
        {
            return null;
        }

        return cellRenderer.GetOwnerPatternInstance(cell);
    }

    public void KillPatternAtCell(ITouchCell cell)
    {
        if (cell == null)
        {
            return;
        }

        PatternInstance instance = GetPatternInstanceForCell(cell);
        if (instance == null)
        {
            return;
        }

        KillPatternInstance(instance);
    }

    public void KillPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!_instances.Contains(instance))
        {
            return;
        }

        // Domain-level kill (fires events, scoring listeners, etc.).
        instance.Kill();

        // Lifetime management.
        UnregisterPatternInstance(instance);
    }

    public void ApplySlowMotion(float durationSeconds, float factor)
    {
        if (durationSeconds <= 0.0f)
        {
            return;
        }

        float clampedFactor = Mathf.Clamp(factor, 0.05f, 1.0f);

        if (clampedFactor < _currentSlowFactor)
        {
            _currentSlowFactor = clampedFactor;
        }

        if (durationSeconds > _slowMotionTimeRemaining)
        {
            _slowMotionTimeRemaining = durationSeconds;
        }
    }

    private void Update()
    {
        float frameDelta = Time.deltaTime;

        if (_slowMotionTimeRemaining > 0.0f)
        {
            _slowMotionTimeRemaining -= frameDelta;
            if (_slowMotionTimeRemaining <= 0.0f)
            {
                _slowMotionTimeRemaining = 0.0f;
                _currentSlowFactor = 1.0f;
            }
        }

        float logicDelta = frameDelta * _currentSlowFactor;

        for (int i = 0; i < _instances.Count; i++)
        {
            PatternInstance instance = _instances[i];
            if (!instance.IsAlive)
            {
                continue;
            }

            instance.Tick(logicDelta);
        }
    }
}
