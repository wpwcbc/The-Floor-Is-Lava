using System.Collections.Generic;
using UnityEngine;

public sealed class PatternRuntimeManager : MonoBehaviour
{
    [SerializeField]
    private PatternToCellsRenderer renderer;

    private readonly List<PatternInstance> _instances = new List<PatternInstance>();

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

        if (renderer != null)
        {
            renderer.RegisterPatternInstance(instance);
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
            if (renderer != null)
            {
                renderer.UnregisterPatternInstance(instance);
            }
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < _instances.Count; i++)
        {
            _instances[i].Tick(deltaTime);
        }
    }
}
