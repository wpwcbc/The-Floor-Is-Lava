using System.Collections.Generic;
using UnityEngine;

public sealed class PatternToCellsRenderer : MonoBehaviour
{
    private readonly List<PatternInstance> _patternInstances = new List<PatternInstance>();

    public void RegisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_patternInstances.Contains(instance))
        {
            return;
        }

        _patternInstances.Add(instance);
    }

    public void UnregisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        _patternInstances.Remove(instance);
    }

    private void LateUpdate()
    {
        RenderPatternsToCells();
    }

    private void RenderPatternsToCells()
    {
        CurrentCellsProvider provider = CurrentCellsProvider.Instance;
        if (provider == null)
        {
            return;
        }

        IReadOnlyList<ITouchCell> allCells = provider.CurrentCells;

        // 1. Clear roles on all cells (visual background can stay as-is or be handled by the cell itself)
        for (int i = 0; i < allCells.Count; i++)
        {
            ITouchCell cell = allCells[i];
            cell.SetRole(CellRole.None);
            cell.SetColor(CellColor.Black);
        }

        // 2. Collect final cell states with layer resolution
        Dictionary<Vector2Int, WorldPatternCell> resolvedCells =
            new Dictionary<Vector2Int, WorldPatternCell>();

        for (int i = 0; i < _patternInstances.Count; i++)
        {
            PatternInstance pattern = _patternInstances[i];
            IEnumerable<WorldPatternCell> occupiedCells = pattern.GetOccupiedCells();

            foreach (WorldPatternCell worldCell in occupiedCells)
            {
                Vector2Int index = new Vector2Int(worldCell.Index.X, worldCell.Index.Y);

                WorldPatternCell existing;
                if (resolvedCells.TryGetValue(index, out existing))
                {
                    // Higher layer wins
                    if (worldCell.Layer >= existing.Layer)
                    {
                        resolvedCells[index] = worldCell;
                    }
                }
                else
                {
                    resolvedCells.Add(index, worldCell);
                }
            }
        }

        // 3. Apply resolved states to the actual ITouchCell instances
        foreach (KeyValuePair<Vector2Int, WorldPatternCell> pair in resolvedCells)
        {
            ITouchCell cell;
            if (!provider.TryGetCell(pair.Key, out cell))
            {
                continue;
            }

            WorldPatternCell patternCell = pair.Value;

            cell.SetRole(patternCell.Role);
            cell.SetColor(patternCell.Color);
        }
    }
}
