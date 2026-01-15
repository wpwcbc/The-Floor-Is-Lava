using System.Collections.Generic;
using UnityEngine;

public static class PatternLevelHelpers
{
    public static List<Vector2Int> BuildCornerSquareOrigins(int gridWidth, int gridHeight, int size)
    {
        int maxX = gridWidth - size;
        int maxY = gridHeight - size;

        List<Vector2Int> origins = new List<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(maxX, 0),
            new Vector2Int(0, maxY),
            new Vector2Int(maxX, maxY)
        };

        return Dedup(origins);
    }

    public static List<Vector2Int> BuildSideMidSquareOrigins(int gridWidth, int gridHeight, int size)
    {
        int maxX = gridWidth - size;
        int maxY = gridHeight - size;

        int midOriginX = Mathf.Clamp((gridWidth - size) / 2, 0, maxX);
        int midOriginY = Mathf.Clamp((gridHeight - size) / 2, 0, maxY);

        List<Vector2Int> origins = new List<Vector2Int>
        {
            new Vector2Int(maxX - midOriginX, 0),     // bottom
            new Vector2Int(midOriginX, maxY),  // top
            new Vector2Int(0, midOriginY),     // left
            new Vector2Int(maxX, maxY - midOriginY),  // right
        };

        return Dedup(origins);
    }

    public static List<Vector2Int> Dedup(List<Vector2Int> input)
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < input.Count; i++)
        {
            Vector2Int p = input[i];
            if (set.Add(p))
            {
                result.Add(p);
            }
        }

        return result;
    }
}
