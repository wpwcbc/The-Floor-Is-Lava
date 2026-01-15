using System.Collections.Generic;
using UnityEngine;

public sealed class PatternToGridResolver : MonoBehaviour
{
    private const string ScriptName = "PatternToGridResolver";

    // [Header("World Grid Size")]
    private int gridWidth;
    private int gridHeight;

    private readonly List<PatternInstance> _patternInstances = new List<PatternInstance>();

    // Resolved world state for this frame.
    private CellState[,] _grid;
    private PatternInstance[,] _owners;
    private int[,] _layers; // For resolving overlaps by layer.

    // --- Effect overlay ---
    private bool[,] _effectActive;
    private Color[,] _effectTint;

    // --- Global push state ---
    private bool _loggedMissingGlobalOnce = false;
    private int _publishedWidth = -1;
    private int _publishedHeight = -1;

    [SerializeField]
    private bool publishDifferencesOnly = true;

    private CellState[,] _publishedGrid;
    private bool _hasPublishedAtLeastOnce = false;
    private bool _forcePublishAll = false;

    public int GridWidth
    {
        get { return gridWidth; }
    }

    public int GridHeight
    {
        get { return gridHeight; }
    }

    private void Start()
    {
        EnsureBuffers();
        EnsureGlobalGridAllocated();
    }

    private void OnValidate()
    {
        if (gridWidth < 1)
        {
            gridWidth = 1;
        }

        if (gridHeight < 1)
        {
            gridHeight = 1;
        }

        EnsureBuffers();
    }

    public void SetWorldGridSize(int width, int height)
    {
        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        gridWidth = width;
        gridHeight = height;

        EnsureBuffers();
        EnsureGlobalGridAllocated();
    }

    private void EnsureBuffers()
    {
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            return;
        }

        bool needResize =
            _grid == null ||
            _grid.GetLength(0) != gridWidth ||
            _grid.GetLength(1) != gridHeight;

