using System.Collections.Generic;

public static class PatternFactory
{
    // -----------------------------
    // FRAME HELPERS
    // -----------------------------

    public static PatternFrame CreateRingFrame(
        int radius,
        CellRole role,
        CellColor color)
    {
        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int manhattanDistance = System.Math.Abs(dx) + System.Math.Abs(dy);
                if (manhattanDistance == radius)
                {
                    CellOffset offset = new CellOffset(dx, dy);
                    LocalPatternCell cell = new LocalPatternCell(
                        offset,
                        role,
                        color);

                    cells.Add(cell);
                }
            }
        }

        PatternFrame frame = new PatternFrame(cells);
        return frame;
    }

    public static PatternFrame CreateSolidRectFrame(
        int width,
        int height,
        CellRole role,
        CellColor color)
    {
        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellOffset offset = new CellOffset(x, y);
                LocalPatternCell cell = new LocalPatternCell(
                    offset,
                    role,
                    color);

                cells.Add(cell);
            }
        }

        PatternFrame frame = new PatternFrame(cells);
        return frame;
    }

    public static PatternFrame CreateHollowRectFrame(
        int width,
        int height,
        int ringWidth,
        CellRole role,
        CellColor color)
    {
        if (ringWidth <= 0)
        {
            ringWidth = 1;
        }

        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isInOuter =
                    x >= 0 && x < width &&
                    y >= 0 && y < height;

                int innerMinX = ringWidth;
                int innerMaxX = width - ringWidth - 1;
                int innerMinY = ringWidth;
                int innerMaxY = height - ringWidth - 1;

                bool hasInner =
                    innerMinX <= innerMaxX &&
                    innerMinY <= innerMaxY;

                bool isInInner =
                    hasInner &&
                    x >= innerMinX && x <= innerMaxX &&
                    y >= innerMinY && y <= innerMaxY;

                // Ring cells = in outer rectangle but not in inner rectangle
                if (isInOuter && !isInInner)
                {
                    CellOffset offset = new CellOffset(x, y);
                    LocalPatternCell cell = new LocalPatternCell(
                        offset,
                        role,
                        color);

                    cells.Add(cell);
                }
            }
        }

        PatternFrame frame = new PatternFrame(cells);
        return frame;
    }

    // -----------------------------
    // DEFINITION HELPERS
    // -----------------------------

    public static PatternDefinition CreateSingleFramePattern(
        string id,
        PatternFrame frame)
    {
        List<PatternFrame> frames = new List<PatternFrame>
        {
            frame
        };

        PatternDefinition definition = new PatternDefinition(id, frames);
        return definition;
    }

    public static PatternDefinition CreateSolidRectPattern(
        string id,
        int width,
        int height,
        CellRole role,
        CellColor color)
    {
        PatternFrame frame = CreateSolidRectFrame(width, height, role, color);
        PatternDefinition definition = CreateSingleFramePattern(id, frame);
        return definition;
    }

    public static PatternDefinition CreateHollowRectPattern(
        string id,
        int width,
        int height,
        int ringWidth,
        CellRole role,
        CellColor color)
    {
        PatternFrame frame = CreateHollowRectFrame(width, height, ringWidth, role, color);
        PatternDefinition definition = CreateSingleFramePattern(id, frame);
        return definition;
    }
}
