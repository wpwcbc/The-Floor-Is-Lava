using System.Reflection;
using UnityEngine;

/// <summary>
/// Runtime-only grid generator: creates a 32x18 grid of TouchCellUI instances at Start(),
/// using the bottom-left of the gridRoot as screen-space origin (0,0).
/// - Does not run in the editor (no ExecuteAlways).
/// - Names cells "Cell_x_y" (structural naming).
/// - Forces each instantiated TouchCellUI into "generated" mode by setting its private
///   isStarting flag to false via reflection so Start() will not recompute from UI rect.
/// 
/// Usage:
/// - Place this component on a GameObject under a Canvas. If gridRoot is left null,
///   this object's RectTransform will be used. Ensure gridRoot has pivot/anchors such that
///   its bottom-left corresponds to screen pixel (0,0) if you want the visual origin to be (0,0).
/// - Assign a TouchCellUI prefab (RectTransform sized to GridConfig.CellSize).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TouchCellRuntimeGenerator : MonoBehaviour
{
    [Header("Runtime setup")]
    [Tooltip("Parent RectTransform under which cells will be created. If null, uses this object's RectTransform.")]
    [SerializeField] private RectTransform gridRoot;

    [Tooltip("Prefab with TouchCellUI component (RectTransform).")]
    [SerializeField] private TouchCellUI cellPrefab;

    // Hard-coded grid size per request
    private const int GridWidth = 32;
    private const int GridHeight = 18;

    private void Start()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("[TouchCellRuntimeGenerator] cellPrefab is not assigned.", this);
            return;
        }

        if (gridRoot == null)
        {
            RectTransform selfRect = transform as RectTransform;
            if (selfRect == null)
            {
                Debug.LogError("[TouchCellRuntimeGenerator] gridRoot is null and this GameObject has no RectTransform.", this);
                return;
            }

            gridRoot = selfRect;
        }

        Canvas parentCanvas = gridRoot.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("[TouchCellRuntimeGenerator] gridRoot must be under a Canvas.", gridRoot);
            return;
        }

        // Clear any existing children
        int childCount = gridRoot.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);
            GameObject.Destroy(child.gameObject);
        }

        // Reflection: get private field "isStarting" on TouchCellUI to force generated mode.
        FieldInfo isStartingField = typeof(TouchCellUI).GetField("isStarting", BindingFlags.NonPublic | BindingFlags.Instance);

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                Vector2Int gridIndex = new Vector2Int(x, y);

                TouchCellUI instance = GameObject.Instantiate<TouchCellUI>(cellPrefab, gridRoot);

                instance.name = string.Format("Cell_{0}_{1}", gridIndex.x, gridIndex.y);
                instance.Size = 1;

                // Force the private isStarting = false so the TouchCellUI.Start() path uses ApplyGridPositionToUI()
                if (isStartingField != null)
                {
                    isStartingField.SetValue(instance, false);
                }

                // Apply grid position (this will move the UI rect)
                instance.SetGridPosition(gridIndex);

                // Basic flags for now
                instance.SetRole(CellRole.Safe);
            }
        }

        Debug.LogFormat("[TouchCellRuntimeGenerator] Created grid {0}x{1} ({2} cells).", GridWidth, GridHeight, GridWidth * GridHeight);
    }
}