        if (needResize)
        {
            _grid = new CellState[gridWidth, gridHeight];
            _owners = new PatternInstance[gridWidth, gridHeight];
            _layers = new int[gridWidth, gridHeight];

            _effectActive = new bool[gridWidth, gridHeight];
            _effectTint = new Color[gridWidth, gridHeight];

            _publishedGrid = new CellState[gridWidth, gridHeight];
            _hasPublishedAtLeastOnce = false;
            _forcePublishAll = true;
        }
    }

    private void EnsureGlobalGridAllocated()
    {
        VariableCellStatesEditInterface global = GlobalVariable.GetGlobal();
        if (global == null)
        {
            if (!_loggedMissingGlobalOnce)
            {
                Debug.LogError($"[{ScriptName}] VariableCellStatesEditInterface is null. Cannot publish grid state.", this);
                _loggedMissingGlobalOnce = true;
            }
            return;
        }

        _loggedMissingGlobalOnce = false;

        if (_publishedWidth != gridWidth || _publishedHeight != gridHeight)
        {
            global.InitGrids(gridWidth, gridHeight);
            _publishedWidth = gridWidth;
            _publishedHeight = gridHeight;

            _forcePublishAll = true;
            _hasPublishedAtLeastOnce = false;
        }
    }

    public void RegisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (_patternInstances.Contains(instance))
        {
            return;
        }

        _patternInstances.Add(instance);
    }

    public void UnregisterPatternInstance(PatternInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        _patternInstances.Remove(instance);
    }

    public bool IsIndexInBounds(Vector2Int index)
    {
        return index.x >= 0
               && index.x < gridWidth
               && index.y >= 0
               && index.y < gridHeight;
    }

    public CellState GetCellState(Vector2Int index)
    {
        if (!IsIndexInBounds(index))
        {
            return CellState.Default;
        }

        int x = index.x;
        int y = index.y;

        CellState state = _grid[x, y];

        if (_effectActive != null && _effectTint != null && _effectActive[x, y])
        {
            state.HasEffectTint = true;
            state.EffectTint = _effectTint[x, y];
        }
        else
        {
            state.HasEffectTint = false;
        }

        return state;
    }

    public PatternInstance GetOwnerAt(Vector2Int index)
    {
        if (!IsIndexInBounds(index))
        {
            return null;
        }

        return _owners[index.x, index.y];
    }

    private void Update()
    {
        ResolveGrid();
    }

    private void ResolveGrid()
    {
        if (_grid == null || _owners == null || _layers == null)
        {
            EnsureBuffers();
            if (_grid == null)
            {
                return;
            }
        }

        // Ensure global grid exists and is sized correctly.
        EnsureGlobalGridAllocated();

        // 1. Clear base grid to default state and reset layers/owners.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _grid[x, y] = CellState.Default;
                _owners[x, y] = null;
                _layers[x, y] = int.MinValue;
                // NOTE: we do NOT clear _effectActive/_effectTint here
            }
        }

        // 2. Collect final cell states with layer resolution.
        int patternCount = _patternInstances.Count;
        for (int i = 0; i < patternCount; i++)
        {
            PatternInstance pattern = _patternInstances[i];
            if (pattern == null)
            {
                continue;
            }

            if (!pattern.IsAlive)
            {
                continue;
            }

            IEnumerable<WorldPatternCell> occupiedCells = pattern.GetOccupiedCells();
            if (occupiedCells == null)
            {
                continue;
            }

            foreach (WorldPatternCell worldCell in occupiedCells)
            {
                int x = worldCell.Index.X;
                int y = worldCell.Index.Y;

                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    continue;
                }

                int currentLayer = _layers[x, y];
                if (worldCell.Layer >= currentLayer)
                {
                    _layers[x, y] = worldCell.Layer;

                    CellState state = _grid[x, y];
                    state.Role = worldCell.Role;
                    state.Color = worldCell.Color;

                    _grid[x, y] = state;
                    _owners[x, y] = pattern;
                }
            }
        }

        // 3. Push final (base + effect overlay) into the global variable store.
        VariableCellStatesEditInterface global = GlobalVariable.GetGlobal();
        if (global == null)
        {
            if (!_loggedMissingGlobalOnce)
            {
                Debug.LogError($"[{ScriptName}] VariableCellStatesEditInterface is null. Cannot publish grid state.", this);
                _loggedMissingGlobalOnce = true;
            }
            return;
        }

        _loggedMissingGlobalOnce = false;

        if (_publishedGrid == null)
        {
            EnsureBuffers();
            if (_publishedGrid == null)
            {
                Debug.LogError($"[{ScriptName}] Published grid buffer is null. Cannot publish.", this);
                return;
            }
        }

        bool publishAll = _forcePublishAll || !_hasPublishedAtLeastOnce || !publishDifferencesOnly;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellState finalState = _grid[x, y];

                if (_effectActive != null && _effectTint != null && _effectActive[x, y])
                {
                    finalState.HasEffectTint = true;
                    finalState.EffectTint = _effectTint[x, y];
                }
                else
                {
                    finalState.HasEffectTint = false;
                }

                if (!publishAll)
                {
                    CellState lastPublishedState = _publishedGrid[x, y];
                    if (AreStatesEqual(finalState, lastPublishedState))
                    {
                        continue;
                    }
                }

                global.SetGrid(new Vector2Int(x, y), finalState);
                _publishedGrid[x, y] = finalState;
            }
        }

        _hasPublishedAtLeastOnce = true;
        _forcePublishAll = false;
    }

    private static bool AreStatesEqual(CellState a, CellState b)
    {
        if (a.Role != b.Role)
        {
            return false;
        }

        if (a.Color != b.Color)
        {
            return false;
        }

        if (a.HasEffectTint != b.HasEffectTint)
        {
            return false;
        }

        if (!a.HasEffectTint)
        {
            return true;
        }

        return a.EffectTint.Equals(b.EffectTint);
    }


    public void SetEffectTint(Vector2Int index, Color tint)
    {
        if (!IsIndexInBounds(index))
        {
            return;
        }

        if (_effectActive == null || _effectTint == null)
        {
            EnsureBuffers();
            if (_effectActive == null || _effectTint == null)
            {
                Debug.LogError($"[{ScriptName}] Effect buffers are not initialized.", this);
                return;
            }
        }

        _effectActive[index.x, index.y] = true;
        _effectTint[index.x, index.y] = tint;
    }

    public void ClearEffectTint(Vector2Int index)
    {
        if (!IsIndexInBounds(index))
        {
            return;
        }

        if (_effectActive == null)
        {
            EnsureBuffers();
            if (_effectActive == null)
            {
                Debug.LogError($"[{ScriptName}] Effect buffers are not initialized.", this);
                return;
            }
        }

        _effectActive[index.x, index.y] = false;
    }
}
