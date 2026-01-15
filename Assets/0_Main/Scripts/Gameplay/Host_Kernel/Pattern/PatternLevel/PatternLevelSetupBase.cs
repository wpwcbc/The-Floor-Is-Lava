using System.Collections.Generic;
using UnityEngine;

public abstract class PatternLevelSetupBase : MonoBehaviour
{
    [SerializeField]
    private PatternRuntimeManager runtimeManager;

    private readonly List<PatternInstance> _buffer = new List<PatternInstance>();
    private readonly List<PatternInstance> _standbyInstances = new List<PatternInstance>();

    protected PatternRuntimeManager RuntimeManager
    {
        get { return runtimeManager; }
    }

    /// <summary>
    /// Called by the host to build and register all standby patterns for this level.
    /// This is for pre-game "waiting" / intro state.
    /// </summary>
    public void InitializeStandby(PatternRuntimeManager manager)
    {
        if (manager != null)
        {
            runtimeManager = manager;
        }
        else
        {
            EnsureRuntimeManager();
        }

        if (runtimeManager == null)
        {
            Debug.LogError("[PatternLevelSetupBase] No PatternRuntimeManager found for standby.");
            return;
        }

        int gridWidth;
        int gridHeight;
        if (!runtimeManager.TryGetGridSize(out gridWidth, out gridHeight))
        {
            Debug.LogError("[PatternLevelSetupBase] RuntimeManager does not have a valid grid size for standby.");
            return;
        }

        ClearStandbyInternal();

        _buffer.Clear();
        BuildStandbyPatterns(_buffer, gridWidth, gridHeight);

        for (int i = 0; i < _buffer.Count; i++)
        {
            PatternInstance instance = _buffer[i];
            if (instance == null)
            {
                continue;
            }

            runtimeManager.RegisterPatternInstance(instance);
            _standbyInstances.Add(instance);
        }
    }

    /// <summary>
    /// Called by the host to build and register all patterns for this level.
    /// </summary>
    public void InitializeLevel(PatternRuntimeManager manager)
    {
        if (manager != null)
        {
            runtimeManager = manager;
        }
        else
        {
            EnsureRuntimeManager();
        }

        if (runtimeManager == null)
        {
            Debug.LogError("[PatternLevelSetupBase] No PatternRuntimeManager found.");
            return;
        }

        int gridWidth;
        int gridHeight;
        if (!runtimeManager.TryGetGridSize(out gridWidth, out gridHeight))
        {
            Debug.LogError("[PatternLevelSetupBase] RuntimeManager does not have a valid grid size.");
            return;
        }

        // Make sure standby visuals are removed when the real level starts.
        ClearStandbyInternal();

        _buffer.Clear();
        BuildLevelPatterns(_buffer, gridWidth, gridHeight);

        for (int i = 0; i < _buffer.Count; i++)
        {
            PatternInstance instance = _buffer[i];
            if (instance == null)
            {
                continue;
            }

            runtimeManager.RegisterPatternInstance(instance);
        }
    }

    /// <summary>
    /// Optional explicit API if the host wants to hide standby manually.
    /// </summary>
    public void ClearStandby()
    {
        ClearStandbyInternal();
    }

    private void ClearStandbyInternal()
    {
        if (runtimeManager == null)
        {
            _standbyInstances.Clear();
            return;
        }

        for (int i = 0; i < _standbyInstances.Count; i++)
        {
            PatternInstance instance = _standbyInstances[i];
            if (instance == null)
            {
                continue;
            }

            runtimeManager.UnregisterPatternInstance(instance);
        }

        _standbyInstances.Clear();
    }

    private void EnsureRuntimeManager()
    {
        if (runtimeManager != null)
        {
            return;
        }

        runtimeManager = FindFirstObjectByType<PatternRuntimeManager>();
    }

    /// <summary>
    /// Override to build the main gameplay patterns.
    /// </summary>
    protected abstract void BuildLevelPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight);

    /// <summary>
    /// Override to build standby patterns (pre-game visuals).
    /// Default is no standby.
    /// </summary>
    protected virtual void BuildStandbyPatterns(
        List<PatternInstance> buffer,
        int gridWidth,
        int gridHeight)
    {
        // Default: no standby.
    }
}
