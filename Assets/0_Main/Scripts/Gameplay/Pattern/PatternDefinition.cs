using System.Collections.Generic;

public sealed class PatternDefinition
{

    public string Id { get; private set; } // e.g. "LShape_1"

    public IReadOnlyList<PatternFrame> Frames { get; private set; }

    public PatternDefinition(
        string id,
        IReadOnlyList<PatternFrame> frames
    )
    {
        Id = id;
        Frames = frames;
    }
}
