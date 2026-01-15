using System.Collections.Generic;

public class CustomLevelDataModel
{
    public string id; // Identifier. Non editable
    public string name; // Level name, editable
    public int gridWidth; // Non editable
    public int gridHeight; // Non editable
    public float defaultFrameCooldownSeconds; // default/suggested cd when first load, still can be adjust in a level instance. Editable

    public Frame SafePattern;
    public List<Frame> ForbiddenFrames;
    public List<Cell> WeaknessCells;

    [System.Serializable]
    public sealed class Cell
    {
        public int x;
        public int y;
    }

    [System.Serializable]
    public sealed class Frame
    {
        public List<Cell> cells;
    }
}
