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
        }
    }

    public bool IsTouched { get; private set; }
    public bool IsSensitive { get; set; } = true;
    public bool IsForbidden { get; set; } = true;

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

        Debug.Log($"GridPos: {position}");
        Debug.Log($"UiPos: {UiPos}");

        CurrentCellsProvider.Instance.RegisterCell(this);

        // Optional debug:
        // Debug.Log($"{name} grid index = {Position}");
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
            // ASSUMPTION: rect.pivot = (0,0) so anchoredPosition == bottom-left
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

        // React to state change (visuals, events, etc.)
        if (isTouched)
        {
            TouchedLogic();
        }
        else
        {
            UntouchedLogic();
        }
    }

    private void TouchedLogic()
    {
        // TEMP
        transform.GetComponent<Image>().color = new Color(1f, 0f, 0f);
    }

    private void UntouchedLogic()
    {
        // TEMP
        transform.GetComponent<Image>().color = new Color(0f, 1f, 0f);
    }

    void OnDisable()
    {
        CurrentCellsProvider.Instance.UnregisterCell(this);
    }
}
