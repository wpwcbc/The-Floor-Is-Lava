using UnityEngine;

public interface IKernelInterface
{
    // From viewport → kernel: touch events
    void HandleCellTouched(Vector2Int worldIndex, CellRole role, CellColor color);
    void HandleCellUntouched(Vector2Int worldIndex, CellRole role, CellColor color);
}

public sealed class KernelInterface : MonoBehaviour, IKernelInterface
{
    private const string ScriptName = "KernelInterface";

    [SerializeField]
    private GridTouchRuleSystem gridTouchRuleSystem;

    [SerializeField]
    private PatternToGridResolver resolver;

    private void Awake()
    {
        if (gridTouchRuleSystem == null)
        {
            gridTouchRuleSystem = FindFirstObjectByType<GridTouchRuleSystem>();
        }

        if (gridTouchRuleSystem == null)
        {
            Debug.LogError($"[{ScriptName}] GridTouchRuleSystem is not assigned or found.", this);
        }

        if (resolver == null)
        {
            resolver = FindFirstObjectByType<PatternToGridResolver>();
        }

        if (resolver == null)
        {
            Debug.LogError($"[{ScriptName}] PatternToGridResolver is not assigned or found.", this);
        }
    }

    // --- touch events from viewport ---

    public void HandleCellTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (gridTouchRuleSystem == null)
        {
            Debug.LogError($"[{ScriptName}] HandleCellTouched called but gridTouchRuleSystem is null.", this);
            return;
        }

        gridTouchRuleSystem.HandleCellTouched(worldIndex, role, color);
    }

    public void HandleCellUntouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (gridTouchRuleSystem == null)
        {
            Debug.LogError($"[{ScriptName}] HandleCellUntouched called but gridTouchRuleSystem is null.", this);
            return;
        }

        gridTouchRuleSystem.HandleCellUntouched(worldIndex, role, color);
    }

    // --- grid state



}

