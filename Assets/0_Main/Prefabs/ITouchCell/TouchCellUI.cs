using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TouchCellUI : MonoBehaviour, ITouchCell
{
    [Header("Layout")]
    [SerializeField] private int sizeInCells = 1;   // 1 = 1x1, 2 = 2x2, etc.

    [Tooltip("If true, this cell is manually placed in the scene and its grid Position is computed from its UI rect at Start. If false, Position drives the UI rect (for spawned cells).")]
    [SerializeField] private bool isStarting = true;

    // Backing field for grid position
    [SerializeField, HideInInspector]
    private Vector2Int position;

    // ITouchCell implementation
    public int Size
    {
        get => sizeInCells;
        set => sizeInCells = Mathf.Max(1, value);
    }

    public Vector2Int Position
    {
        get => position;
        set
        {
            position = value;
            ApplyGridPositionToUI();

            // If you move cells at runtime and want animation center to follow,
            // uncomment this:
            // CacheBaseTransformState();
        }
    }

    public bool IsTouched { get; private set; }
    public CellRole role { get; private set; } = CellRole.None;
    public void SetRole(CellRole role)
    {
        this.role = role;
    }

    public CellColor color { get; private set; }
    public void SetColor(CellColor color)
    {
        this.color = color;
        transform.GetComponent<Image>().sprite = CellColorSprites.GetSprite(color);
    }

    /// <summary>
    /// Helper: bottom-left of this cell in screen pixels.
    /// </summary>
    private Vector2Int UiPos
    {
        get => GridMathUtils.GridToPixelOrigin(position);
        set => Position = GridMathUtils.PixelToGridIndex(value);
    }

    private RectTransform rect;
    private Canvas canvas;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (!rect)
        {
            Debug.LogError($"TouchCellUI on {name} requires a RectTransform.", this);
        }

        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            Debug.LogError($"TouchCellUI on {name} must be under a Canvas.", this);
        }
    }

    private void Start()
    {
        if (!rect || !canvas) return;

        if (isStarting)
        {
            // Manually placed in scene: read UI and compute grid index
            position = ComputeGridPositionFromUI();
        }
        else
        {
            // Spawned/moved from code: ensure UI matches current grid position
            ApplyGridPositionToUI();
        }

        // Cache transform state AFTER grid placement so "rest" = correct bottom-left.
        CacheBaseTransformState();

        CurrentCellsProvider.Instance.RegisterCell(this);
    }

    private void CacheBaseTransformState()
    {
        if (rect == null) return;

        baseScale = rect.localScale;
        baseAnchoredPos = rect.anchoredPosition;

        // rect.rect is already in local coordinates of this RectTransform
        Rect r = rect.rect;
        pivotToCenter = new Vector2(r.width * 0.5f, r.height * 0.5f);

        baseTransformCached = true;
    }

    private Vector2Int ComputeGridPositionFromUI()
    {
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // Get world corners of this RectTransform
        // 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector3 worldBottomLeft = corners[0];

        // Convert world → screen pixels
        Vector2 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(cam, worldBottomLeft);

        // Finally: screen pixels → grid index
        return GridMathUtils.PixelToGridIndex(screenBottomLeft);
    }

    private void ApplyGridPositionToUI()
    {
        if (!rect || !canvas) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (!canvasRect) return;

        // Target bottom-left in screen pixels
        Vector2 screenBottomLeft = GridMathUtils.GridToPixelOrigin(position);

        // Convert to canvas local space
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenBottomLeft,
                cam,
                out Vector2 localPoint))
        {
            // ASSUMPTION: rect.pivot = (0,0) and anchors at bottom-left.
            rect.anchoredPosition = localPoint;
        }
    }

    // Public helpers for future spawning logic:

    /// <summary>
    /// Set this cell's grid index and move its UI rect accordingly.
    /// </summary>
    public void SetGridPosition(Vector2Int gridIndex)
    {
        Position = gridIndex;
    }

    /// <summary>
    /// Set this cell based on a bottom-left screen pixel position.
    /// </summary>
    public void SetUiBottomLeft(Vector2 screenPos)
    {
        UiPos = Vector2Int.RoundToInt(screenPos);
    }

    public void SetIsTouched(bool isTouched)
    {
        if (IsTouched == isTouched)
            return;

        IsTouched = isTouched;

        if (isTouched)
        {
            TouchedLogic();
        }
        else
        {
            UntouchedLogic();
        }
    }

    #region Touch Logic

    // --- animation state backing fields ---
    private Coroutine touchAnimCoroutine;
    private float animDuration = 0.1f; // seconds
    private float shrinkScale = 0.75f;   // relative scale factor (1 -> normal, 0.9 -> 90%)

    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private Vector2 pivotToCenter;   // vector from pivot (bottom-left) to center in local space
    private bool baseTransformCached = false;

    private void TouchedLogic()
    {
        if (!baseTransformCached)
        {
            CacheBaseTransformState();
        }

        if (touchAnimCoroutine != null)
        {
            StopCoroutine(touchAnimCoroutine);
        }

        touchAnimCoroutine = StartCoroutine(ShrinkAnim());
    }

    private void UntouchedLogic()
    {
        if (!baseTransformCached)
        {
            CacheBaseTransformState();
        }

        if (touchAnimCoroutine != null)
        {
            StopCoroutine(touchAnimCoroutine);
        }

        touchAnimCoroutine = StartCoroutine(UnshrinkAnim());
    }

    /// <summary>
    /// Apply a relative scale factor around the cell's visual center,
    /// keeping the center fixed even though the pivot is bottom-left.
    /// scaleFactor = 1.0 → rest; shrinkScale → shrunk.
    /// </summary>
    private void ApplyScaleAroundCenter(float scaleFactor)
    {
        if (!baseTransformCached || rect == null)
        {
            return;
        }

        // Scale relative to baseScale
        float scaledX = baseScale.x * scaleFactor;
        float scaledY = baseScale.y * scaleFactor;

        rect.localScale = new Vector3(scaledX, scaledY, baseScale.z);

        // Offset from pivot to center in parent space at base scale
        Vector2 baseOffset = new Vector2(baseScale.x * pivotToCenter.x, baseScale.y * pivotToCenter.y);

        // Move pivot so that center stays fixed as we change scaleFactor
        // center = baseAnchoredPos + baseOffset (constant)
        // anchoredPos = center - scaleFactor * baseOffset
        rect.anchoredPosition = baseAnchoredPos + (1.0f - scaleFactor) * baseOffset;
    }

    private IEnumerator ShrinkAnim()
    {
        if (!baseTransformCached || rect == null)
        {
            yield break;
        }

        float elapsed = 0.0f;

        // Current relative scale factor (in case we retrigger mid-animation)
        float currentScaleFactor = 1.0f;
        if (Mathf.Abs(baseScale.x) > Mathf.Epsilon)
        {
            currentScaleFactor = rect.localScale.x / baseScale.x;
        }

        float startScaleFactor = currentScaleFactor;
        float targetScaleFactor = shrinkScale;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float scaleFactor = Mathf.Lerp(startScaleFactor, targetScaleFactor, t);

            ApplyScaleAroundCenter(scaleFactor);
            yield return null;
        }

        ApplyScaleAroundCenter(targetScaleFactor);
    }

    private IEnumerator UnshrinkAnim()
    {
        if (!baseTransformCached || rect == null)
        {
            yield break;
        }

        float elapsed = 0.0f;

        float currentScaleFactor = 1.0f;
        if (Mathf.Abs(baseScale.x) > Mathf.Epsilon)
        {
            currentScaleFactor = rect.localScale.x / baseScale.x;
        }

        float startScaleFactor = currentScaleFactor;
        float targetScaleFactor = 1.0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float scaleFactor = Mathf.Lerp(startScaleFactor, targetScaleFactor, t);

            ApplyScaleAroundCenter(scaleFactor);
            yield return null;
        }

        ApplyScaleAroundCenter(targetScaleFactor);
    }

    #endregion

    void OnDisable()
    {
        if (touchAnimCoroutine != null)
        {
            StopCoroutine(touchAnimCoroutine);
            touchAnimCoroutine = null;
        }

        if (CurrentCellsProvider.Instance != null)
        {
            CurrentCellsProvider.Instance.UnregisterCell(this);
        }
    }
}
