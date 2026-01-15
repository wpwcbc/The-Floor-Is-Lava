using UnityEngine;

public interface ICellRoleBehavior
{
    void OnTouched(Vector2Int worldIndex, CellRole role, CellColor color);
    void OnUntouched(Vector2Int worldIndex, CellRole role, CellColor color);
}


public sealed class ForbiddenRoleBehavior : ICellRoleBehavior
{
    private readonly GridFlashEffectSystem flashSystem;

    public ForbiddenRoleBehavior(GridFlashEffectSystem flashSystem)
    {
        this.flashSystem = flashSystem;
    }

    public void OnTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (flashSystem == null)
        {
            return;
        }

        flashSystem.TriggerForbiddenHitFlashAt(worldIndex);
    }

    public void OnUntouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        // Maybe clear effect, maybe nothing
    }
}

public sealed class WeaknessRoleBehavior : ICellRoleBehavior
{
    private readonly PatternRuntimeManager patternRuntimeManager;

    public WeaknessRoleBehavior(PatternRuntimeManager patternRuntimeManager)
    {
        this.patternRuntimeManager = patternRuntimeManager;
    }

    public void OnTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (patternRuntimeManager == null)
        {
            return;
        }

        patternRuntimeManager.KillPatternAtIndex(worldIndex);
    }

    public void OnUntouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        // no-op
    }
}