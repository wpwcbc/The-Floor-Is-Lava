using UnityEngine;
using UnityEngine.UI;

public class DebugMarker : MonoBehaviour
{
    [SerializeField] private Text idText; // cache instead of GetChild every time

    public void Set(int id, Vector2 screenPos)
    {
        // Find canvas and rects
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransform markerRect = transform as RectTransform;

        // For Screen Space Overlay, camera = null
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                cam,
                out localPoint))
        {
            // localPoint is in the canvas's local space
            markerRect.anchoredPosition = localPoint;
        }

        if (idText != null)
        {
            idText.text = $"{id}\n{screenPos}\n{localPoint}";
        }
    }
}
