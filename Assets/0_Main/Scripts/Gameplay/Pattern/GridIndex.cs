public struct CellOffset
{
    public int DeltaX;
    public int DeltaY;

    public CellOffset(int deltaX, int deltaY)
    {
        DeltaX = deltaX;
        DeltaY = deltaY;
    }
}

public struct GridIndex
{
    public int X;
    public int Y;

    public GridIndex(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static GridIndex operator +(GridIndex index, CellOffset offset)
    {
        return new GridIndex(index.X + offset.DeltaX, index.Y + offset.DeltaY);
    }
}