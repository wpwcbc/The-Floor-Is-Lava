using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TouchCellUI : MonoBehaviour, ITouchCell
{
    [Header("Layout")]
    [SerializeField] private int sizeInCells = 1;
    [SerializeField] private bool isStarting = true;

    [SerializeField, HideInInspector]
    private Vector2Int position;

    [Header("Direction Visuals")]
    [SerializeField]
    private Transform visualsRoot; // assign a child container that holds Image/Text/etc.

    private int _visualDirection = 1;
    private bool _loggedMissingVisualsRootOnce = false;

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

        System.Action<ITouchCell, CellRole, CellRole> handler = RoleChanged;
        if (handler != null)
        {
            handler(this, oldRole, newRole);
        }
    }

    public CellColor color { get; private set; }

    public void SetColor(CellColor color)
    {
        this.color = color;
        baseColor = CellColorSprites.GetColor(color);
        UpdateVisualColor();
    }

    // NEW
    public void SetUiText(string text)
    {
        if (text == null)
        {
            text = string.Empty;
        }

        if (string.Equals(_cachedUiText, text))
        {
            return;
        }

        _cachedUiText = text;

        EnsureLabelRef();
        if (cellLabel == null)
        {
            if (!_loggedMissingLabelOnce)
            {
                Debug.LogError("[TouchCellUI] SetUiText called but no Text label is assigned/found.", this);
                _loggedMissingLabelOnce = true;
            }

            return;
        }

        cellLabel.text = _cachedUiText;
    }

    #endregion

    #region Visual

    [Header("Visual")]
    [SerializeField] private Image cellImage;

    // NEW: label
    [SerializeField] private Text cellLabel;

    private string _cachedUiText = string.Empty;
    private bool _loggedMissingLabelOnce = false;

    private Color baseColor = Color.black;

    private bool hasEffectTint = false;
    private Color effectTint = Color.white;

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

    private void EnsureLabelRef()
    {
        if (cellLabel != null)
        {
            return;
        }

        Text found = GetComponentInChildren<Text>(true);
        if (found != null)
        {
            cellLabel = found;
            cellLabel.text = _cachedUiText;
        }
    }

    #endregion

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
            cellImage = GetComponentInChildren<Image>();
        }

        if (cellImage == null)
        {
            Debug.LogError("TouchCellUI on " + name + " requires a child Image component.", this);
        }

        EnsureLabelRef();
        EnsureVisualsRootRef();
        ApplyVisualDirection();
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
        }
        else
        {
            ApplyGridPositionToUI();
        }

        CacheBaseTransformState();

        CurrentCellsProvider provider = CurrentCellsProvider.Instance;
        if (provider == null)
        {
            Debug.LogError("[TouchCellUI] CurrentCellsProvider.Instance is null. Cannot register cell.", this);
            return;
        }

        provider.RegisterCell(this);
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
            System.Action<ITouchCell> handler = Touched;
            if (handler != null)
            {
                handler(this);
            }
        }
        else
        {
            System.Action<ITouchCell> handler = Untouched;
            if (handler != null)
            {
                handler(this);
            }
        }
    }

    private Coroutine touchAnimCoroutine;
    private float animDuration = 0.1f;
    private float shrinkScale = 0.75f;

    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;
    private Vector2 pivotToCenter;
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

        CurrentCellsProvider provider = CurrentCellsProvider.Instance;
        if (provider != null)
        {
            provider.UnregisterCell(this);
        }
    }

    public void SetVisualDirection(int direction)
    {
        if (direction != 1 && direction != 2)
        {
            Debug.LogError($"[TouchCellUI] Unsupported direction={direction}. Expected 1 or 2. Defaulting to 1.", this);
            direction = 1;
        }

        if (_visualDirection == direction)
        {
            return;
        }

        _visualDirection = direction;
        ApplyVisualDirection();
    }

    private void ApplyVisualDirection()
    {
        EnsureVisualsRootRef();
        if (visualsRoot == null)
        {
            if (!_loggedMissingVisualsRootOnce)
            {
                Debug.LogError("[TouchCellUI] visualsRoot is null. Assign a child container (NOT the cell root) to rotate.", this);
                _loggedMissingVisualsRootOnce = true;
            }
            return;
        }

        // Z axis 180 (flip in UI plane)
        if (_visualDirection == 2)
        {
            visualsRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
        }
        else
        {
            visualsRoot.localRotation = Quaternion.identity;
        }
    }

    private void EnsureVisualsRootRef()
    {
        if (visualsRoot != null)
        {
            return;
        }

        // Prefer explicit assignment in inspector.
        // Safe fallback: try to use Image's parent as the visuals container.
        if (cellImage != null && cellImage.transform != null && cellImage.transform.parent != null)
        {
            // This is a child container (good). Do NOT ever set visualsRoot = transform.
            visualsRoot = cellImage.transform.parent;
            return;
        }

        // No fallback to self. Rotating self breaks positioning/animations.
        visualsRoot = null;
    }
}
