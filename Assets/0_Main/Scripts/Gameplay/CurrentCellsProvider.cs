using System.Collections.Generic;
using UnityEngine;

public class CurrentCellsProvider : MonoBehaviour
{
    #region Singleton
    public static CurrentCellsProvider Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // optional, but avoids multiple singletons
            return;
        }

        Instance = this;
    }
    #endregion

    private readonly Dictionary<Vector2Int, ITouchCell> _cellsByIndex = new();
    public IReadOnlyDictionary<Vector2Int, ITouchCell> CellsByIndex => _cellsByIndex;

    private List<ITouchCell> _currentCells = new();
    public IReadOnlyList<ITouchCell> CurrentCells => _currentCells;

    public event System.Action<ITouchCell> CellRegisteredEvents;
    public event System.Action<ITouchCell> CellUnregisteredEvents;

    public void RegisterCell(ITouchCell cell)
    {
        if (cell == null) return;
        if (_currentCells.Contains(cell)) return; // avoid duplicates

        _currentCells.Add(cell);

        _cellsByIndex[cell.Position] = cell;

        CellRegisteredEvents(cell);
    }

    public void UnregisterCell(ITouchCell cell)
    {
        if (cell == null) return;

        _currentCells.Remove(cell);

        if (_cellsByIndex.TryGetValue(cell.Position, out var existing) && ReferenceEquals(existing, cell))
        {
            _cellsByIndex.Remove(cell.Position);
        }

        CellUnregisteredEvents(cell);
    }

    public bool TryGetCell(Vector2Int index, out ITouchCell cell)
    {
        return _cellsByIndex.TryGetValue(index, out cell);
    }
}
