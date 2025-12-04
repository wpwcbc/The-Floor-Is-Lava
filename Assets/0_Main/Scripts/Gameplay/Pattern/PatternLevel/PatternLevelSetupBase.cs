using System.Collections.Generic;
using UnityEngine;

public abstract class PatternLevelSetupBase : MonoBehaviour
{
    [SerializeField]
    private PatternRuntimeManager runtimeManager;

    private readonly List<PatternInstance> _buffer = new List<PatternInstance>();

    protected PatternRuntimeManager RuntimeManager
    {
        get { return runtimeManager; }
    }

    private void Start()
    {
        EnsureRuntimeManager();
        if (runtimeManager == null)
        {
            Debug.LogError("[PatternLevelSetupBase] No PatternRuntimeManager found.");
            return;
        }

        _buffer.Clear();
        BuildLevelPatterns(_buffer);

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

    private void EnsureRuntimeManager()
    {
        if (runtimeManager != null)
        {
            return;
        }

        runtimeManager = FindFirstObjectByType<PatternRuntimeManager>();
    }

    /// <summary>
    /// Override this in concrete level setup classes.
    /// Add all PatternInstance objects you want in this level to the buffer list.
    /// The base class will handle registration.
    /// </summary>
    protected abstract void BuildLevelPatterns(List<PatternInstance> buffer);
}
