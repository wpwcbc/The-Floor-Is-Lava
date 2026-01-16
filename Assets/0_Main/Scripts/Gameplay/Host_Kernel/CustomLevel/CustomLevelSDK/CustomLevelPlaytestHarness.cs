using UnityEngine;

public sealed class CustomLevelPlaytestHarness : MonoBehaviour
{
    private const int ViewportMaxWidth = 16;
    private const int ViewportMaxHeight = 9;

    [SerializeField] private FullGridGameController gameController;
    [SerializeField] private PatternLevel_CustomFromData customLevel;
    [SerializeField] private LinkToViewportInterface linkToViewportInterface;

    [Header("Viewport Direction (0/1/2/3)")]
    [SerializeField] private int viewportDirection = 0;

    public void ApplyAndPlaytest(CustomLevelDataModel model)
    {
        if (gameController == null)
        {
            Debug.LogError("[CustomLevelPlaytestHarness] gameController is null.", this);
            return;
        }

        if (customLevel == null)
        {
            Debug.LogError("[CustomLevelPlaytestHarness] customLevel is null.", this);
            return;
        }

        if (model == null)
        {
            Debug.LogError("[CustomLevelPlaytestHarness] model is null.", this);
            return;
        }

        if (linkToViewportInterface == null)
        {
            Debug.LogError("[CustomLevelPlaytestHarness] linkToViewportInterface is null.", this);
            return;
        }

        gameController.StopLevel();

        gameController.ConfigureFullGrid(model.gridWidth, model.gridHeight);

        ConfigureViewportForModel(model);

        customLevel.SetData(model);

        gameController.SetActiveLevel(customLevel);
        gameController.ShowStandby();
        gameController.StartLevel();
    }

    private void ConfigureViewportForModel(CustomLevelDataModel model)
    {
        int gridWidth = Mathf.Max(1, model.gridWidth);
        int gridHeight = Mathf.Max(1, model.gridHeight);

        int vpWidth = Mathf.Min(ViewportMaxWidth, gridWidth);
        int vpHeight = Mathf.Min(ViewportMaxHeight, gridHeight);

        // Show the "center" of the world grid inside the viewport.
        // worldOrigin is the world cell that maps to localOrigin in the viewport.
        // We set localOrigin at (0,0) => worldOrigin becomes viewport bottom-left world cell.
        Vector2Int localOrigin = Vector2Int.zero;

        int worldOriginX = (gridWidth - vpWidth) / 2;
        int worldOriginY = (gridHeight - vpHeight) / 2;
        Vector2Int worldOrigin = new Vector2Int(worldOriginX, worldOriginY);

        int dir = viewportDirection;
        if (dir < 0) dir = 0;
        if (dir > 3) dir = 3;

        Debug.Log(
            "[CustomLevelPlaytestHarness] ConfigureViewport: " +
            "grid=" + gridWidth + "x" + gridHeight +
            " vp=" + vpWidth + "x" + vpHeight +
            " worldOrigin=" + worldOrigin +
            " localOrigin=" + localOrigin +
            " dir=" + dir,
            this);

        linkToViewportInterface.ConfigureViewport(
            worldOrigin,
            localOrigin,
            vpWidth,
            vpHeight,
            dir);

        linkToViewportInterface.SetViewportEnabled(true);
    }
}
