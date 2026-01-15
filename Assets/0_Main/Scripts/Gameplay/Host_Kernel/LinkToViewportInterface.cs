using UnityEngine;

public sealed class LinkToViewportInterface : MonoBehaviour, IViewportInterface
{
    [SerializeField]
    private MonoBehaviour viewportInterfaceImpl;

    private IViewportInterface viewportInterface;

    private bool hasLoggedMissingViewport;

    private void Awake()
    {
        if (viewportInterfaceImpl == null)
        {
            Debug.LogError("[LinkToViewportInterface] viewportInterfaceImpl is not assigned.", this);
            hasLoggedMissingViewport = true;
            return;
        }

        viewportInterface = viewportInterfaceImpl as IViewportInterface;
        if (viewportInterface == null)
        {
            Debug.LogError("[LinkToViewportInterface] viewportInterfaceImpl does not implement IViewportInterface.", this);
            hasLoggedMissingViewport = true;
        }
    }

    public void ConfigureViewport(
        Vector2Int worldOrigin,
        Vector2Int localOrigin,
        int width,
        int height,
        int direction)
    {
        if (viewportInterface == null)
        {
            if (!hasLoggedMissingViewport)
            {
                Debug.LogError("[LinkToViewportInterface] ConfigureViewport called but viewportInterface is null.", this);
                hasLoggedMissingViewport = true;
            }
            return;
        }

        viewportInterface.ConfigureViewport(worldOrigin, localOrigin, width, height, direction);
    }

    public void SetViewportEnabled(bool enabled)
    {
        if (viewportInterface == null)
        {
            if (!hasLoggedMissingViewport)
            {
                Debug.LogError("[LinkToViewportInterface] SetViewportEnabled called but viewportInterface is null.", this);
                hasLoggedMissingViewport = true;
            }
            return;
        }

        viewportInterface.SetViewportEnabled(enabled);
    }

    public void SetRoleUiText(CellRole role, string text)
    {
        if (viewportInterface == null)
        {
            if (!hasLoggedMissingViewport)
            {
                Debug.LogError("[LinkToViewportInterface] SetRoleUiText called but viewportInterface is null.", this);
                hasLoggedMissingViewport = true;
            }
            return;
        }

        viewportInterface.SetRoleUiText(role, text);
    }
}
