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

    #region Interface Impl

    public int Size
    {
        get { return sizeInCells; }
        set { sizeInCells = Mathf.Max(1, value); }
    }

    public Vector2Int Position
    {
        get { return position; }
        set
        {
            position = value;
            ApplyGridPositionToUI();
            // If you move cells at runtime and want animation center to follow,
            // you can call CacheBaseTransformState() here.
        }
    }

    public bool IsTouched { get; private set; }

    public CellRole role { get; private set; } = CellRole.None;

    public event System.Action<ITouchCell, CellRole, CellRole> RoleChanged;

    public void SetRole(CellRole newRole)
    {

        if (role == newRole)
        {
            return;
        }

        CellRole oldRole = role;
        role = newRole;

        if (RoleChanged != null)
        {
            RoleChanged(this, oldRole, newRole);
        }
    }

    public CellColor color { get; private set; }

    public void SetColor(CellColor color)
    {
        this.color = color;
        baseColor = CellColorSprites.GetColor(color);
        UpdateVisualColor();
    }

    #endregion

    #region Visual

    [Header("Visual")]
    [SerializeField] private Image cellImage; // child Image that actually shows the tile color

    // Base logical color (from patterns)
    private Color baseColor = Color.black;

    // Effect layer
    private bool hasEffectTint = false;
    private Color effectTint = Color.white;

    // Visual-only API for effects (do not add to ITouchCell)
    public void SetEffectTint(Color tint)
    {
        hasEffectTint = true;
        effectTint = tint;
        UpdateVisualColor();
    }

    public void ClearEffectTint()
    {
        hasEffectTint = false;
        UpdateVisualColor();
    }

    private void UpdateVisualColor()
    {
        if (cellImage == null)
        {
            return;
        }

        if (hasEffectTint)
        {
            cellImage.color = effectTint;
        }
        else
        {
            cellImage.color = baseColor;
        }
    }

    #endregion

    /// <summary>
    /// Bottom-left of this cell in canvas-local pixels (same space as GridMathUtils).
    /// </summary>
    private Vector2Int UiPos
    {
        get
        {
            if (rect == null)
            {
                return Vector2Int.zero;
            }

            return Vector2Int.RoundToInt(rect.anchoredPosition);
        }
        set
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = value;
            position = GridMathUtils.PixelToGridIndex(value);
        }
    }

    private RectTransform rect;
    private Canvas canvas;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (rect == null)
        {
            Debug.LogError("TouchCellUI on " + name + " requires a RectTransform.", this);
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("TouchCellUI on " + name + " must be under a Canvas.", this);
        }

        if (cellImage == null)
        {
            // Parent has no Image; the child does.
            cellImage = GetComponentInChildren<Image>();
        }

        if (cellImage == null)
        {
            Debug.LogError("TouchCellUI on " + name + " requires a child Image component.", this);
        }
    }

    private void Start()
    {
        if (rect == null)
        {
            return;
        }

        if (isStarting)
        {
            position = ComputeGridPositionFromUI();
            Debug.Log(position);
        }
        else
        {
            ApplyGridPositionToUI();
        }

        CacheBaseTransformState();
        CurrentCellsProvider.Instance.RegisterCell(this);
    }

    private void CacheBaseTransformState()
    {
        if (rect == null)
        {
            return;
        }

        baseScale = rect.localScale;
        baseAnchoredPos = rect.anchoredPosition;

        Rect r = rect.rect;
        pivotToCenter = new Vector2(r.width * 0.5f, r.height * 0.5f);

        baseTransformCached = true;
    }

    private Vector2Int ComputeGridPositionFromUI()
    {
        if (rect == null)
        {
            return Vector2Int.zero;
        }

        // anchoredPosition is bottom-left in canvas-local space (after CanvasScaler)
        Vector2 canvasBottomLeft = rect.anchoredPosition;
        return GridMathUtils.PixelToGridIndex(canvasBottomLeft);
    }

    private void ApplyGridPositionToUI()
    {
        if (rect == null)
        {
            return;
        }

        Vector2Int canvasBottomLeft = GridMathUtils.GridToPixelOrigin(position);
        rect.anchoredPosition = canvasBottomLeft;
    }


    // Public helpers for future spawning logic:

    public void SetGridPosition(Vector2Int gridIndex)
    {
        Position = gridIndex;
    }

    public void SetUiBottomLeft(Vector2 screenPos)
    {
        UiPos = Vector2Int.RoundToInt(screenPos);
    }

    #region Touch Logic

    public event System.Action<ITouchCell> Touched;
    public event System.Action<ITouchCell> Untouched;

    public void SetIsTouched(bool isTouched)
    {
        if (IsTouched == isTouched)
        {
            return;
        }

        IsTouched = isTouched;

        if (isTouched)
        {
            TouchedLogic();
        }
        else
        {
            UntouchedLogic();
        }

        if (isTouched)
        {
            if (Touched != null)
            {
                Touched(this);
            }
        }
        else
        {
            if (Untouched != null)
            {
                Untouched(this);
            }
        }
    }

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

    private void ApplyScaleAroundCenter(float scaleFactor)
    {
        if (!baseTransformCached || rect == null)
        {
            return;
        }

        float scaledX = baseScale.x * scaleFactor;
        float scaledY = baseScale.y * scaleFactor;

        rect.localScale = new Vector3(scaledX, scaledY, baseScale.z);

        Vector2 baseOffset = new Vector2(baseScale.x * pivotToCenter.x, baseScale.y * pivotToCenter.y);

        rect.anchoredPosition = baseAnchoredPos + (1.0f - scaleFactor) * baseOffset;
    }

    private IEnumerator ShrinkAnim()
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

    private void OnDisable()
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
