using System.Collections.Generic;
using UnityEngine;

public sealed class PatternRuntimeManager : MonoBehaviour
{
    [SerializeField]
    private PatternToGridResolver gridResolver;

    private readonly List<PatternInstance> _instances = new List<PatternInstance>();

    [SerializeField]
    private float _currentSlowFactor = 1.0f;

    [SerializeField]
    private float _slowMotionTimeRemaining = 0.0f;

    [SerializeField]
    private bool _simulationEnabled = true;

    public bool SimulationEnabled
    {
        get { return _simulationEnabled; }
    }

    public void SetSimulationEnabled(bool enabled)
    {
        _simulationEnabled = enabled;
    }

    public bool TryGetGridSize(out int width, out int height)
    {
        if (gridResolver == null)
        {
            width = 0;
            height = 0;
            Debug.LogError("[PatternRuntimeManager] gridResolver is null in TryGetGridSize.", this);
            return false;
        }

        width = gridResolver.GridWidth;
        height = gridResolver.GridHeight;
        return true;
    }

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

        if (gridResolver != null)
        {
            gridResolver.RegisterPatternInstance(instance);
        }
        else
        {
            Debug.LogError("[PatternRuntimeManager] gridResolver is null in RegisterPatternInstance.", this);
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
            if (gridResolver != null)
            {
                gridResolver.UnregisterPatternInstance(instance);
            }
            else
            {
                Debug.LogError("[PatternRuntimeManager] gridResolver is null in UnregisterPatternInstance.", this);
            }
        }
    }

    public PatternInstance GetPatternInstanceAtIndex(Vector2Int worldIndex)
    {
        if (gridResolver == null)
        {
            Debug.LogError("[PatternRuntimeManager] gridResolver is null in GetPatternInstanceAtIndex.", this);
            return null;
        }

        return gridResolver.GetOwnerAt(worldIndex);
    }

    public void KillPatternAtIndex(Vector2Int worldIndex)
    {
        PatternInstance instance = GetPatternInstanceAtIndex(worldIndex);
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

        instance.Kill();
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
        if (!_simulationEnabled)
        {
            return;
        }

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

        int count = _instances.Count;
        for (int i = 0; i < count; i++)
        {
            PatternInstance instance = _instances[i];
            if (!instance.IsAlive)
            {
                continue;
            }

            instance.Tick(logicDelta);
        }
    }

    /// <summary>
    /// Count how many weakness cells still exist in the world,
    /// based on all alive pattern instances (ignores layering).
    /// </summary>
    public int GetWeaknessCellCount()
    {
        int count = 0;

        int instanceCount = _instances.Count;
        for (int i = 0; i < instanceCount; i++)
        {
            PatternInstance instance = _instances[i];
            if (instance == null)
            {
                continue;
            }

            if (!instance.IsAlive)
            {
                continue;
            }

            IEnumerable<WorldPatternCell> occupiedCells = instance.GetOccupiedCells();
            if (occupiedCells == null)
            {
                continue;
            }

            foreach (WorldPatternCell worldCell in occupiedCells)
            {
                if (worldCell.Role == CellRole.Weakness)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Fill the buffer with world indices of all weakness cells that still exist
    /// (based on alive pattern instances, ignores layering).
    /// </summary>
    public void GetWeaknessCellIndices(List<Vector2Int> buffer)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternRuntimeManager] GetWeaknessCellIndices called with null buffer.", this);
            return;
        }

        buffer.Clear();

        int instanceCount = _instances.Count;
        for (int i = 0; i < instanceCount; i++)
        {
            PatternInstance instance = _instances[i];
            if (instance == null)
            {
                continue;
            }

            if (!instance.IsAlive)
            {
                continue;
            }

            IEnumerable<WorldPatternCell> occupiedCells = instance.GetOccupiedCells();
            if (occupiedCells == null)
            {
                continue;
            }

            foreach (WorldPatternCell worldCell in occupiedCells)
            {
                if (worldCell.Role != CellRole.Weakness)
                {
                    continue;
                }

                Vector2Int index = new Vector2Int(worldCell.Index.X, worldCell.Index.Y);
                buffer.Add(index);
            }
        }
    }
}
