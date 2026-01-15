using System.Collections.Generic;
using UnityEngine;

public sealed class GridViewportRenderer : MonoBehaviour
{
    private const string ScriptName = "GridViewportRenderer";

    [Header("Viewport Mapping (bottom-left origin)")]
    [SerializeField]
    private ViewportConfig viewportConfig;

    [Header("Default State For Unused Cells")]
    [SerializeField]
    private CellRole defaultRole = CellRole.None;

    [SerializeField]
    private CellColor defaultColor = CellColor.Black;

    private bool _loggedMissingGlobalOnce = false;
    private bool _loggedInvalidDirectionOnce = false;

    // NEW: role -> label text (set by wrapper via ViewportInterface / LinkToViewportInterface)
    private readonly Dictionary<CellRole, string> _roleUiText =
        new Dictionary<CellRole, string>();

    public ViewportConfig Config
    {
        get { return viewportConfig; }
        set { viewportConfig = value; }
    }

    public void SetUpViewport(
        Vector2Int worldOrigin,
        Vector2Int localOrigin,
        int width,
        int height,
        int direction)
    {
        if (width < 0)
        {
            width = 0;
        }

        if (height < 0)
        {
            height = 0;
        }

        if (direction != 1 && direction != 2)
        {
            Debug.LogError($"[{ScriptName}] Unsupported direction={direction}. Expected 1 or 2. Defaulting to 1.", this);
            direction = 1;
        }

        ViewportConfig config = new ViewportConfig();
        config.WorldOrigin = worldOrigin;
        config.LocalOrigin = localOrigin;
        config.Size = new Vector2Int(width, height);
        config.Direction = direction;

        viewportConfig = config;
    }

    // NEW: called by ViewportInterface -> GridViewportRenderer
    public void SetRoleUiText(CellRole role, string text)
    {
        if (text == null)
        {
            text = string.Empty;
        }

        _roleUiText[role] = text;
    }

    private void LateUpdate()
    {
        RenderViewport();
    }

    private void RenderViewport()
    {
        VariableCellStatesEditInterface global = GlobalVariable.GetGlobal();
        if (global == null)
        {
            if (!_loggedMissingGlobalOnce)
            {
                Debug.LogError($"[{ScriptName}] VariableCellStatesEditInterface is null. No global grid state available.", this);
                _loggedMissingGlobalOnce = true;
            }
            return;
        }

        CurrentCellsProvider provider = CurrentCellsProvider.Instance;
        if (provider == null)
        {
            Debug.LogError($"[{ScriptName}] CurrentCellsProvider.Instance is null.", this);
            return;
        }

        IReadOnlyList<ITouchCell> allCells = provider.CurrentCells;
        if (allCells == null)
        {
            Debug.LogError($"[{ScriptName}] provider.CurrentCells is null.", this);
            return;
        }

        int direction = viewportConfig.Direction;

        int cellCount = allCells.Count;
        for (int i = 0; i < cellCount; i++)
        {
            ITouchCell cell = allCells[i];
            if (cell == null)
            {
                continue;
            }

            cell.SetVisualDirection(direction);

            Vector2Int localIndex = cell.Position;

            if (!viewportConfig.IsLocalIndexInViewport(localIndex))
            {
                ApplyDefaultIfNeeded(cell);
                continue;
            }

            Vector2Int worldIndex = LocalToWorld_WithDirection(localIndex);

            CellState state = global.GetGrid(worldIndex);

            if (cell.role != state.Role)
            {
                cell.SetRole(state.Role);
            }

            if (cell.color != state.Color)
            {
                cell.SetColor(state.Color);
            }

            // NEW: role UI text (viewport-side overlay)
            string uiText = GetUiTextForRole(state.Role);
            cell.SetUiText(uiText);

            // Effects remain visual-only (TouchCellUI)
            TouchCellUI ui = cell as TouchCellUI;
            if (ui != null)
            {
                if (state.HasEffectTint)
                {
                    ui.SetEffectTint(state.EffectTint);
                }
                else
                {
                    ui.ClearEffectTint();
                }
            }
        }
    }

    private Vector2Int LocalToWorld_WithDirection(Vector2Int localIndex)
    {
        int direction = viewportConfig.Direction;

        if (direction == 1)
        {
            return viewportConfig.LocalToWorld(localIndex);
        }

        if (direction == 2)
        {
            Vector2Int flippedLocal = FlipLocalIndex180(localIndex);
            return viewportConfig.LocalToWorld(flippedLocal);
        }

        if (!_loggedInvalidDirectionOnce)
        {
            Debug.LogError($"[{ScriptName}] Unsupported viewportConfig.Direction={direction}. Expected 1 or 2. Treating as 1.", this);
            _loggedInvalidDirectionOnce = true;
        }

        return viewportConfig.LocalToWorld(localIndex);
    }

    // Rotate 180 degrees within the viewport rectangle.
    private Vector2Int FlipLocalIndex180(Vector2Int localIndex)
    {
        Vector2Int size = viewportConfig.Size;
        Vector2Int origin = viewportConfig.LocalOrigin;

        if (size.x <= 0 || size.y <= 0)
        {
            return localIndex;
        }

        Vector2Int relative = localIndex - origin;

        int flippedX = (size.x - 1) - relative.x;
        int flippedY = (size.y - 1) - relative.y;

        Vector2Int flippedRelative = new Vector2Int(flippedX, flippedY);
        Vector2Int flippedLocal = origin + flippedRelative;

        return flippedLocal;
    }

    private string GetUiTextForRole(CellRole role)
    {
        string text;
        if (_roleUiText.TryGetValue(role, out text))
        {
            if (text != null)
            {
                return text;
            }
        }

        return string.Empty;
    }

    private void ApplyDefaultIfNeeded(ITouchCell cell)
    {
        if (cell.role != defaultRole)
        {
            cell.SetRole(defaultRole);
        }

        if (cell.color != defaultColor)
        {
            cell.SetColor(defaultColor);
        }

        // NEW: default role UI text (usually empty unless you set it)
        string uiText = GetUiTextForRole(defaultRole);
        cell.SetUiText(uiText);

        TouchCellUI ui = cell as TouchCellUI;
        if (ui != null)
        {
            ui.ClearEffectTint();
        }
    }

    public bool TryGetWorldIndexForCell(ITouchCell cell, out Vector2Int worldIndex)
    {
        if (cell == null)
        {
            worldIndex = default(Vector2Int);
            return false;
        }

        Vector2Int localIndex = cell.Position;

        if (!viewportConfig.IsLocalIndexInViewport(localIndex))
        {
            worldIndex = default(Vector2Int);
            return false;
        }

        worldIndex = LocalToWorld_WithDirection(localIndex);
        return true;
    }
}
