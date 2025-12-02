using UnityEngine;

public static class GridConfig
{
    // Unit in screen pixels
    public static readonly Vector2Int GridOrigin = new Vector2Int(0, 0); // In screen space
    public static readonly int CellSize = 60;
    public static int CellRadius => CellSize / 2;
    public static readonly Vector2Int GridRect = new Vector2Int(32, 18);
}
