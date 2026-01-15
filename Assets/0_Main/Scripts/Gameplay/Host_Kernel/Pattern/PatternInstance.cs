using System;
using System.Collections.Generic;

public sealed class PatternInstance
{
    public PatternDefinition Definition { get; private set; }
    public GridIndex Origin { get; private set; }
    public int CurrentFrameIndex { get; private set; }
    public int Layer { get; private set; }

    private IPatternUpdateLogic _updateLogic;

    public IPatternUpdateLogic UpdateLogic
    {
        get { return _updateLogic; }
    }

    public bool IsAlive { get; private set; } = true;

    public event Action<PatternInstance> Killed;

    public PatternInstance(
        PatternDefinition definition,
        GridIndex origin,
        int layer,
        IPatternUpdateLogic updateLogic)
    {
        Definition = definition;
        Origin = origin;
        CurrentFrameIndex = 0;
        Layer = layer;
        _updateLogic = updateLogic;
    }

    public IEnumerable<WorldPatternCell> GetOccupiedCells()
    {
        PatternFrame frame = Definition.Frames[CurrentFrameIndex];

        foreach (LocalPatternCell localCell in frame.Cells)
        {
            GridIndex worldIndex = Origin + localCell.Offset;

            yield return new WorldPatternCell(
                worldIndex,
                localCell.Role,
                localCell.Color,
                Layer);
        }
    }

    public void SetOrigin(GridIndex origin)
    {
        Origin = origin;
    }

    public void MoveBy(CellOffset offset)
    {
        Origin = Origin + offset;
    }

    public void SetFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= Definition.Frames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        CurrentFrameIndex = frameIndex;
    }

    public void NextFrame()
    {
        int next = CurrentFrameIndex + 1;

        if (next >= Definition.Frames.Count)
        {
            next = 0;
        }

        CurrentFrameIndex = next;
    }

    public void Tick(float deltaTime)
    {
        if (!IsAlive)
        {
            return;
        }

        if (_updateLogic == null)
        {
            return;
        }

        _updateLogic.Tick(this, deltaTime);
    }

    internal void Kill()
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;

        Action<PatternInstance> handler = Killed;
        if (handler != null)
        {
            handler(this);
        }
    }
}
