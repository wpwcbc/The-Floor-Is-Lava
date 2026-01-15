using System.Collections;
using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public sealed class GameBootstrapper : MonoBehaviour
{
    [SerializeField]
    private FullGridGameController gameController;

    [SerializeField]
    private PatternLevelSetupBase[] levels;
    private PatternLevelSetupBase _selectedLevel;

    [SerializeField]
    private LinkToViewportInterface linkToViewportInterface;

    [Header("UI")]
    [SerializeField]
    private GameObject uiPanel;

    [SerializeField]
    private Text uiText;

    [Header("Demo Grid Size")]
    [SerializeField]
    private int demoGridWidth = 32;

    [SerializeField]
    private int demoGridHeight = 18;

    [Header("Demo Viewport Config")]
    [SerializeField]
    private Vector2Int demoWorldOrigin = new Vector2Int(16, 9);

    [SerializeField]
    private Vector2Int demoLocalOrigin = new Vector2Int(0, 0);

    [SerializeField]
    private int demoViewportWidth = 16;

    [SerializeField]
    private int demoViewportHeight = 9;

    [SerializeField]
    private int demoViewportDirection = 0;

    [Header("HP")]
    [SerializeField]
    private int startingHp = 50;

    private int _currentHp;

    private IFullGridGameControl _game;
    private readonly List<Vector2Int> _weaknessBuffer = new List<Vector2Int>();

    // Countdown runtime
    private Coroutine _countdownCoroutine;
    private bool _countdownRunning;

    // Game state (wrapper-side)
    private bool _levelRunning;
    private bool _gameEnded;

    // WIN sequence (delayed stop to allow one more resolve + render)
    private Coroutine _winSequenceCoroutine;
    private bool _winSequenceRunning;

    [Header("Immunity")]
    [SerializeField]
    private float weaknessTouchImmunitySeconds = 0.25f;

    private readonly Dictionary<Vector2Int, float> _immuneUntilUnscaledTime =
        new Dictionary<Vector2Int, float>();

    private float Now
    {
        get { return Time.unscaledTime; }
    }

    private void ClearImmunity()
    {
        _immuneUntilUnscaledTime.Clear();
    }

    private void GrantImmunity(Vector2Int worldIndex)
    {
        if (weaknessTouchImmunitySeconds <= 0.0f)
        {
            return;
        }

        float until = Now + weaknessTouchImmunitySeconds;
        _immuneUntilUnscaledTime[worldIndex] = until;
    }

    private bool IsImmune(Vector2Int worldIndex)
    {
        float until;
        if (!_immuneUntilUnscaledTime.TryGetValue(worldIndex, out until))
        {
            return false;
        }

        if (Now < until)
        {
            return true;
        }

        _immuneUntilUnscaledTime.Remove(worldIndex);
        return false;
    }

    private void CleanupExpiredImmunity()
    {
        if (_immuneUntilUnscaledTime.Count == 0)
        {
            return;
        }

        // Small count => simple sweep is fine.
        List<Vector2Int> toRemove = null;
        foreach (KeyValuePair<Vector2Int, float> kv in _immuneUntilUnscaledTime)
        {
            if (Now >= kv.Value)
            {
                if (toRemove == null)
                {
                    toRemove = new List<Vector2Int>();
                }

                toRemove.Add(kv.Key);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            _immuneUntilUnscaledTime.Remove(toRemove[i]);
        }
    }

    private void Awake()
    {
        if (startingHp < 0)
        {
            startingHp = 0;
        }

        _currentHp = startingHp;

        if (gameController == null)
        {
            gameController = FindFirstObjectByType<FullGridGameController>();
            if (gameController == null)
            {
                Debug.LogError("[GameBootstrapper] gameController is null and not found in scene.", this);
                return;
            }
        }

        _game = gameController as IFullGridGameControl;
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] gameController does not implement IFullGridGameControl.", this);
        }
    }

    private void OnEnable()
    {
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] OnEnable but _game is null. Cannot subscribe to events.", this);
            return;
        }

        _game.WorldCellTouched += OnWorldCellTouched;
    }

    private void OnDisable()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
            _countdownRunning = false;
        }

        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
            _winSequenceRunning = false;
        }

        if (_game == null)
        {
            return;
        }

        _game.WorldCellTouched -= OnWorldCellTouched;
    }

    private void Start()
    {
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] Start called but _game is null.", this);
            return;
        }

        _selectedLevel = PickRandomLevel();
        if (_selectedLevel == null)
        {
            Debug.LogError("[GameBootstrapper] No valid levels assigned in 'levels' array.", this);
            return;
        }

        Demo_ConfigureGridAndLevel();
        Demo_ConfigureViewport();
        Demo_ShowStandby();

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        PushHpToSafeCells();
    }

    private void Update()
    {
        CleanupExpiredImmunity();

        if (!_levelRunning || _gameEnded || _game == null || _winSequenceRunning)
        {
            return;
        }

        int weaknessCount = _game.GetWeaknessCellCount();
        if (weaknessCount <= 0)
        {
            BeginWinSequence();
        }
    }

    private void PushHpToSafeCells()
    {
        if (linkToViewportInterface == null)
        {
            Debug.LogError("[GameBootstrapper] PushHpToSafeCells called but linkToViewportInterface is null.", this);
            return;
        }

        if (_currentHp < 0)
        {
            _currentHp = 0;
        }

        string hpText = _currentHp.ToString();
        linkToViewportInterface.SetRoleUiText(CellRole.Safe, hpText);
    }

    private void ShowPanelText(string text)
    {
        if (uiPanel == null)
        {
            Debug.LogError("[GameBootstrapper] ShowPanelText called but uiPanel is null.", this);
            return;
        }

        if (uiText == null)
        {
            Debug.LogError("[GameBootstrapper] ShowPanelText called but uiText is null.", this);
            return;
        }

        uiPanel.SetActive(true);
        uiText.text = text;
    }

    private void HidePanel()
    {
        if (uiPanel == null)
        {
            return;
        }

        uiPanel.SetActive(false);
    }

    // ----------------------------
    // WIN: delayed stop sequence
    // ----------------------------

    private void BeginWinSequence()
    {
        if (_gameEnded)
        {
            return;
        }

        if (_winSequenceRunning)
        {
            return;
        }

        _winSequenceRunning = true;

        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
        }

        _winSequenceCoroutine = StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        // Allow ONE more frame for:
        // - PatternToGridResolver.Update() to publish cleared grid
        // - GridViewportRenderer.LateUpdate() to render it
        yield return null;
        yield return new WaitForEndOfFrame();

        // If GAME OVER happened during the wait, do nothing.
        if (_gameEnded)
        {
            _winSequenceRunning = false;
            _winSequenceCoroutine = null;
            yield break;
        }

        EndGameWinImmediate();
    }

    private void EndGameWinImmediate()
    {
        if (_gameEnded)
        {
            return;
        }

        _gameEnded = true;
        _levelRunning = false;

        if (_game != null)
        {
            _game.StopLevel();
        }

        ShowPanelText("WIN");

        _winSequenceRunning = false;

        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
        }
    }

    private void EndGameOver()
    {
        if (_gameEnded)
        {
            return;
        }

        // Cancel pending WIN sequence if any.
        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
            _winSequenceRunning = false;
        }

        _gameEnded = true;
        _levelRunning = false;

        if (_game != null)
        {
            _game.StopLevel();
        }

        ShowPanelText("GAME OVER");
    }

    // --------------------------------------------------------------------
    // Demo pieces
    // --------------------------------------------------------------------

    private void Demo_ConfigureGridAndLevel()
    {
        Debug.Log("[GameBootstrapper] Configuring full grid and setting active level.", this);

        _game.ConfigureFullGrid(demoGridWidth, demoGridHeight);
        _game.SetActiveLevel(_selectedLevel);

        int w = _game.GridWidth;
        int h = _game.GridHeight;
        Debug.Log("[GameBootstrapper] Grid configured to " + w + " x " + h + " cells.", this);
    }

    private void Demo_ConfigureViewport()
    {
        if (linkToViewportInterface == null)
        {
            Debug.LogError("[GameBootstrapper] linkToViewportInterface is null. Viewport will not be configured.", this);
            return;
        }

        Debug.Log("[GameBootstrapper] Configuring viewport via LinkToViewportInterface.", this);

        linkToViewportInterface.ConfigureViewport(
            demoWorldOrigin,
            demoLocalOrigin,
            demoViewportWidth,
            demoViewportHeight,
            demoViewportDirection);

        linkToViewportInterface.SetViewportEnabled(true);
    }

    private void Demo_ShowStandby()
    {
        Debug.Log("[GameBootstrapper] Showing standby pattern (safe area, etc.).", this);
        _game.ShowStandby();
    }

    // --------------------------------------------------------------------
    // Buttons
    // --------------------------------------------------------------------

    [ProButton]
    public void StartCountDown()
    {
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] StartCountDown called but _game is null.", this);
            return;
        }

        if (uiPanel == null)
        {
            Debug.LogError("[GameBootstrapper] StartCountDown called but uiPanel is null.", this);
            return;
        }

        if (uiText == null)
        {
            Debug.LogError("[GameBootstrapper] StartCountDown called but uiText is null.", this);
            return;
        }

        if (_countdownRunning)
        {
            Debug.LogError("[GameBootstrapper] StartCountDown called but countdown is already running.", this);
            return;
        }

        _gameEnded = false;
        _levelRunning = false;

        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
            _winSequenceRunning = false;
        }

        uiPanel.SetActive(true);
        uiText.text = string.Empty;

        _countdownRunning = true;

        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        ClearImmunity();

        _countdownCoroutine = StartCoroutine(CountDownRoutine());
    }

    private IEnumerator CountDownRoutine()
    {
        uiText.text = "3";
        yield return new WaitForSeconds(1.0f);

        uiText.text = "2";
        yield return new WaitForSeconds(1.0f);

        uiText.text = "1";
        yield return new WaitForSeconds(1.0f);

        uiText.text = "GO";
        yield return new WaitForSeconds(1.0f);

        HidePanel();

        _countdownRunning = false;
        _countdownCoroutine = null;

        StartLevel();
    }

    [ProButton]
    public void StartLevel()
    {
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] StartLevel button pressed but _game is null.", this);
            return;
        }

        if (_gameEnded)
        {
            Debug.LogError("[GameBootstrapper] StartLevel called but game is ended. Use StartCountDown / reset state first.", this);
            return;
        }

        Debug.Log("[GameBootstrapper] StartLevel called.", this);

        ClearImmunity();

        _game.StartLevel();
        _levelRunning = true;

        PushHpToSafeCells();

        int weaknessCount = _game.GetWeaknessCellCount();
        if (weaknessCount <= 0)
        {
            BeginWinSequence();
        }
    }

    [ProButton]
    public void StopLevel()
    {
        if (_game == null)
        {
            Debug.LogError("[GameBootstrapper] StopLevel button pressed but _game is null.", this);
            return;
        }

        Debug.Log("[GameBootstrapper] StopLevel called.", this);

        _game.StopLevel();
        _levelRunning = false;
    }

    [ProButton]
    public void ResetHpToStarting()
    {
        _currentHp = startingHp;
        PushHpToSafeCells();
        Debug.Log("[GameBootstrapper] HP reset to " + _currentHp + ".", this);
    }

    [ProButton]
    public void Restart()
    {
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    private PatternLevelSetupBase PickRandomLevel()
    {
        if (levels == null || levels.Length == 0)
        {
            return null;
        }

        // Build a list of valid entries (ignore nulls).
        List<PatternLevelSetupBase> valid = null;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null)
            {
                continue;
            }

            if (valid == null)
            {
                valid = new List<PatternLevelSetupBase>();
            }

            valid.Add(levels[i]);
        }

        if (valid == null || valid.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, valid.Count);
        return valid[index];
    }



    // --------------------------------------------------------------------
    // WorldCellTouched: deduct HP on Forbidden touch
    // --------------------------------------------------------------------

    private void OnWorldCellTouched(Vector2Int worldIndex, CellRole role, CellColor color)
    {
        if (_gameEnded || !_levelRunning)
        {
            return;
        }

        if (role == CellRole.Weakness)
        {
            GrantImmunity(worldIndex);
            // no return
        }

        if (role != CellRole.Forbidden)
        {
            return;
        }

        if (IsImmune(worldIndex))
        {
            return;
        }

        if (_currentHp <= 0)
        {
            return;
        }

        _currentHp -= 1;
        if (_currentHp < 0)
        {
            _currentHp = 0;
        }

        PushHpToSafeCells();

        if (_currentHp <= 0)
        {
            EndGameOver();
        }
    }

}
