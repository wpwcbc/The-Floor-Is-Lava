using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GridFlashEffectSystem : MonoBehaviour
{
    [SerializeField]
    private PatternRuntimeManager patternRuntimeManager;

    [SerializeField]
    private float flashDuration = 0.5f;

    [SerializeField]
    private int flashCycles = 3;

    private Coroutine activeFlash;

    public void TriggerForbiddenHitFlash(ITouchCell hitCell)
    {
        // if (activeFlash != null)
        // {
        //     StopCoroutine(activeFlash);
        // }

        activeFlash = StartCoroutine(FlashRoutine(hitCell));
    }

    private IEnumerator FlashRoutine(ITouchCell hitCell)
    {
        // Freeze logic while beeping.
        if (patternRuntimeManager != null)
        {
            patternRuntimeManager.ApplySlowMotion(flashDuration, 0.0f);
        }

        CurrentCellsProvider provider = CurrentCellsProvider.Instance;
        if (provider == null)
        {
            yield break;
        }

        IReadOnlyList<ITouchCell> cells = provider.CurrentCells;
        if (cells == null)
        {
            yield break;
        }

        TouchCellUI hitUi = hitCell as TouchCellUI;

        float singleCycleDuration = flashDuration / flashCycles;
        float halfCycle = singleCycleDuration * 0.5f;

        for (int cycle = 0; cycle < flashCycles; cycle++)
        {
            // Phase 1: effect on
            for (int i = 0; i < cells.Count; i++)
            {
                TouchCellUI ui = cells[i] as TouchCellUI;
                if (ui == null)
                {
                    continue;
                }

                if (cells[i] == hitCell)
                {
                    ui.SetEffectTint(Color.white);
                }
                else
                {
                    ui.SetEffectTint(Color.darkRed);
                }
            }

            yield return new WaitForSeconds(halfCycle);

            // Phase 2: effect off (restore original)
            for (int i = 0; i < cells.Count; i++)
            {
                TouchCellUI ui = cells[i] as TouchCellUI;
                if (ui == null)
                {
                    continue;
                }

                ui.ClearEffectTint();
            }

            yield return new WaitForSeconds(halfCycle);
        }

        // Ensure cleared.
        for (int i = 0; i < cells.Count; i++)
        {
            TouchCellUI ui = cells[i] as TouchCellUI;
            if (ui != null)
            {
                ui.ClearEffectTint();
            }
        }

        activeFlash = null;
    }
}
