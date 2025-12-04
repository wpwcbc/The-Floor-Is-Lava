using System.Collections.Generic;
using UnityEngine;

public enum CellColor
{
    Black = 0,
    Green = 1,
    Red = 2,
    // Add more colors as needed
}

public static class CellColorSprites
{
    private static readonly Dictionary<CellColor, Sprite> _sprites;

    public static IReadOnlyDictionary<CellColor, Sprite> Sprites => _sprites;

    static CellColorSprites()
    {
        _sprites = new Dictionary<CellColor, Sprite>();

        AddSprite(CellColor.Black, "Sprites/CellColor/cell-black");
        AddSprite(CellColor.Green, "Sprites/CellColor/cell-green");
        AddSprite(CellColor.Red, "Sprites/CellColor/cell-red");
        // Add more mappings here as you add colors
    }

    private static void AddSprite(CellColor color, string resourcesPath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcesPath);
        if (sprite == null)
        {
            Debug.LogError(
                "[CellColorSprites] Failed to load sprite at path: " + resourcesPath +
                " for color: " + color
            );
            return;
        }

        _sprites[color] = sprite;
    }

    public static Sprite GetSprite(CellColor color)
    {
        Sprite sprite;
        if (!_sprites.TryGetValue(color, out sprite))
        {
            Debug.LogError("[CellColorSprites] No sprite registered for color: " + color);
            return null;
        }

        return sprite;
    }
}
