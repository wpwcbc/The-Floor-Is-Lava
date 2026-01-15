using System.Collections.Generic;
using UnityEngine;

public class OverlapDetector : MonoBehaviour
{
    private const string ScriptName = "OverlapDetector.cs";

    [Header("UI")]
    [SerializeField]
    private Canvas gridCanvas;   // Canvas that contains your TouchCellUI grid

    private RectTransform _canvasRect;
    private Camera _canvasCamera;
    private Vector2 _canvasHalfSize;

    private void Awake()
    {
        if (gridCanvas == null)
        {
            gridCanvas = GetComponentInParent<Canvas>();
        }

        if (gridCanvas == null)
        {
            Debug.LogError($"[{ScriptName}] gridCanvas is not assigned and no Canvas found.");
            enabled = false;
            return;
        }

        _canvasRect = gridCanvas.transform as RectTransform;
        if (_canvasRect == null)
        {
            Debug.LogError($"[{ScriptName}] gridCanvas does not have a RectTransform.");
            enabled = false;
            return;
        }

        _canvasCamera = gridCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : gridCanvas.worldCamera;

        // Local space of the canvas has origin at its pivot (usually center),
        // so we cache half size to convert that to a bottom-left origin.
        _canvasHalfSize = _canvasRect.rect.size * 0.5f;
    }

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
            // 1. Screen space (TouchPoint.Position) → canvas local (origin at canvas pivot)
            Vector2 localOnCanvas;
            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                touchPoint.Position,
                _canvasCamera,
                out localOnCanvas);

            if (!ok)
            {
                continue;
            }

            // 2. Convert from pivot-centered coords to bottom-left–origin coords
            //    so it matches your GridMathUtils / TouchCellUI convention.
            Vector2 canvasBottomLeftPos = localOnCanvas + _canvasHalfSize;

            // 3. Now use the grid math (expects "canvas pixels" with bottom-left origin)
            Vector2Int gridIndex = GridMathUtils.PixelToGridIndex(canvasBottomLeftPos);

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
