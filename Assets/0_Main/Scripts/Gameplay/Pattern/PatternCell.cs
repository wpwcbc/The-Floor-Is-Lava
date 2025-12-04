public struct LocalPatternCell
{
    public CellOffset Offset;
    public CellRole Role;
    public CellColor Color;

    public LocalPatternCell(CellOffset offset, CellRole role, CellColor color)
    {
        Offset = offset;
        Role = role;
        Color = color;
    }
}

public struct WorldPatternCell
{
    public GridIndex Index;
    public CellRole Role;
    public CellColor Color;
    public int Layer;

    public WorldPatternCell(GridIndex index, CellRole role, CellColor color, int layer)
    {
        Index = index;
        Role = role;
        Color = color;
        Layer = layer;
    }
}