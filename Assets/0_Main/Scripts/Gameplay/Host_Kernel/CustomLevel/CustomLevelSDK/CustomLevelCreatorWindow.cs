#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class CustomLevelCreatorWindow : EditorWindow
{
    private enum BasePaintMode
    {
        Safe = 0,
        Weakness = 1,
        Erase = 2
    }

    private enum ForbiddenPaintMode
    {
        Forbidden = 0,
        Erase = 1
    }

    // ---------------------------
    // Authoring state (in editor)
    // ---------------------------

    private string _id = string.Empty;
    private string _levelName = "Untitled";
    private int _gridWidth = 16;
    private int _gridHeight = 9;
    private float _defaultFrameCooldownSeconds = 0.25f;

    private BasePaintMode _basePaintMode = BasePaintMode.Safe;
    private ForbiddenPaintMode _forbiddenPaintMode = ForbiddenPaintMode.Forbidden;

    // Base layers
    private readonly HashSet<Vector2Int> _safeCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _weaknessCells = new HashSet<Vector2Int>();

    // Forbidden: multi-frame
    private readonly List<HashSet<Vector2Int>> _forbiddenFrames = new List<HashSet<Vector2Int>>();
    private int _activeForbiddenFrameIndex = 0;

    // Painting drag state (shared)
    private bool _isDragging = false;
    private bool _dragAdd = true;
    private Vector2Int _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
    private bool _warnedForbiddenOnSafeThisDrag = false;

    // Optional: explicit harness reference (recommended)
    private CustomLevelPlaytestHarness _harness;

    // UI
    private Vector2 _scroll;

    [MenuItem("Tools/Floor Is Lava/Custom Level Creator")]
    public static void Open()
    {
        CustomLevelCreatorWindow window = GetWindow<CustomLevelCreatorWindow>("Custom Level Creator");
        window.minSize = new Vector2(520.0f, 600.0f);
    }

    private void OnEnable()
    {
        EnsureId();
        EnsureForbiddenFramesAtLeastOne();
        ClampAndSanitize();
    }

    private void OnGUI()
    {
        EnsureId();
        EnsureForbiddenFramesAtLeastOne();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        EditorGUILayout.Space(8.0f);

        DrawLevelFields();
        EditorGUILayout.Space(8.0f);

        DrawForbiddenFramesControls();
        EditorGUILayout.Space(8.0f);

        DrawPlaytestHarnessField();
        EditorGUILayout.Space(8.0f);

        DrawBaseGrid();
        EditorGUILayout.Space(12.0f);

        DrawForbiddenGrid();
        EditorGUILayout.Space(12.0f);

        DrawActions();

        EditorGUILayout.EndScrollView();

        HandleMouseUpOutsideWindow();
    }

    // ---------------------------
    // UI blocks
    // ---------------------------

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Custom Level Authoring", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("ID", GUILayout.Width(24.0f));
            EditorGUILayout.SelectableLabel(_id, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button("Regenerate", GUILayout.Width(96.0f)))
            {
                _id = Guid.NewGuid().ToString("N");
                Repaint();
            }
        }
    }

    private void DrawLevelFields()
    {
        EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);

        _levelName = EditorGUILayout.TextField("Name", _levelName);

        int newWidth = EditorGUILayout.IntField("Grid Width", _gridWidth);
        int newHeight = EditorGUILayout.IntField("Grid Height", _gridHeight);

        if (newWidth < 1) newWidth = 1;
        if (newHeight < 1) newHeight = 1;

        if (newWidth != _gridWidth || newHeight != _gridHeight)
        {
            _gridWidth = newWidth;
            _gridHeight = newHeight;
            ClampAndSanitize();
        }

        _defaultFrameCooldownSeconds = EditorGUILayout.FloatField("Default Forbidden CD (s)", _defaultFrameCooldownSeconds);
        if (_defaultFrameCooldownSeconds < 0.0f)
        {
            _defaultFrameCooldownSeconds = 0.0f;
        }
    }

    private void DrawForbiddenFramesControls()
    {
        EditorGUILayout.LabelField("Forbidden Frames", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Frame Count", GUILayout.Width(80.0f));
            int count = _forbiddenFrames.Count;
            EditorGUILayout.LabelField(count.ToString(), GUILayout.Width(40.0f));

            if (GUILayout.Button("+", GUILayout.Width(28.0f)))
            {
                _forbiddenFrames.Add(new HashSet<Vector2Int>());
                _activeForbiddenFrameIndex = _forbiddenFrames.Count - 1;
            }

            using (new EditorGUI.DisabledScope(_forbiddenFrames.Count <= 1))
            {
                if (GUILayout.Button("-", GUILayout.Width(28.0f)))
                {
                    int removeIndex = Mathf.Clamp(_activeForbiddenFrameIndex, 0, _forbiddenFrames.Count - 1);
                    _forbiddenFrames.RemoveAt(removeIndex);
                    _activeForbiddenFrameIndex = Mathf.Clamp(_activeForbiddenFrameIndex, 0, _forbiddenFrames.Count - 1);
                }
            }

            GUILayout.FlexibleSpace();
        }

        if (_forbiddenFrames.Count > 0)
        {
            int newIndex = EditorGUILayout.IntSlider("Active Frame", _activeForbiddenFrameIndex, 0, _forbiddenFrames.Count - 1);
            if (newIndex != _activeForbiddenFrameIndex)
            {
                _activeForbiddenFrameIndex = newIndex;
                Repaint();
            }
        }
    }

    private void DrawPlaytestHarnessField()
    {
        EditorGUILayout.LabelField("Playtest", EditorStyles.boldLabel);

        _harness = (CustomLevelPlaytestHarness)EditorGUILayout.ObjectField(
            "Harness (Scene Object)",
            _harness,
            typeof(CustomLevelPlaytestHarness),
            true);

        EditorGUILayout.HelpBox(
            "Recommended: drag the CustomLevelPlaytestHarness from Hierarchy into this field. " +
            "This avoids finding the wrong object at runtime.",
            MessageType.Info);
    }

    private void DrawBaseGrid()
    {
        EditorGUILayout.LabelField("Grid A: Base (Safe + Weakness, cannot overlap)", EditorStyles.boldLabel);

        _basePaintMode = (BasePaintMode)EditorGUILayout.EnumPopup("Paint", _basePaintMode);

        float cellSize = 22.0f;
        float padding = 6.0f;

        float desiredWidth = (_gridWidth * cellSize) + (2.0f * padding);
        float desiredHeight = (_gridHeight * cellSize) + (2.0f * padding);

        Rect rect = GUILayoutUtility.GetRect(desiredWidth, desiredHeight, GUILayout.ExpandWidth(true));
        Rect gridRect = new Rect(rect.x + padding, rect.y + padding, _gridWidth * cellSize, _gridHeight * cellSize);

        DrawGridBackground(gridRect);
        DrawBaseCells(gridRect, cellSize);
        HandleGridMouse_Base(gridRect, cellSize);

        EditorGUILayout.Space(4.0f);
        EditorGUILayout.LabelField("Rule: Safe and Weakness overwrite each other.");
    }

    private void DrawForbiddenGrid()
    {
        EditorGUILayout.LabelField("Grid B: Forbidden (Safe/Weakness as background; Forbidden cannot paint on Safe)", EditorStyles.boldLabel);

        _forbiddenPaintMode = (ForbiddenPaintMode)EditorGUILayout.EnumPopup("Paint", _forbiddenPaintMode);

        float cellSize = 22.0f;
        float padding = 6.0f;

        float desiredWidth = (_gridWidth * cellSize) + (2.0f * padding);
        float desiredHeight = (_gridHeight * cellSize) + (2.0f * padding);

        Rect rect = GUILayoutUtility.GetRect(desiredWidth, desiredHeight, GUILayout.ExpandWidth(true));
        Rect gridRect = new Rect(rect.x + padding, rect.y + padding, _gridWidth * cellSize, _gridHeight * cellSize);

        DrawGridBackground(gridRect);
        DrawForbiddenWithBaseBackground(gridRect, cellSize);
        HandleGridMouse_Forbidden(gridRect, cellSize);

        EditorGUILayout.Space(4.0f);
        EditorGUILayout.LabelField("Rule: Forbidden add is blocked on Safe cells.");
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Safe"))
            {
                _safeCells.Clear();
                Repaint();
            }

            if (GUILayout.Button("Clear Weakness"))
            {
                _weaknessCells.Clear();
                Repaint();
            }

            if (GUILayout.Button("Clear Forbidden (Active Frame)"))
            {
                HashSet<Vector2Int> frame = GetActiveForbiddenFrame();
                if (frame != null)
                {
                    frame.Clear();
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space(8.0f);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Playtest In Running Scene", GUILayout.Height(28.0f)))
            {
                CustomLevelPlaytestHarness harness = ResolveHarness();
                if (harness == null)
                {
                    Debug.LogError("[CustomLevelCreatorWindow] No CustomLevelPlaytestHarness found/assigned in the active scene.");
                    return;
                }

                CustomLevelDataModel model = BuildModelFromEditorState();

                if (!LogModelSummaryAndValidate(model))
                {
                    return;
                }

                harness.ApplyAndPlaytest(model);
                Debug.Log("[CustomLevelCreatorWindow] ApplyAndPlaytest invoked.");
            }
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode in the Playtest scene to use Playtest injection.", MessageType.Info);
        }

        EditorGUILayout.Space(8.0f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy JSON To Clipboard"))
            {
                CustomLevelDataModel model = BuildModelFromEditorState();
                string json = JsonUtility.ToJson(model, true);
                EditorGUIUtility.systemCopyBuffer = json;
                Debug.Log("[CustomLevelCreatorWindow] Level JSON copied to clipboard.");
            }

            if (GUILayout.Button("Log JSON"))
            {
                CustomLevelDataModel model = BuildModelFromEditorState();
                string json = JsonUtility.ToJson(model, true);
                Debug.Log(json);
            }
        }

        EditorGUILayout.Space(6.0f);

        if (GUILayout.Button("Create JSON File...", GUILayout.Height(24.0f)))
        {
            CreateJsonFileAsset();
        }
    }

    // ---------------------------
    // Grid drawing
    // ---------------------------

    private void DrawGridBackground(Rect gridRect)
    {
        EditorGUI.DrawRect(gridRect, new Color(0.12f, 0.12f, 0.12f, 1.0f));
    }

    private void DrawBaseCells(Rect gridRect, float cellSize)
    {
        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + (x * cellSize),
                    gridRect.y + ((_gridHeight - 1 - y) * cellSize),
                    cellSize - 1.0f,
                    cellSize - 1.0f);

                Vector2Int cell = new Vector2Int(x, y);

                EditorGUI.DrawRect(cellRect, new Color(0.18f, 0.18f, 0.18f, 1.0f));

                if (_safeCells.Contains(cell))
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.10f, 0.55f, 0.10f, 0.80f));
                }

                if (_weaknessCells.Contains(cell))
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.10f, 0.35f, 0.75f, 0.80f));
                }
            }
        }
    }

    private void DrawForbiddenWithBaseBackground(Rect gridRect, float cellSize)
    {
        HashSet<Vector2Int> activeForbidden = GetActiveForbiddenFrame();

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + (x * cellSize),
                    gridRect.y + ((_gridHeight - 1 - y) * cellSize),
                    cellSize - 1.0f,
                    cellSize - 1.0f);

                Vector2Int cell = new Vector2Int(x, y);

                // Base
                EditorGUI.DrawRect(cellRect, new Color(0.18f, 0.18f, 0.18f, 1.0f));

                // Background: Safe/Weakness (fainter)
                if (_safeCells.Contains(cell))
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.10f, 0.55f, 0.10f, 0.35f));
                }

                if (_weaknessCells.Contains(cell))
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.10f, 0.35f, 0.75f, 0.35f));
                }

                // Foreground: Forbidden
                if (activeForbidden != null && activeForbidden.Contains(cell))
                {
                    EditorGUI.DrawRect(cellRect, new Color(0.70f, 0.10f, 0.10f, 0.80f));
                }
            }
        }
    }

    // ---------------------------
    // Mouse interaction
    // ---------------------------

    private void HandleGridMouse_Base(Rect gridRect, float cellSize)
    {
        Event e = Event.current;
        if (e == null) return;
        if (!gridRect.Contains(e.mousePosition)) return;

        Vector2Int cell;
        if (!TryGetCellAtMouse(gridRect, cellSize, e.mousePosition, out cell)) return;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            _isDragging = true;
            _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

            bool exists = IsCellInBaseLayer(cell, _basePaintMode);
            _dragAdd = !exists;

            ApplyPaint_Base(cell, _dragAdd);
            _lastPaintedCell = cell;

            e.Use();
            Repaint();
            return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (!_isDragging) return;

            if (cell != _lastPaintedCell)
            {
                ApplyPaint_Base(cell, _dragAdd);
                _lastPaintedCell = cell;
                Repaint();
            }

            e.Use();
            return;
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            _isDragging = false;
            e.Use();
            return;
        }
    }

    private void HandleGridMouse_Forbidden(Rect gridRect, float cellSize)
    {
        Event e = Event.current;
        if (e == null) return;
        if (!gridRect.Contains(e.mousePosition)) return;

        Vector2Int cell;
        if (!TryGetCellAtMouse(gridRect, cellSize, e.mousePosition, out cell)) return;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            _isDragging = true;
            _warnedForbiddenOnSafeThisDrag = false;
            _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

            bool exists = IsCellForbidden(cell);
            _dragAdd = !exists;

            ApplyPaint_Forbidden(cell, _dragAdd);
            _lastPaintedCell = cell;

            e.Use();
            Repaint();
            return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (!_isDragging) return;

            if (cell != _lastPaintedCell)
            {
                ApplyPaint_Forbidden(cell, _dragAdd);
                _lastPaintedCell = cell;
                Repaint();
            }

            e.Use();
            return;
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            _isDragging = false;
            _warnedForbiddenOnSafeThisDrag = false;
            e.Use();
            return;
        }
    }

    private void HandleMouseUpOutsideWindow()
    {
        Event e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseUp)
        {
            _isDragging = false;
            _warnedForbiddenOnSafeThisDrag = false;
        }
    }

    private bool TryGetCellAtMouse(Rect gridRect, float cellSize, Vector2 mouse, out Vector2Int cell)
    {
        float localX = mouse.x - gridRect.x;
        float localY = mouse.y - gridRect.y;

        int x = Mathf.FloorToInt(localX / cellSize);
        int yFromTop = Mathf.FloorToInt(localY / cellSize);
        int y = (_gridHeight - 1) - yFromTop;

        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
        {
            cell = Vector2Int.zero;
            return false;
        }

        cell = new Vector2Int(x, y);
        return true;
    }

    private bool IsCellInBaseLayer(Vector2Int cell, BasePaintMode mode)
    {
        if (mode == BasePaintMode.Safe)
        {
            return _safeCells.Contains(cell);
        }

        if (mode == BasePaintMode.Weakness)
        {
            return _weaknessCells.Contains(cell);
        }

        // Erase: treat "exists" if in either
        return _safeCells.Contains(cell) || _weaknessCells.Contains(cell);
    }

    private bool IsCellForbidden(Vector2Int cell)
    {
        HashSet<Vector2Int> frame = GetActiveForbiddenFrame();
        if (frame == null) return false;
        return frame.Contains(cell);
    }

    private void ApplyPaint_Base(Vector2Int cell, bool add)
    {
        if (_basePaintMode == BasePaintMode.Erase)
        {
            _safeCells.Remove(cell);
            _weaknessCells.Remove(cell);
            return;
        }

        if (_basePaintMode == BasePaintMode.Safe)
        {
            if (add)
            {
                _safeCells.Add(cell);
                _weaknessCells.Remove(cell); // overwrite rule
            }
            else
            {
                _safeCells.Remove(cell);
            }
            return;
        }

        if (_basePaintMode == BasePaintMode.Weakness)
        {
            if (add)
            {
                _weaknessCells.Add(cell);
                _safeCells.Remove(cell); // overwrite rule
            }
            else
            {
                _weaknessCells.Remove(cell);
            }
            return;
        }
    }

    private void ApplyPaint_Forbidden(Vector2Int cell, bool add)
    {
        HashSet<Vector2Int> frame = GetActiveForbiddenFrame();
        if (frame == null)
        {
            Debug.LogError("[CustomLevelCreatorWindow] Active forbidden frame is null.");
            return;
        }

        if (_forbiddenPaintMode == ForbiddenPaintMode.Erase)
        {
            frame.Remove(cell);
            return;
        }

        // Forbidden mode
        if (add)
        {
            if (_safeCells.Contains(cell))
            {
                if (!_warnedForbiddenOnSafeThisDrag)
                {
                    _warnedForbiddenOnSafeThisDrag = true;
                    Debug.LogWarning("[CustomLevelCreatorWindow] Forbidden cannot be painted onto Safe cells.");
                }
                return;
            }

            frame.Add(cell);
        }
        else
        {
            frame.Remove(cell);
        }
    }

    private HashSet<Vector2Int> GetActiveForbiddenFrame()
    {
        if (_forbiddenFrames == null || _forbiddenFrames.Count == 0) return null;
        int index = Mathf.Clamp(_activeForbiddenFrameIndex, 0, _forbiddenFrames.Count - 1);
        return _forbiddenFrames[index];
    }

    // ---------------------------
    // Model build / sanitation
    // ---------------------------

    private void EnsureId()
    {
        if (string.IsNullOrEmpty(_id))
        {
            _id = Guid.NewGuid().ToString("N");
        }
    }

    private void EnsureForbiddenFramesAtLeastOne()
    {
        if (_forbiddenFrames.Count == 0)
        {
            _forbiddenFrames.Add(new HashSet<Vector2Int>());
            _activeForbiddenFrameIndex = 0;
        }
    }

    private void ClampAndSanitize()
    {
        if (_gridWidth < 1) _gridWidth = 1;
        if (_gridHeight < 1) _gridHeight = 1;

        RemoveOutOfBounds(_safeCells);
        RemoveOutOfBounds(_weaknessCells);

        for (int i = 0; i < _forbiddenFrames.Count; i++)
        {
            HashSet<Vector2Int> frame = _forbiddenFrames[i];
            if (frame == null)
            {
                _forbiddenFrames[i] = new HashSet<Vector2Int>();
                continue;
            }
            RemoveOutOfBounds(frame);
        }

        _activeForbiddenFrameIndex = Mathf.Clamp(_activeForbiddenFrameIndex, 0, Mathf.Max(0, _forbiddenFrames.Count - 1));
    }

    private void RemoveOutOfBounds(HashSet<Vector2Int> set)
    {
        if (set == null || set.Count == 0) return;

        List<Vector2Int> toRemove = null;
        foreach (Vector2Int c in set)
        {
            if (c.x < 0 || c.x >= _gridWidth || c.y < 0 || c.y >= _gridHeight)
            {
                if (toRemove == null) toRemove = new List<Vector2Int>();
                toRemove.Add(c);
            }
        }

        if (toRemove == null) return;

        for (int i = 0; i < toRemove.Count; i++)
        {
            set.Remove(toRemove[i]);
        }
    }

    private CustomLevelDataModel BuildModelFromEditorState()
    {
        EnsureForbiddenFramesAtLeastOne();

        CustomLevelDataModel model = new CustomLevelDataModel();
        model.id = _id;
        model.name = _levelName;
        model.gridWidth = _gridWidth;
        model.gridHeight = _gridHeight;
        model.defaultFrameCooldownSeconds = _defaultFrameCooldownSeconds;

        CustomLevelDataModel.Frame safeFrame = new CustomLevelDataModel.Frame();
        safeFrame.cells = ToCellsList(_safeCells);
        model.SafePattern = safeFrame;

        List<CustomLevelDataModel.Frame> forbidden = new List<CustomLevelDataModel.Frame>();
        for (int i = 0; i < _forbiddenFrames.Count; i++)
        {
            CustomLevelDataModel.Frame f = new CustomLevelDataModel.Frame();
            f.cells = ToCellsList(_forbiddenFrames[i]);
            forbidden.Add(f);
        }
        model.ForbiddenFrames = forbidden;

        model.WeaknessCells = ToCellsList(_weaknessCells);

        return model;
    }

    private List<CustomLevelDataModel.Cell> ToCellsList(HashSet<Vector2Int> set)
    {
        List<CustomLevelDataModel.Cell> list = new List<CustomLevelDataModel.Cell>();
        if (set == null || set.Count == 0) return list;

        foreach (Vector2Int c in set)
        {
            CustomLevelDataModel.Cell cell = new CustomLevelDataModel.Cell();
            cell.x = c.x;
            cell.y = c.y;
            list.Add(cell);
        }

        return list;
    }

    // ---------------------------
    // Export JSON file
    // ---------------------------

    private void CreateJsonFileAsset()
    {
        CustomLevelDataModel model = BuildModelFromEditorState();
        string json = JsonUtility.ToJson(model, true);

        string safeName = MakeSafeFileName(string.IsNullOrEmpty(_levelName) ? "CustomLevel" : _levelName);
        string defaultFileName = safeName + ".json";

        string path = EditorUtility.SaveFilePanelInProject(
            "Create Custom Level JSON",
            defaultFileName,
            "json",
            "Choose a location under Assets/ to save the level JSON.");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log("[CustomLevelCreatorWindow] Wrote JSON: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("[CustomLevelCreatorWindow] Failed to write JSON file. " + ex);
        }
    }

    private string MakeSafeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
        {
            name = name.Replace(invalid[i].ToString(), "_");
        }

        name = name.Replace(" ", "_");
        return name;
    }

    private CustomLevelPlaytestHarness ResolveHarness()
    {
        if (_harness != null)
        {
            return _harness;
        }

        CustomLevelPlaytestHarness found = UnityEngine.Object.FindFirstObjectByType<CustomLevelPlaytestHarness>();
        return found;
    }

    private bool LogModelSummaryAndValidate(CustomLevelDataModel model)
    {
        if (model == null)
        {
            Debug.LogError("[CustomLevelCreatorWindow] Model is null.");
            return false;
        }

        int safeCount = 0;
        if (model.SafePattern != null && model.SafePattern.cells != null)
        {
            safeCount = model.SafePattern.cells.Count;
        }

        int forbiddenFrameCount = 0;
        int forbiddenCellTotal = 0;
        if (model.ForbiddenFrames != null)
        {
            forbiddenFrameCount = model.ForbiddenFrames.Count;
            for (int i = 0; i < model.ForbiddenFrames.Count; i++)
            {
                CustomLevelDataModel.Frame f = model.ForbiddenFrames[i];
                if (f != null && f.cells != null)
                {
                    forbiddenCellTotal += f.cells.Count;
                }
            }
        }

        int weaknessCount = 0;
        if (model.WeaknessCells != null)
        {
            weaknessCount = model.WeaknessCells.Count;
        }

        Debug.Log(
            "[CustomLevelCreatorWindow] Model Summary: " +
            "name=" + model.name +
            ", id=" + model.id +
            ", grid=" + model.gridWidth + "x" + model.gridHeight +
            ", safe=" + safeCount +
            ", forbiddenFrames=" + forbiddenFrameCount +
            ", forbiddenCellsTotal=" + forbiddenCellTotal +
            ", weakness=" + weaknessCount +
            ", defaultCD=" + model.defaultFrameCooldownSeconds);

        // Minimal validity rule: at least some content
        if (safeCount == 0 && forbiddenCellTotal == 0 && weaknessCount == 0)
        {
            Debug.LogError("[CustomLevelCreatorWindow] Model has no cells (safe/forbidden/weakness all empty).");
            return false;
        }

        return true;
    }

}
#endif
