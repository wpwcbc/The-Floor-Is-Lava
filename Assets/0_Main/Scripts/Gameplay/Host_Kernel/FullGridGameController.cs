using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FullGridGameController : MonoBehaviour, IFullGridGameControl
{
    private const string ScriptName = "FullGridGameController";

    [Header("Core References")]
    [SerializeField]
    private PatternRuntimeManager runtimeManager;

    [SerializeField]
    private PatternToGridResolver resolver;

    [SerializeField]
    private GridTouchRuleSystem gridTouchRuleSystem;

    [Header("Level")]
    [SerializeField]
    private PatternLevelSetupBase defaultLevel;

    private PatternLevelSetupBase _activeLevel;
    private bool _levelRunning;

    public int GridWidth
    {
        get
        {
            if (resolver == null)
            {
                Debug.LogError($"[{ScriptName}] GridWidth requested but resolver is null.", this);
                return 0;
            }

            return resolver.GridWidth;
        }
    }

    public int GridHeight
    {
        get
        {
            if (resolver == null)
            {
                Debug.LogError($"[{ScriptName}] GridHeight requested but resolver is null.", this);
                return 0;
            }

            return resolver.GridHeight;
        }
    }

    private void Awake()
    {
        if (runtimeManager == null)
        {
            runtimeManager = FindFirstObjectByType<PatternRuntimeManager>();
            if (runtimeManager == null)
            {
                Debug.LogError($"[{ScriptName}] PatternRuntimeManager is not assigned or found.", this);
            }
        }

        if (resolver == null)
        {
            resolver = FindFirstObjectByType<PatternToGridResolver>();
            if (resolver == null)
            {
                Debug.LogError($"[{ScriptName}] PatternToGridResolver is not assigned or found.", this);
            }
        }

        if (gridTouchRuleSystem == null)
        {
            gridTouchRuleSystem = FindFirstObjectByType<GridTouchRuleSystem>();
            if (gridTouchRuleSystem == null)
            {
                Debug.LogError($"[{ScriptName}] GridTouchRuleSystem is not assigned or found.", this);
            }
        }

        // Ensure nothing starts ticking before we explicitly start a level.
        if (runtimeManager != null)
        {
            runtimeManager.enabled = false;
        }

        if (resolver != null)
        {
            resolver.enabled = false;
        }
    }

    private void Start()
    {
        if (_activeLevel == null && defaultLevel != null)
        {
            SetActiveLevel(defaultLevel);
        }
    }

    // ---------------- IFullGridGameControl: existing methods ----------------

    public void ConfigureFullGrid(int width, int height)
    {
        if (resolver == null)
        {
            Debug.LogError($"[{ScriptName}] ConfigureFullGrid called but resolver is null.", this);
            return;
        }

        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        resolver.SetWorldGridSize(width, height);
    }

    public void SetActiveLevel(PatternLevelSetupBase level)
    {
        _activeLevel = level;
    }

    public void ShowStandby()
    {
        bool missing = false;

        if (_activeLevel == null)
        {
            Debug.LogError($"[{ScriptName}] ShowStandby called but activeLevel is null.", this);
            missing = true;
        }

        if (runtimeManager == null)
        {
            Debug.LogError($"[{ScriptName}] ShowStandby called but runtimeManager is null.", this);
            missing = true;
        }

        if (resolver == null)
        {
            Debug.LogError($"[{ScriptName}] ShowStandby called but resolver is null.", this);
            missing = true;
        }

        if (missing)
        {
            return;
        }

        _activeLevel.InitializeStandby(runtimeManager);
        resolver.enabled = true;
        runtimeManager.enabled = true;
    }

    public void StartLevel()
    {
        if (_levelRunning)
        {
            return;
        }

        bool missing = false;

        if (resolver == null)
        {
            Debug.LogError($"[{ScriptName}] StartLevel called but resolver is null.", this);
            missing = true;
        }

        if (runtimeManager == null)
        {
            Debug.LogError($"[{ScriptName}] StartLevel called but runtimeManager is null.", this);
            missing = true;
        }

        if (_activeLevel == null)
        {
            Debug.LogError($"[{ScriptName}] StartLevel called but activeLevel is null.", this);
            missing = true;
        }

        if (missing)
        {
            return;
        }

        if (GridWidth <= 0 || GridHeight <= 0)
        {
            Debug.LogError($"[{ScriptName}] Grid size is not configured. Call ConfigureFullGrid first.", this);
            return;
        }

        _activeLevel.InitializeLevel(runtimeManager);

        resolver.enabled = true;
        runtimeManager.enabled = true;

        _levelRunning = true;
    }

    public void StopLevel()
    {
        if (!_levelRunning)
        {
            return;
        }

        if (resolver != null)
        {
            resolver.enabled = false;
        }
        else
        {
            Debug.LogError($"[{ScriptName}] StopLevel called but resolver is null.", this);
        }

        if (runtimeManager != null)
        {
            runtimeManager.enabled = false;
        }
        else
        {
            Debug.LogError($"[{ScriptName}] StopLevel called but runtimeManager is null.", this);
        }

        _levelRunning = false;
    }

    // ---------------- IFullGridGameControl: NEW state methods ----------------

    public IReadOnlyCollection<Vector2Int> GetCurrentOverlappedCells()
    {
        if (gridTouchRuleSystem == null)
        {
            Debug.LogError($"[{ScriptName}] GetCurrentOverlappedCells called but gridTouchRuleSystem is null.", this);
            return Array.Empty<Vector2Int>();
        }

        return gridTouchRuleSystem.CurrentOverlappedCells;
    }

    public int GetWeaknessCellCount()
    {
        if (runtimeManager == null)
        {
            Debug.LogError($"[{ScriptName}] GetWeaknessCellCount called but runtimeManager is null.", this);
            return 0;
        }

        return runtimeManager.GetWeaknessCellCount();
    }

    public void GetWeaknessCellIndices(List<Vector2Int> buffer)
    {
        if (buffer == null)
        {
            Debug.LogError($"[{ScriptName}] GetWeaknessCellIndices called with null buffer.", this);
            return;
        }

        if (runtimeManager == null)
        {
            Debug.LogError($"[{ScriptName}] GetWeaknessCellIndices called but runtimeManager is null.", this);
            return;
        }

        runtimeManager.GetWeaknessCellIndices(buffer);
    }

    // ---------------- IFullGridGameControl: event ----------------

    public event Action<Vector2Int, CellRole, CellColor> WorldCellTouched
    {
        add
        {
            if (gridTouchRuleSystem == null)
            {
                Debug.LogError($"[{ScriptName}] Subscribe WorldCellTouched but gridTouchRuleSystem is null.", this);
                return;
            }

            gridTouchRuleSystem.WorldCellTouched += value;
        }
        remove
        {
            if (gridTouchRuleSystem == null)
            {
                //Debug.LogError($"[{ScriptName}] Unsubscribe WorldCellTouched but gridTouchRuleSystem is null.", this);
                return;
            }

            gridTouchRuleSystem.WorldCellTouched -= value;
        }
    }
}
