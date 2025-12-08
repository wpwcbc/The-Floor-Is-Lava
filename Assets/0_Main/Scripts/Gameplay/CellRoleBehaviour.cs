using UnityEngine;

public interface ICellRoleBehavior
{
    void OnTouched(ITouchCell cell);
    void OnUntouched(ITouchCell cell);
}

public sealed class ForbiddenRoleBehavior : ICellRoleBehavior
{
    private readonly PatternRuntimeManager patternRuntimeManager;
    private readonly GridFlashEffectSystem flashSystem;

    public ForbiddenRoleBehavior(
        PatternRuntimeManager patternRuntimeManager,
        GridFlashEffectSystem flashSystem)
    {
        this.patternRuntimeManager = patternRuntimeManager;
        this.flashSystem = flashSystem;
    }

    public void OnTouched(ITouchCell cell)
    {
        // if (patternRuntimeManager != null)
        // {
        //     patternRuntimeManager.ApplySlowMotion(0.5f, 0.0f); // pause movement
        // }

        if (flashSystem != null)
        {
            flashSystem.TriggerForbiddenHitFlash(cell);
        }
    }

    public void OnUntouched(ITouchCell cell)
    {
    }
}


public sealed class WeaknessRoleBehavior : ICellRoleBehavior
{
    private readonly PatternRuntimeManager patternRuntimeManager;

    public WeaknessRoleBehavior(PatternRuntimeManager patternRuntimeManager)
    {
        this.patternRuntimeManager = patternRuntimeManager;
    }

    public void OnTouched(ITouchCell cell)
    {
        if (patternRuntimeManager == null)
        {
            return;
        }

        patternRuntimeManager.KillPatternAtCell(cell);
    }

    public void OnUntouched(ITouchCell cell)
    {
    }
}
