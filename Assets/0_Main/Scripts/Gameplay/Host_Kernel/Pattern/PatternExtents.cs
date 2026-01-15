using System.Collections.Generic;

public static class PatternExtents
{
    public static void GetOffsetExtents(
        PatternDefinition definition,
        out int minOffsetX,
        out int maxOffsetX,
        out int minOffsetY,
        out int maxOffsetY)
    {
        if (definition == null || definition.Frames == null || definition.Frames.Count == 0)
        {
            minOffsetX = 0;
            maxOffsetX = 0;
            minOffsetY = 0;
            maxOffsetY = 0;
            return;
        }

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        bool foundAnyCell = false;

        int frameCount = definition.Frames.Count;
        for (int f = 0; f < frameCount; f++)
        {
            PatternFrame frame = definition.Frames[f];
            if (frame == null || frame.Cells == null)
            {
                continue;
            }

            IReadOnlyList<LocalPatternCell> cells = frame.Cells;

            int cellCount = cells.Count;
            for (int i = 0; i < cellCount; i++)
            {
                LocalPatternCell cell = cells[i];

                int ox = cell.Offset.DeltaX;
                int oy = cell.Offset.DeltaY;

                if (!foundAnyCell)
                {
                    foundAnyCell = true;
                    minX = ox;
                    maxX = ox;
                    minY = oy;
                    maxY = oy;
                }
                else
                {
                    if (ox < minX) minX = ox;
                    if (ox > maxX) maxX = ox;
                    if (oy < minY) minY = oy;
                    if (oy > maxY) maxY = oy;
                }
            }
        }

        if (!foundAnyCell)
        {
            minOffsetX = 0;
            maxOffsetX = 0;
            minOffsetY = 0;
            maxOffsetY = 0;
            return;
        }

        minOffsetX = minX;
        maxOffsetX = maxX;
        minOffsetY = minY;
        maxOffsetY = maxY;
    }
}
