using System.Collections.Generic;
using UnityEngine;

public class OverlapDetector : MonoBehaviour
{
    private const string ScriptName = "OverlapDetector.cs";

    private void Update()
    {
        var pointsProvider = CurrentPointsProvider.Instance;
        if (pointsProvider == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentPointsProvider.Instance is null.");
            return;
        }

        var cellsProvider = CurrentCellsProvider.Instance;
        if (cellsProvider == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentCellsProvider.Instance is null.");
            return;
        }

        var points = pointsProvider.CurrentPoints;
        if (points == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentPointsProvider.CurrentPoints is null.");
            return;
        }

        // Shallow copy current cells so we can remove those that are hit this frame
        IReadOnlyList<ITouchCell> allCells = cellsProvider.CurrentCells;
        // If there are zero cells, nothing to do
        if (allCells == null || allCells.Count == 0)
            return;

        List<ITouchCell> remainingCells = new List<ITouchCell>(allCells);

        // 1. Process all touch points
        foreach (TouchPoint touchPoint in points)
        {
            // 1.1 Map point to grid index
            Vector2Int gridIndex = GridMathUtils.PixelToGridIndex(touchPoint.Position);

            // 1.2 Look up cell at this index
            if (!cellsProvider.TryGetCell(gridIndex, out ITouchCell cell))
                continue;

            if (!cell.IsSensitive)
                continue;

            // 1.3 If not already touched, mark as touched
            if (!cell.IsTouched)
            {
                cell.SetIsTouched(true);
            }

            // 1.4 Remove this cell from remainingCells so we don't reset it to false later
            remainingCells.Remove(cell);
        }

        // 2. Any cell not hit by any point this frame should be set to not touched
        foreach (ITouchCell cell in remainingCells)
        {
            if (!cell.IsSensitive)
                continue;

            // Only set when the state has chenged
            if (cell.IsTouched)
            {
                cell.SetIsTouched(false);
            }
        }
    }
}
