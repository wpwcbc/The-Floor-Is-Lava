using System.Collections.Generic;
using UnityEngine;

namespace LEGACY
{
    public sealed class PatternToCellsRenderer : MonoBehaviour
    {
        private readonly List<PatternInstance> _patternInstances = new List<PatternInstance>();

        private readonly Dictionary<ITouchCell, PatternInstance> _cellOwners =
            new Dictionary<ITouchCell, PatternInstance>();

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
            // _cellOwners will be rebuilt next frame, so no need to clean it here.
        }

        public PatternInstance GetOwnerPatternInstance(ITouchCell cell)
        {
            if (cell == null)
            {
                return null;
            }

            PatternInstance owner;
            if (_cellOwners.TryGetValue(cell, out owner))
            {
                return owner;
            }

            return null;
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
            if (allCells == null)
            {
                return;
            }

            _cellOwners.Clear();

            // 1. Collect final cell states with layer resolution.
            Dictionary<Vector2Int, WorldPatternCell> resolvedCells =
                new Dictionary<Vector2Int, WorldPatternCell>();

            Dictionary<Vector2Int, PatternInstance> resolvedOwners =
                new Dictionary<Vector2Int, PatternInstance>();

            for (int i = 0; i < _patternInstances.Count; i++)
            {
                PatternInstance pattern = _patternInstances[i];
                if (!pattern.IsAlive)
                {
                    continue;
                }

                IEnumerable<WorldPatternCell> occupiedCells = pattern.GetOccupiedCells();

                foreach (WorldPatternCell worldCell in occupiedCells)
                {
                    Vector2Int index = new Vector2Int(worldCell.Index.X, worldCell.Index.Y);

                    WorldPatternCell existing;
                    if (resolvedCells.TryGetValue(index, out existing))
                    {
                        if (worldCell.Layer >= existing.Layer)
                        {
                            resolvedCells[index] = worldCell;
                            resolvedOwners[index] = pattern;
                        }
                    }
                    else
                    {
                        resolvedCells.Add(index, worldCell);
                        resolvedOwners.Add(index, pattern);
                    }
                }
            }

            // 2. Build a lookup: grid index -> ITouchCell.
            Dictionary<Vector2Int, ITouchCell> cellByIndex =
                new Dictionary<Vector2Int, ITouchCell>();

            for (int i = 0; i < allCells.Count; i++)
            {
                ITouchCell cell = allCells[i];
                Vector2Int index = cell.Position;

                // If there are multiple cells per index, last one wins, but your grid
                // should normally have exactly one cell per index.
                cellByIndex[index] = cell;
            }

            // 3. Apply resolved states and record ownership (only when changed).
            foreach (KeyValuePair<Vector2Int, WorldPatternCell> pair in resolvedCells)
            {
                Vector2Int index = pair.Key;

                ITouchCell cell;
                if (!cellByIndex.TryGetValue(index, out cell))
                {
                    continue;
                }

                WorldPatternCell patternCell = pair.Value;

                if (cell.role != patternCell.Role)
                {
                    cell.SetRole(patternCell.Role);
                }

                if (cell.color != patternCell.Color)
                {
                    cell.SetColor(patternCell.Color);
                }

                PatternInstance owner;
                if (resolvedOwners.TryGetValue(index, out owner))
                {
                    _cellOwners[cell] = owner;
                }
            }

            // 4. Any cell not covered by a pattern this frame should be None/Black.
            for (int i = 0; i < allCells.Count; i++)
            {
                ITouchCell cell = allCells[i];
                Vector2Int index = cell.Position;

                if (resolvedCells.ContainsKey(index))
                {
                    continue;
                }

                if (cell.role != CellRole.None)
                {
                    cell.SetRole(CellRole.None);
                }

                if (cell.color != CellColor.Black)
                {
                    cell.SetColor(CellColor.Black);
                }

                _cellOwners.Remove(cell);
            }
        }
    }
}