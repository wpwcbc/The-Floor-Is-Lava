using System.Collections.Generic;
using UnityEngine;

public sealed class CellTouchRuleSystem : MonoBehaviour
{
    [SerializeField] private PatternRuntimeManager patternRuntimeManager;
    [SerializeField] private GridFlashEffectSystem gridFlashEffectSystem;

    [SerializeField]
    private CurrentCellsProvider cellsProvider;

    // Strategy map: which behavior to use for which role.
    private readonly Dictionary<CellRole, ICellRoleBehavior> roleBehaviors =
        new Dictionary<CellRole, ICellRoleBehavior>();

    private void Awake()
    {
        if (patternRuntimeManager == null)
        {
            patternRuntimeManager = FindFirstObjectByType<PatternRuntimeManager>();
        }

        roleBehaviors[CellRole.Forbidden] = new ForbiddenRoleBehavior(patternRuntimeManager, gridFlashEffectSystem);
        roleBehaviors[CellRole.Weakness] = new WeaknessRoleBehavior(patternRuntimeManager);
    }


    private void OnEnable()
    {
        if (cellsProvider == null)
        {
            cellsProvider = CurrentCellsProvider.Instance;
        }

        if (cellsProvider == null)
        {
            Debug.LogError("[CellTouchRuleSystem] cellsProvider is null.");
            return;
        }

        IReadOnlyList<ITouchCell> currentCells = cellsProvider.CurrentCells;
        if (currentCells != null)
        {
            for (int i = 0; i < currentCells.Count; i++)
            {
                RegisterCell(currentCells[i]);
            }
        }

        cellsProvider.CellRegisteredEvents += RegisterCell;
        cellsProvider.CellUnregisteredEvents += UnregisterCell;
    }

    private void OnDisable()
    {
        if (cellsProvider == null)
        {
            return;
        }

        cellsProvider.CellRegisteredEvents -= RegisterCell;
        cellsProvider.CellUnregisteredEvents -= UnregisterCell;

        IReadOnlyList<ITouchCell> currentCells = cellsProvider.CurrentCells;
        if (currentCells == null)
        {
            return;
        }

        for (int i = 0; i < currentCells.Count; i++)
        {
            UnregisterCell(currentCells[i]);
        }
    }

    private void RegisterCell(ITouchCell cell)
    {
        cell.Touched += OnCellTouched;
        cell.Untouched += OnCellUntouched;
        cell.RoleChanged += OnCellRoleChanged;
    }

    private void UnregisterCell(ITouchCell cell)
    {
        cell.Touched -= OnCellTouched;
        cell.Untouched -= OnCellUntouched;
        cell.RoleChanged -= OnCellRoleChanged;
    }

    private void OnCellTouched(ITouchCell cell)
    {
        ICellRoleBehavior behavior;
        if (!roleBehaviors.TryGetValue(cell.role, out behavior))
        {
            return;
        }

        Debug.Log("CellToouchRuleSystem OnCellTouched");
        behavior.OnTouched(cell);
    }

    private void OnCellUntouched(ITouchCell cell)
    {
        ICellRoleBehavior behavior;
        if (!roleBehaviors.TryGetValue(cell.role, out behavior))
        {
            return;
        }

        behavior.OnUntouched(cell);
    }

    private void OnCellRoleChanged(ITouchCell cell, CellRole oldRole, CellRole newRole)
    {
        // If no one is touching this cell, we do nothing.
        if (!cell.IsTouched)
        {
            return;
        }

        // Treat this as "untouch old role, touch new role" while the physical contact stays.
        ICellRoleBehavior oldBehavior;
        if (roleBehaviors.TryGetValue(oldRole, out oldBehavior))
        {
            oldBehavior.OnUntouched(cell);
        }

        ICellRoleBehavior newBehavior;
        if (roleBehaviors.TryGetValue(newRole, out newBehavior))
        {

            newBehavior.OnTouched(cell);
        }
    }
}
