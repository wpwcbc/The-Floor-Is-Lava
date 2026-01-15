using System.Collections.Generic;
using UnityEngine;

public static class CustomLevelPatternBuilder
{
    public static PatternFrame BuildFrameFromCells(
        CustomLevelDataModel.Frame frameData,
        int gridWidth,
        int gridHeight,
        CellRole role,
        CellColor color)
    {
        if (frameData == null)
        {
            Debug.LogError("[CustomLevelPatternBuilder] frameData is null.");
            return new PatternFrame(new List<LocalPatternCell>());
        }

        if (frameData.cells == null)
        {
            Debug.LogError("[CustomLevelPatternBuilder] frameData.cells is null.");
            return new PatternFrame(new List<LocalPatternCell>());
        }

        HashSet<Vector2Int> used = new HashSet<Vector2Int>();
        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int i = 0; i < frameData.cells.Count; i++)
        {
            CustomLevelDataModel.Cell c = frameData.cells[i];
            if (c == null)
            {
                continue;
            }

            int x = c.x;
            int y = c.y;

            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                continue;
            }

            Vector2Int key = new Vector2Int(x, y);
            if (!used.Add(key))
            {
                continue;
            }

            CellOffset offset = new CellOffset(x, y);
            LocalPatternCell local = new LocalPatternCell(offset, role, color);
            cells.Add(local);
        }

        return new PatternFrame(cells);
    }

    public static PatternDefinition BuildDefinitionFromFrames(
        string id,
        List<PatternFrame> frames)
    {
        if (frames == null || frames.Count == 0)
        {
            Debug.LogError("[CustomLevelPatternBuilder] frames is null/empty for definition: " + id);
            frames = new List<PatternFrame> { new PatternFrame(new List<LocalPatternCell>()) };
        }

        return new PatternDefinition(id, frames);
    }
}
