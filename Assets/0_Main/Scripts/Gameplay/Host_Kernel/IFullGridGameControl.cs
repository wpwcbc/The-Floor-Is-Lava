using System;
using System.Collections.Generic;
using UnityEngine;

public interface IFullGridGameControl
{
    /// <summary>
    /// Configure the full world grid size (in cells). Must be called before starting a level.
    /// </summary>
    void ConfigureFullGrid(int width, int height);

    /// <summary>
    /// Set the active level that will be initialized when StartLevel is called.
    /// </summary>
    void SetActiveLevel(PatternLevelSetupBase level);

    /// <summary>
    /// Show the standby patterns which shows the starting safe area (or any customize patterns) of the selected level.
    /// </summary>
    void ShowStandby();

    /// <summary>
    /// Build and register patterns for the active level and start simulation.
    /// </summary>
    void StartLevel();

    /// <summary>
    /// Stop simulation for the current level (does not necessarily destroy patterns).
    /// </summary>
    void StopLevel();

    int GridWidth { get; }
    int GridHeight { get; }

    // ---------- NEW: kernel state / events ----------

    /// <summary>
    /// World indices of cells overlapped by any touch point in the current frame.
    /// </summary>
    IReadOnlyCollection<Vector2Int> GetCurrentOverlappedCells();

    /// <summary>
    /// Number of cells currently in Weakness role in the full grid.
    /// </summary>
    int GetWeaknessCellCount();

    /// <summary>
    /// Fills the buffer with world indices of cells in Weakness role.
    /// Buffer is cleared first.
    /// </summary>
    void GetWeaknessCellIndices(List<Vector2Int> buffer);

    /// <summary>
    /// Raised whenever a world cell is touched this frame.
    /// (Forwarded from the kernel touch system.)
    /// </summary>
    event Action<Vector2Int, CellRole, CellColor> WorldCellTouched;
}
