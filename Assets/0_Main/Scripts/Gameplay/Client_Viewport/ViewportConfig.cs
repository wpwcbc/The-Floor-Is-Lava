using UnityEngine;

[System.Serializable]
public struct ViewportConfig
{
    // Bottom-left in world grid.
    public Vector2Int WorldOrigin;

    // Bottom-left in local grid (ITouchCell.Position space).
    public Vector2Int LocalOrigin;

    // Width/height in cells.
    public Vector2Int Size;

    // Placeholder for future use (rotation, mirroring, etc.).
    public int Direction;

    public bool IsLocalIndexInViewport(Vector2Int localIndex)
    {
        int minX = LocalOrigin.x;
        int minY = LocalOrigin.y;
        int maxX = LocalOrigin.x + Size.x - 1;
        int maxY = LocalOrigin.y + Size.y - 1;

        if (localIndex.x < minX || localIndex.x > maxX)
        {
            return false;
        }

        if (localIndex.y < minY || localIndex.y > maxY)
        {
            return false;
        }

        return true;
    }

    public Vector2Int LocalToWorld(Vector2Int localIndex)
    {
        int worldX = WorldOrigin.x + (localIndex.x - LocalOrigin.x);
        int worldY = WorldOrigin.y + (localIndex.y - LocalOrigin.y);

        Vector2Int index = new Vector2Int(worldX, worldY);
        return index;
    }

    public bool TryWorldToLocal(Vector2Int worldIndex, out Vector2Int localIndex)
    {
        int minWorldX = WorldOrigin.x;
        int minWorldY = WorldOrigin.y;
        int maxWorldX = WorldOrigin.x + Size.x - 1;
        int maxWorldY = WorldOrigin.y + Size.y - 1;

        if (worldIndex.x < minWorldX || worldIndex.x > maxWorldX ||
            worldIndex.y < minWorldY || worldIndex.y > maxWorldY)
        {
            localIndex = default(Vector2Int);
            return false;
        }

        int localX = LocalOrigin.x + (worldIndex.x - WorldOrigin.x);
        int localY = LocalOrigin.y + (worldIndex.y - WorldOrigin.y);

        localIndex = new Vector2Int(localX, localY);
        return true;
    }
}
