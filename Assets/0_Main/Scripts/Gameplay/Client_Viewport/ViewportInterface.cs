using UnityEngine;

public interface IViewportInterface
{
    void ConfigureViewport(
        Vector2Int worldOrigin,
        Vector2Int localOrigin,
        int width,
        int height,
        int direction);

    void SetViewportEnabled(bool enabled);

    // NEW: wrapper -> viewport (visual label mapping)
    void SetRoleUiText(CellRole role, string text);
}

public sealed class ViewportInterface : MonoBehaviour, IViewportInterface
{
    [SerializeField]
    private GridViewportRenderer viewportRenderer;

    private void Awake()
    {
        if (viewportRenderer == null)
        {
            viewportRenderer = GetComponent<GridViewportRenderer>();
        }

        if (viewportRenderer == null)
        {
            Debug.LogError("[ViewportInterface] GridViewportRenderer is not assigned or found on this GameObject.", this);
        }
    }

    public void ConfigureViewport(
        Vector2Int worldOrigin,
        Vector2Int localOrigin,
        int width,
        int height,
        int direction)
    {
        if (viewportRenderer == null)
        {
            Debug.LogError("[ViewportInterface] ConfigureViewport called but viewportRenderer is null.", this);
            return;
        }

        viewportRenderer.SetUpViewport(worldOrigin, localOrigin, width, height, direction);
    }

    public void SetViewportEnabled(bool enabled)
    {
        if (viewportRenderer == null)
        {
            Debug.LogError("[ViewportInterface] SetViewportEnabled called but viewportRenderer is null.", this);
            return;
        }

        viewportRenderer.enabled = enabled;
    }

    public void SetRoleUiText(CellRole role, string text)
    {
        if (viewportRenderer == null)
        {
            Debug.LogError("[ViewportInterface] SetRoleUiText called but viewportRenderer is null.", this);
            return;
        }

        viewportRenderer.SetRoleUiText(role, text);
    }
}
