using System.Collections.Generic;

public static class PatternPool
{
    public static PatternDefinition CreateSafeCenterWithForbiddenRingPattern()
    {
        // Define local cells around the origin
        List<LocalPatternCell> cells = new List<LocalPatternCell>
        {
            // Center cell: Safe
            new LocalPatternCell(
                new CellOffset(0, 0),
                CellRole.Safe,
                CellColor.Green),

            // Right: Forbidden
            new LocalPatternCell(
                new CellOffset(1, 0),
                CellRole.Forbidden,
                CellColor.Red),

            // Up: Forbidden
            new LocalPatternCell(
                new CellOffset(0, 1),
                CellRole.Forbidden,
                CellColor.Red),

            // Left: Forbidden
            new LocalPatternCell(
                new CellOffset(-1, 0),
                CellRole.Forbidden,
                CellColor.Red),
        };

        PatternFrame frame = new PatternFrame(cells);

        PatternDefinition definition =
            PatternFactory.CreateSingleFramePattern(
                "SafeCenter_ForbiddenRing_3Arms",
                frame);

        return definition;
    }

    public static PatternDefinition LavaRipple()
    {
        List<PatternFrame> frames = new List<PatternFrame>();

        // Frame 0: single center cell (radius 0)
        List<LocalPatternCell> frame0Cells = new List<LocalPatternCell>
        {
            new LocalPatternCell(
                new CellOffset(0, 0),
                CellRole.Forbidden,
                CellColor.Red)
        };
        frames.Add(new PatternFrame(frame0Cells));

        // Frame 1: ring at radius 1 (4 cells around center)
        frames.Add(PatternFactory.CreateRingFrame(
            1,
            CellRole.Forbidden,
            CellColor.Red));

        // Frame 2: ring at radius 2
        frames.Add(PatternFactory.CreateRingFrame(
            2,
            CellRole.Forbidden,
            CellColor.Red));

        // Frame 3: ring at radius 3
        frames.Add(PatternFactory.CreateRingFrame(
            3,
            CellRole.Forbidden,
            CellColor.Red));

        // Frame 4: empty -> decay to none
        List<LocalPatternCell> emptyCells = new List<LocalPatternCell>();
        frames.Add(new PatternFrame(emptyCells));

        PatternDefinition definition = new PatternDefinition(
            "LavaRipple",
            frames);

        return definition;
    }

    // Specific: 3 x GridHeight vertical bar of red forbidden cells
    public static PatternDefinition CreateVerticalLavaBarPattern(int gridHeight)
    {
        PatternDefinition definition = PatternFactory.CreateSolidRectPattern(
            "VerticalLavaBar_3xGridHeight",
            3,                    // width in cells
            gridHeight,           // height in cells
            CellRole.Forbidden,
            CellColor.Red);

        return definition;
    }

    // Specific: GREEN SAFE RING with grid size and ring width 3
    public static PatternDefinition CreateSafeGridRingPattern(
        int gridWidth,
        int gridHeight,
        int ringWidth)
    {
        PatternDefinition definition = PatternFactory.CreateHollowRectPattern(
            "SafeGridRing",
            gridWidth,
            gridHeight,
            ringWidth,
            CellRole.Safe,
            CellColor.Green);

        return definition;
    }

    public static PatternDefinition CreateWeaknessPointPattern()
    {
        List<LocalPatternCell> cells = new List<LocalPatternCell>
        {
            new LocalPatternCell(
                new CellOffset(0, 0),
                CellRole.Weakness,
                CellColor.Blue)
        };

        PatternFrame frame = new PatternFrame(cells);

        PatternDefinition definition =
            PatternFactory.CreateSingleFramePattern(
                "Point",
                frame);

        return definition;
    }
}
