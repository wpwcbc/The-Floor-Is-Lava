using UnityEngine;

public static class GridMathUtils
{
    // pointPos MUST be in canvas-local coordinates (same as RectTransform.anchoredPosition)
    public static bool IsPointInCell(Vector2 pointPos, Vector2Int cellPos, int sizeInCells = 1)
    {
        int cellSizePx = GridConfig.CellSize;

        Vector2 min = (Vector2)GridConfig.GridOrigin + new Vector2(
            cellPos.x * cellSizePx,
            cellPos.y * cellSizePx
        );

        Vector2 max = min + new Vector2(
            sizeInCells * cellSizePx,
            sizeInCells * cellSizePx
        );

        return pointPos.x >= min.x &&
               pointPos.x < max.x &&
               pointPos.y >= min.y &&
               pointPos.y < max.y;
    }

    public static Vector2Int GridToPixelOrigin(Vector2Int cellPos)
    {
        int cellSizePx = GridConfig.CellSize;
        return GridConfig.GridOrigin + new Vector2Int(
            cellPos.x * cellSizePx,
            cellPos.y * cellSizePx
        );
    }

    // pointPos MUST be in canvas-local coordinates (not raw screen pixels)
    public static Vector2Int PixelToGridIndex(Vector2 pointPos)
    {
        int cellSizePx = GridConfig.CellSize;
        Vector2 offset = pointPos - (Vector2)GridConfig.GridOrigin;

        int x = Mathf.FloorToInt(offset.x / cellSizePx);
        int y = Mathf.FloorToInt(offset.y / cellSizePx);

        return new Vector2Int(x, y);
    }
}
