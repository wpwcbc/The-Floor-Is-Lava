using UnityEngine;

public sealed class LinkToKernelInterface : MonoBehaviour, IKernelInterface
{
    private const string ScriptName = "LinkToKernelInterface";

    [SerializeField]
    private MonoBehaviour kernelInterfaceImpl;

    private IKernelInterface kernelInterface;

    private void Awake()
    {
        if (kernelInterfaceImpl != null)
        {
            kernelInterface = kernelInterfaceImpl as IKernelInterface;
            if (kernelInterface == null)
            {
                Debug.LogError(
                    $"[{ScriptName}] kernelInterfaceImpl does not implement IKernelInterface.",
                    this);
            }
        }
        else
        {
            // Fallback: check this GameObject.
            kernelInterface = GetComponent<IKernelInterface>();
            if (kernelInterface == null)
            {
                Debug.LogError(
                    $"[{ScriptName}] kernelInterfaceImpl is not assigned and no IKernelInterface found on this GameObject.",
                    this);
            }
        }
    }

    public void HandleCellTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (kernelInterface == null)
        {
            Debug.LogError($"[{ScriptName}] HandleCellTouched called but kernelInterface is null.", this);
            return;
        }

        kernelInterface.HandleCellTouched(worldIndex, role, color);
    }

    public void HandleCellUntouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (kernelInterface == null)
        {
            Debug.LogError($"[{ScriptName}] HandleCellUntouched called but kernelInterface is null.", this);
            return;
        }

        kernelInterface.HandleCellUntouched(worldIndex, role, color);
    }
}
