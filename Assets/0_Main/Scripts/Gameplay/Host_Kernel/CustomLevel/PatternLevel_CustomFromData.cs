using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PatternLevel_CustomFromData : PatternLevelSetupBase
{
    private const int SafeLayer = 1000;
    private const int ForbiddenLayer = 0;
    private const int WeaknessLayer = -100;

    [Header("Data Source (optional)")]
    [SerializeField]
    private TextAsset singleLevelJson;

    [SerializeField]
    private bool loadJsonOnAwake = false;

    [Header("Runtime Overrides (do not write back to data)")]
    [SerializeField]
    private bool overrideForbiddenFrameCooldown = false;

    [SerializeField, Min(0.0f)]
    private float forbiddenFrameCooldownOverrideSeconds = 0.25f;

    private CustomLevelDataModel _data;

    private bool _loggedMissingData;
    private bool _loggedGridMismatch;

    private void Awake()
    {
        if (!loadJsonOnAwake)
        {
            return;
        }

        if (singleLevelJson == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] loadJsonOnAwake is true but singleLevelJson is null.", this);
            return;
        }

        SetDataFromJson(singleLevelJson.text);
    }

    public void SetData(CustomLevelDataModel data)
    {
        if (data == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] SetData received null CustomLevelDataModel.", this);
            _data = null;
            return;
        }

        _data = data;
        _loggedMissingData = false;
        _loggedGridMismatch = false;
    }

    /// <summary>
    /// NOTE: Unity JsonUtility cannot parse a root JSON array.
    /// This expects a single CustomLevelDataModel object JSON.
    /// </summary>
    public void SetDataFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[PatternLevel_CustomFromData] SetDataFromJson received null/empty json.", this);
            return;
        }

        try
        {
            CustomLevelDataModel data = JsonUtility.FromJson<CustomLevelDataModel>(json);
            if (data == null)
            {
                Debug.LogError("[PatternLevel_CustomFromData] JsonUtility returned null CustomLevelDataModel.", this);
                return;
            }

            SetData(data);
        }
        catch (Exception ex)
        {
            Debug.LogError("[PatternLevel_CustomFromData] Failed to parse json into CustomLevelDataModel. " + ex, this);
        }
    }

    protected override void BuildLevelPatterns(List<PatternInstance> buffer, int gridWidth, int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] buffer is null.", this);
            return;
        }

        if (_data == null)
        {
            if (!_loggedMissingData)
            {
                _loggedMissingData = true;
                Debug.LogError("[PatternLevel_CustomFromData] No CustomLevelDataModel assigned. Call SetData() before InitializeLevel().", this);
            }
            return;
        }

        if (_data.gridWidth != gridWidth || _data.gridHeight != gridHeight)
        {
            if (!_loggedGridMismatch)
            {
                _loggedGridMismatch = true;
                Debug.LogError(
                    "[PatternLevel_CustomFromData] Grid size mismatch. Data=" +
                    _data.gridWidth + "x" + _data.gridHeight +
                    ", Runtime=" + gridWidth + "x" + gridHeight,
                    this);
            }
            return;
        }

        // --- Safe pattern (single frame, no logic, origin (0,0)) ---
        PatternDefinition safeDef = BuildSingleFrameDefinitionFromFrameData(
            "CustomSafe_" + _data.id,
            _data.SafePattern,
            gridWidth,
            gridHeight,
            CellRole.Safe,
            CellColor.Green);

        if (safeDef != null)
        {
            buffer.Add(new PatternInstance(safeDef, new GridIndex(0, 0), SafeLayer, null));
        }

        // --- Forbidden pattern (multi-frame, FrameLoopLogic, origin (0,0)) ---
        PatternDefinition forbiddenDef = BuildMultiFrameDefinitionFromFramesData(
            "CustomForbidden_" + _data.id,
            _data.ForbiddenFrames,
            gridWidth,
            gridHeight,
            CellRole.Forbidden,
            CellColor.Red);

        if (forbiddenDef != null)
        {
            float cooldown = _data.defaultFrameCooldownSeconds;
            if (overrideForbiddenFrameCooldown)
            {
                cooldown = forbiddenFrameCooldownOverrideSeconds;
            }

            IPatternUpdateLogic logic = new FrameLoopLogic(cooldown, 0);
            buffer.Add(new PatternInstance(forbiddenDef, new GridIndex(0, 0), ForbiddenLayer, logic));
        }

        // --- Weakness (one PatternInstance per cell) ---
        AddWeaknessPoints(buffer, _data.WeaknessCells, gridWidth, gridHeight);
    }

    protected override void BuildStandbyPatterns(List<PatternInstance> buffer, int gridWidth, int gridHeight)
    {
        if (buffer == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] standby buffer is null.", this);
            return;
        }

        if (_data == null)
        {
            if (!_loggedMissingData)
            {
                _loggedMissingData = true;
                Debug.LogError("[PatternLevel_CustomFromData] No CustomLevelDataModel assigned for standby.", this);
            }
            return;
        }

        if (_data.gridWidth != gridWidth || _data.gridHeight != gridHeight)
        {
            if (!_loggedGridMismatch)
            {
                _loggedGridMismatch = true;
                Debug.LogError(
                    "[PatternLevel_CustomFromData] Standby grid size mismatch. Data=" +
                    _data.gridWidth + "x" + _data.gridHeight +
                    ", Runtime=" + gridWidth + "x" + gridHeight,
                    this);
            }
            return;
        }

        // Standby: safe only (keep simple)
        PatternDefinition safeDef = BuildSingleFrameDefinitionFromFrameData(
            "CustomSafe_" + _data.id,
            _data.SafePattern,
            gridWidth,
            gridHeight,
            CellRole.Safe,
            CellColor.Green);

        if (safeDef != null)
        {
            buffer.Add(new PatternInstance(safeDef, new GridIndex(0, 0), SafeLayer, null));
        }
    }

    // ---------------------------------------------------------------------
    // Translation helpers
    // ---------------------------------------------------------------------

    private static PatternDefinition BuildSingleFrameDefinitionFromFrameData(
        string id,
        CustomLevelDataModel.Frame frameData,
        int gridWidth,
        int gridHeight,
        CellRole role,
        CellColor color)
    {
        PatternFrame frame = BuildFrameFromFrameData(frameData, gridWidth, gridHeight, role, color);
        if (frame == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] Failed to build safe frame: " + id);
            return null;
        }

        List<PatternFrame> frames = new List<PatternFrame> { frame };
        return new PatternDefinition(id, frames);
    }

    private static PatternDefinition BuildMultiFrameDefinitionFromFramesData(
        string id,
        List<CustomLevelDataModel.Frame> framesData,
        int gridWidth,
        int gridHeight,
        CellRole role,
        CellColor color)
    {
        if (framesData == null || framesData.Count == 0)
        {
            Debug.LogError("[PatternLevel_CustomFromData] ForbiddenFrames is null/empty for: " + id);
            return null;
        }

        List<PatternFrame> frames = new List<PatternFrame>();

        for (int i = 0; i < framesData.Count; i++)
        {
            PatternFrame frame = BuildFrameFromFrameData(framesData[i], gridWidth, gridHeight, role, color);
            if (frame == null)
            {
                Debug.LogError("[PatternLevel_CustomFromData] Failed to build forbidden frame index " + i + " for: " + id);
                continue;
            }

            frames.Add(frame);
        }

        if (frames.Count == 0)
        {
            Debug.LogError("[PatternLevel_CustomFromData] All forbidden frames failed to build for: " + id);
            return null;
        }

        return new PatternDefinition(id, frames);
    }

    private static PatternFrame BuildFrameFromFrameData(
        CustomLevelDataModel.Frame frameData,
        int gridWidth,
        int gridHeight,
        CellRole role,
        CellColor color)
    {
        if (frameData == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] Frame data is null.");
            return null;
        }

        if (frameData.cells == null)
        {
            Debug.LogError("[PatternLevel_CustomFromData] Frame data cells is null.");
            return new PatternFrame(new List<LocalPatternCell>());
        }

        HashSet<Vector2Int> used = new HashSet<Vector2Int>();
        List<LocalPatternCell> cells = new List<LocalPatternCell>();

        for (int i = 0; i < frameData.cells.Count; i++)
        {
            CustomLevelDataModel.Cell c = frameData.cells[i];
            if (c == null)
            {
                continue;
            }

            int x = c.x;
            int y = c.y;

            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                continue;
            }

            Vector2Int key = new Vector2Int(x, y);
            if (!used.Add(key))
            {
                continue;
            }

            CellOffset offset = new CellOffset(x, y);
            LocalPatternCell local = new LocalPatternCell(offset, role, color);
            cells.Add(local);
        }

        return new PatternFrame(cells);
    }

    private static void AddWeaknessPoints(
        List<PatternInstance> buffer,
        List<CustomLevelDataModel.Cell> weaknessCells,
        int gridWidth,
        int gridHeight)
    {
        if (weaknessCells == null || weaknessCells.Count == 0)
        {
            return;
        }

        PatternDefinition weaknessPointDef = PatternPool.CreateWeaknessPointPattern();

        HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        for (int i = 0; i < weaknessCells.Count; i++)
        {
            CustomLevelDataModel.Cell c = weaknessCells[i];
            if (c == null)
            {
                continue;
            }

            int x = c.x;
            int y = c.y;

            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                continue;
            }

            Vector2Int key = new Vector2Int(x, y);
            if (!used.Add(key))
            {
                continue;
            }

            buffer.Add(new PatternInstance(
                weaknessPointDef,
                new GridIndex(x, y),
                WeaknessLayer,
                null));
        }
    }
}
