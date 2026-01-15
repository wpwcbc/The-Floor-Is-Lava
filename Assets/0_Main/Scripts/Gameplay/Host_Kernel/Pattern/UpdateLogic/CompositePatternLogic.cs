using UnityEngine;

public sealed class CompositePatternLogic : IPatternUpdateLogic
{
    private readonly IPatternUpdateLogic[] _logics;
    private bool _loggedNullInstance;

    public CompositePatternLogic(params IPatternUpdateLogic[] logics)
    {
        _logics = logics;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            if (!_loggedNullInstance)
            {
                _loggedNullInstance = true;
                Debug.LogError("[CompositePatternLogic] Tick called with null PatternInstance.");
            }
            return;
        }

        if (_logics == null || _logics.Length == 0)
        {
            Debug.LogError("[CompositePatternLogic] No child logics were provided.");
            return;
        }

        for (int i = 0; i < _logics.Length; i++)
        {
            IPatternUpdateLogic logic = _logics[i];
            if (logic == null)
            {
                Debug.LogError("[CompositePatternLogic] Child logic at index " + i + " is null.");
                continue;
            }

            logic.Tick(instance, deltaTime);
        }
    }
}
