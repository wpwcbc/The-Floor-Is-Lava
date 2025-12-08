using System.Collections.Generic;
using UnityEngine;

public class OverlapDetector : MonoBehaviour
{
    private const string ScriptName = "OverlapDetector.cs";

    private void Update()
    {
        CurrentPointsProvider pointsProvider = CurrentPointsProvider.Instance;
        if (pointsProvider == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentPointsProvider.Instance is null.");
            return;
        }

        CurrentCellsProvider cellsProvider = CurrentCellsProvider.Instance;
        if (cellsProvider == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentCellsProvider.Instance is null.");
            return;
        }

        List<TouchPoint> points = pointsProvider.CurrentPoints;
        if (points == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentPointsProvider.CurrentPoints is null.");
            return;
        }

        IReadOnlyList<ITouchCell> allCells = cellsProvider.CurrentCells;
        if (allCells == null || allCells.Count == 0)
        {
            return;
        }

        List<ITouchCell> remainingCells = new List<ITouchCell>(allCells);

        foreach (TouchPoint touchPoint in points)
        {
            Vector2Int gridIndex = GridMathUtils.PixelToGridIndex(touchPoint.Position);

            ITouchCell cell;
            if (!cellsProvider.TryGetCell(gridIndex, out cell))
            {
                continue;
            }

            if (!cell.IsTouched)
            {
                cell.SetIsTouched(true);
            }

            remainingCells.Remove(cell);
        }

        foreach (ITouchCell cell in remainingCells)
        {
            if (cell.IsTouched)
            {
                cell.SetIsTouched(false);
            }
        }
    }
}
