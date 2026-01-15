using System.Collections;
using UnityEngine;

public sealed class GridFlashEffectSystem : MonoBehaviour
{
    [SerializeField]
    private PatternRuntimeManager patternRuntimeManager;

    [SerializeField]
    private PatternToGridResolver gridResolver;

    [SerializeField]
    private float flashDuration = 0.5f;

    [SerializeField]
    private int flashCycles = 3;

    private Coroutine activeFlash;

    private bool hasLoggedMissingResolver;
    private const string ScriptName = "GridFlashEffectSystem";

    private void Awake()
    {
        if (patternRuntimeManager == null)
        {
            patternRuntimeManager = FindFirstObjectByType<PatternRuntimeManager>();
        }

        if (gridResolver == null)
        {
            gridResolver = FindFirstObjectByType<PatternToGridResolver>();
        }

        if (gridResolver == null)
        {
            Debug.LogError($"[{ScriptName}] PatternToGridResolver is not assigned or found in the scene.", this);
            hasLoggedMissingResolver = true;
        }
    }

    /// <summary>
    /// Called by ForbiddenRoleBehavior when a forbidden world cell is touched.
    /// </summary>
    public void TriggerForbiddenHitFlashAt(Vector2Int hitWorldIndex)
    {
        if (gridResolver == null)
        {
            if (!hasLoggedMissingResolver)
            {
                Debug.LogError($"[{ScriptName}] TriggerForbiddenHitFlashAt called but gridResolver is null.", this);
                hasLoggedMissingResolver = true;
            }

            return;
        }

        if (!gridResolver.IsIndexInBounds(hitWorldIndex))
        {
            Debug.LogError($"[{ScriptName}] TriggerForbiddenHitFlashAt called with out-of-bounds index {hitWorldIndex}.", this);
            return;
        }

        if (activeFlash != null)
        {
            StopCoroutine(activeFlash);
            activeFlash = null;
        }

        activeFlash = StartCoroutine(FlashRoutine(hitWorldIndex));
    }

    private IEnumerator FlashRoutine(Vector2Int hitWorldIndex)
    {
        // Optional: freeze/slow logic while flashing.
        if (patternRuntimeManager != null)
        {
            // patternRuntimeManager.ApplySlowMotion(flashDuration, 0.0f);
        }

        int width = gridResolver.GridWidth;
        int height = gridResolver.GridHeight;

        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"[{ScriptName}] Invalid grid size {width}x{height}.", this);
            yield break;
        }

        float singleCycleDuration = flashDuration / Mathf.Max(1, flashCycles);
        float halfCycle = singleCycleDuration * 0.5f;

        Color hitColor = Color.white;
        Color otherColor = new Color(0.4f, 0.0f, 0.0f); // dark-ish red

        for (int cycle = 0; cycle < flashCycles; cycle++)
        {
            // Phase 1: effect on
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int index = new Vector2Int(x, y);
                    if (index == hitWorldIndex)
                    {
                        gridResolver.SetEffectTint(index, hitColor);
                    }
                    else
                    {
                        //gridResolver.SetEffectTint(index, otherColor);
                    }
                }
            }

            yield return new WaitForSeconds(halfCycle);

            // Phase 2: effect off
            ClearAllEffectTints(width, height);

            yield return new WaitForSeconds(halfCycle);
        }

        // Ensure cleared at the end.
        ClearAllEffectTints(width, height);
        activeFlash = null;
    }

    private void ClearAllEffectTints(int width, int height)
    {
        if (gridResolver == null)
        {
            if (!hasLoggedMissingResolver)
            {
                Debug.LogError($"[{ScriptName}] ClearAllEffectTints called but gridResolver is null.", this);
                hasLoggedMissingResolver = true;
            }

            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridResolver.ClearEffectTint(new Vector2Int(x, y));
            }
        }
    }
}
