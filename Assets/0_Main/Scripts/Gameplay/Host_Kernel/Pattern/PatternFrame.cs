using System.Collections.Generic;

public sealed class PatternFrame
{
    public IReadOnlyList<LocalPatternCell> Cells { get; private set; }

    public PatternFrame(IReadOnlyList<LocalPatternCell> cells)
    {
        Cells = cells;
    }
}
