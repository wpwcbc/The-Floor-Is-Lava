using System.Collections.Generic;
using UnityEngine;

public sealed class ViewportCellTouchAdapter : MonoBehaviour
{
    [SerializeField]
    private CurrentCellsProvider cellsProvider;

    [SerializeField]
    private GridViewportRenderer viewportRenderer;

    [SerializeField]
    private LinkToKernelInterface linkToKernelInterface;

    // Flags to avoid spamming the same error every frame / event.
    private bool hasLoggedMissingCellsProvider;
    private bool hasLoggedMissingViewportRenderer;
    private bool hasLoggedMissingKernelInterface;

    private void OnEnable()
    {
        if (cellsProvider == null)
        {
            cellsProvider = CurrentCellsProvider.Instance;
        }

        if (cellsProvider == null)
        {
            if (!hasLoggedMissingCellsProvider)
            {
                Debug.LogError("[ViewportCellTouchAdapter] cellsProvider is null on OnEnable. CurrentCellsProvider.Instance was not found.", this);
                hasLoggedMissingCellsProvider = true;
            }

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
            if (!hasLoggedMissingCellsProvider)
            {
                Debug.LogError("[ViewportCellTouchAdapter] cellsProvider is null on OnDisable.", this);
                hasLoggedMissingCellsProvider = true;
            }

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
        if (cell == null)
        {
            Debug.LogError("[ViewportCellTouchAdapter] Attempted to register a null ITouchCell.", this);
            return;
        }

        cell.Touched += OnCellTouched;
        cell.Untouched += OnCellUntouched;
        cell.RoleChanged += OnCellRoleChanged;
    }

    private void UnregisterCell(ITouchCell cell)
    {
        if (cell == null)
        {
            Debug.LogError("[ViewportCellTouchAdapter] Attempted to unregister a null ITouchCell.", this);
            return;
        }

        cell.Touched -= OnCellTouched;
        cell.Untouched -= OnCellUntouched;
        cell.RoleChanged -= OnCellRoleChanged;
    }

    private bool TryGetWorldIndexAndState(ITouchCell cell, out Vector2Int worldIndex)
    {
        worldIndex = default(Vector2Int);

        if (viewportRenderer == null)
        {
            if (!hasLoggedMissingViewportRenderer)
            {
                Debug.LogError("[ViewportCellTouchAdapter] viewportRenderer is null in TryGetWorldIndexAndState.", this);
                hasLoggedMissingViewportRenderer = true;
            }

            return false;
        }

        if (cell == null)
        {
            Debug.LogError("[ViewportCellTouchAdapter] ITouchCell is null in TryGetWorldIndexAndState.", this);
            return false;
        }

        if (!viewportRenderer.TryGetWorldIndexForCell(cell, out worldIndex))
        {
            // This is not an error: cell may simply be outside this viewport.
            return false;
        }

        return true;
    }

    private void OnCellTouched(ITouchCell cell)
    {
        if (linkToKernelInterface == null)
        {
            if (!hasLoggedMissingKernelInterface)
            {
                Debug.LogError("[ViewportCellTouchAdapter] linkToKernelInterface is null in OnCellTouched.", this);
                hasLoggedMissingKernelInterface = true;
            }

            return;
        }

        Vector2Int worldIndex;
        if (!TryGetWorldIndexAndState(cell, out worldIndex))
        {
            return;
        }

        linkToKernelInterface.HandleCellTouched(worldIndex, cell.role, cell.color);
    }

    private void OnCellUntouched(ITouchCell cell)
    {
        if (linkToKernelInterface == null)
        {
            if (!hasLoggedMissingKernelInterface)
            {
                Debug.LogError("[ViewportCellTouchAdapter] linkToKernelInterface is null in OnCellUntouched.", this);
                hasLoggedMissingKernelInterface = true;
            }

            return;
        }

        Vector2Int worldIndex;
        if (!TryGetWorldIndexAndState(cell, out worldIndex))
        {
            return;
        }

        linkToKernelInterface.HandleCellUntouched(worldIndex, cell.role, cell.color);
    }

    private void OnCellRoleChanged(ITouchCell cell, CellRole oldRole, CellRole newRole)
    {
        if (!cell.IsTouched)
        {
            return;
        }

        Vector2Int worldIndex;
        if (!TryGetWorldIndexAndState(cell, out worldIndex))
        {
            return;
        }

        if (linkToKernelInterface == null)
        {
            if (!hasLoggedMissingKernelInterface)
            {
                Debug.LogError("[ViewportCellTouchAdapter] linkToKernelInterface is null in OnCellRoleChanged.", this);
                hasLoggedMissingKernelInterface = true;
            }

            return;
        }

        // emulate "untouch old role, touch new role"
        linkToKernelInterface.HandleCellUntouched(worldIndex, oldRole, cell.color);
        linkToKernelInterface.HandleCellTouched(worldIndex, newRole, cell.color);
    }
}
