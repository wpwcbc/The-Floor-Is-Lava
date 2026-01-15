using System.Collections.Generic;
using UnityEngine;

public enum CellColor
{
    Black = 0,
    Green = 1,
    Red = 2,
    Blue = 3,
    Gray = 4,
    // Add more colors as needed
}

public static class CellColorSprites
{
    private static readonly Dictionary<CellColor, Color> _colors;

    public static IReadOnlyDictionary<CellColor, Color> Colors
    {
        get { return _colors; }
    }

    static CellColorSprites()
    {
        _colors = new Dictionary<CellColor, Color>();

        // You can tweak these values later (e.g. use ColorUtility.FromHtmlString for custom shades).
        _colors[CellColor.Black] = Color.black;
        _colors[CellColor.Green] = Color.green;
        _colors[CellColor.Red] = Color.red;
        _colors[CellColor.Blue] = Color.blue;
        _colors[CellColor.Gray] = Color.gray;
    }

    public static Color GetColor(CellColor color)
    {
        Color result;
        if (!_colors.TryGetValue(color, out result))
        {
            Debug.LogError("[CellColorSprites] No Color registered for CellColor: " + color);
            return Color.magenta; // obvious debugging color
        }

        return result;
    }
}
