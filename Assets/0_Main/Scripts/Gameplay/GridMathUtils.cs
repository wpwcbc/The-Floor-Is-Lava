using UnityEngine;

public static class GridMathUtils
{
    /// <summary>
    /// Returns true if a point (in screen pixels) is inside a cell's rectangle
    /// defined by its grid index and size in tiles.
    /// </summary>
    /// <param name="pointPos">Point in screen-space pixels (bottom-left origin).</param>
    /// <param name="cellPos">Cell grid index (0,0 at GridConfig.GridOrigin).</param>
    /// <param name="sizeInCells">Span of the cell in tiles. 1 = 1x1, 2 = 2x2, etc.</param>
    public static bool IsPointInCell(Vector2 pointPos, Vector2Int cellPos, int sizeInCells = 1)
    {
        int cellSizePx = GridConfig.CellSize;

        // Bottom-left corner of the cell block in pixels
        Vector2 min = (Vector2)GridConfig.GridOrigin + new Vector2(
            cellPos.x * cellSizePx,
            cellPos.y * cellSizePx
        );

        // Top-right corner of the cell block in pixels
        Vector2 max = min + new Vector2(
            sizeInCells * cellSizePx,
            sizeInCells * cellSizePx
        );

        // Standard half-open rectangle test [min, max)
        return pointPos.x >= min.x &&
               pointPos.x < max.x &&
               pointPos.y >= min.y &&
               pointPos.y < max.y;
    }

    /// <summary>
    /// Converts a grid index (i,j) to the bottom-left pixel coordinate of that cell.
    /// </summary>
    public static Vector2Int GridToPixelOrigin(Vector2Int cellPos)
    {
        int cellSizePx = GridConfig.CellSize;
        return GridConfig.GridOrigin + new Vector2Int(
            cellPos.x * cellSizePx,
            cellPos.y * cellSizePx
        );
    }

    /// <summary>
    /// Converts a screen-space pixel position into a grid index.
    /// </summary>
    public static Vector2Int PixelToGridIndex(Vector2 pointPos)
    {
        int cellSizePx = GridConfig.CellSize;
        Vector2 offset = pointPos - (Vector2)GridConfig.GridOrigin;

        int x = Mathf.FloorToInt(offset.x / cellSizePx);
        int y = Mathf.FloorToInt(offset.y / cellSizePx);

        return new Vector2Int(x, y);
    }
}
