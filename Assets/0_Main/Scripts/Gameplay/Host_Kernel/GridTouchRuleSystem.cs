using System.Collections.Generic;
using UnityEngine;

public sealed class GridTouchRuleSystem : MonoBehaviour
{
    [SerializeField]
    private PatternRuntimeManager patternRuntimeManager;

    [SerializeField]
    private GridFlashEffectSystem gridFlashEffectSystem;

    private readonly Dictionary<CellRole, ICellRoleBehavior> roleBehaviors =
        new Dictionary<CellRole, ICellRoleBehavior>();

    private readonly HashSet<Vector2Int> overlappedCells =
        new HashSet<Vector2Int>();

    public IReadOnlyCollection<Vector2Int> CurrentOverlappedCells
    {
        get { return overlappedCells; }
    }

    public event System.Action<Vector2Int, CellRole, CellColor> WorldCellTouched;
    public event System.Action<Vector2Int, CellRole, CellColor> WorldCellUntouched;

    private void Awake()
    {
        if (patternRuntimeManager == null)
        {
            patternRuntimeManager = FindFirstObjectByType<PatternRuntimeManager>();
            if (patternRuntimeManager == null)
            {
                Debug.LogError("[GridTouchRuleSystem] PatternRuntimeManager is not assigned or found.", this);
            }
        }

        if (gridFlashEffectSystem == null)
        {
            gridFlashEffectSystem = FindFirstObjectByType<GridFlashEffectSystem>();
            if (gridFlashEffectSystem == null)
            {
                Debug.LogError("[GridTouchRuleSystem] GridFlashEffectSystem is not assigned or found.", this);
            }
        }

        // Wire behaviors with world-based signatures.
        roleBehaviors[CellRole.Forbidden] = new ForbiddenRoleBehavior(gridFlashEffectSystem);
        roleBehaviors[CellRole.Weakness] = new WeaknessRoleBehavior(patternRuntimeManager);
        // Add other roles if needed.
    }

    public void HandleCellTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        overlappedCells.Add(worldIndex);

        ICellRoleBehavior behavior;
        if (roleBehaviors.TryGetValue(role, out behavior))
        {
            behavior.OnTouched(worldIndex, role, color);
        }

        if (WorldCellTouched != null)
        {
            WorldCellTouched(worldIndex, role, color);
        }
    }

    public void HandleCellUntouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        overlappedCells.Remove(worldIndex);

        ICellRoleBehavior behavior;
        if (roleBehaviors.TryGetValue(role, out behavior))
        {
            behavior.OnUntouched(worldIndex, role, color);
        }

        if (WorldCellUntouched != null)
        {
            WorldCellUntouched(worldIndex, role, color);
        }
    }
}
