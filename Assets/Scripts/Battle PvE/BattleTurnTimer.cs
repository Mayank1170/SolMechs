using System;
using UnityEngine;
using UnityEngine.Events;

// Optional UI dependencies
#if TMP_PRESENT
using TMPro;
#endif
using UnityEngine.UI;

/// <summary>
/// Simple chess-like turn timer for PvP/PvE.
/// - Each side has a time bank (decrements only on its own turn)
/// - Optional increment per completed turn
/// - Optional pause during cutscenes/animations
/// - Fires events when a side runs out of time
/// 
/// Hook points you likely have already:
/// - Call Initialize() at battle start
/// - Call BeginTurn(isPlayer) when a new turn starts
/// - Call EndTurn() when the acting side locks its move
/// - Call SetPaused(true/false) around long animations if desired
/// </summary>
public class BattleTurnTimer : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Initial bank per side, seconds.")]
    public int initialBankSeconds = 120; // e.g., 2 minutes like Showdown casual

    [Tooltip("Per-turn increment after ending a turn, seconds (0 = none).")]
    public int incrementPerTurn = 5;     // small reward to keep pace

    [Tooltip("Optional cap for the bank (0 = no cap).")]
    public int maxBankSeconds = 180;

    [Tooltip("Update the display every this many seconds (for perf/UI).")]
    public float displayUpdateStep = 0.05f;

    [Tooltip("If true, time does not tick while paused (e.g. long animations).")]
    public bool pauseWhenBlocked = true;

    [Header("UI (optional)")]
#if TMP_PRESENT
    public TMP_Text playerText;
    public TMP_Text enemyText;
#else
    public Text playerText;
    public Text enemyText;
#endif

    [Header("Events")]
    public UnityEvent onPlayerFlagFall;  // Player ran out of time
    public UnityEvent onEnemyFlagFall;   // Enemy ran out of time

    // Internal state
    private float _playerBank;
    private float _enemyBank;
    private bool _playerTurn;   // whose clock is currently ticking
    private bool _running;      // timer started at least once
    private bool _paused;
    private float _accumDisp;

    public void Initialize()
    {
        _playerBank = initialBankSeconds;
        _enemyBank = initialBankSeconds;
        _running = false;
        _paused = false;
        UpdateDisplay(true);
    }

    /// <summary>
    /// Called by battle flow when a turn starts.
    /// </summary>
    public void BeginTurn(bool attackerIsPlayer)
    {
        _playerTurn = attackerIsPlayer;
        _running = true;
        _accumDisp = 0f;
        UpdateDisplay(true);
    }

    /// <summary>
    /// Called when the acting side confirmed a move (locks decision).
    /// Adds increment and stops ticking until next BeginTurn.
    /// </summary>
    public void EndTurn()
    {
        if (!_running) return;

        if (_playerTurn)
            _playerBank = ApplyIncrement(_playerBank);
        else
            _enemyBank = ApplyIncrement(_enemyBank);

        _running = false;
        UpdateDisplay(true);
    }

    /// <summary>
    /// Use around long animations/cutscenes if you don't want to consume player's clock.
    /// </summary>
    public void SetPaused(bool paused) => _paused = paused;

    private float ApplyIncrement(float bank)
    {
        if (incrementPerTurn <= 0) return bank;
        bank += incrementPerTurn;
        if (maxBankSeconds > 0) bank = Mathf.Min(bank, maxBankSeconds);
        return bank;
    }

    private void Update()
    {
        if (!_running) return;
        if (_paused && pauseWhenBlocked) return;

        float dt = Time.deltaTime;
        if (_playerTurn)
        {
            _playerBank -= dt;
            if (_playerBank <= 0f) { _playerBank = 0f; FlagFall(true); return; }
        }
        else
        {
            _enemyBank -= dt;
            if (_enemyBank <= 0f) { _enemyBank = 0f; FlagFall(false); return; }
        }

        _accumDisp += dt;
        if (_accumDisp >= displayUpdateStep)
        {
            _accumDisp = 0f;
            UpdateDisplay(false);
        }
    }

    private void FlagFall(bool player)
    {
        _running = false;
        UpdateDisplay(true);
        if (player) onPlayerFlagFall?.Invoke();
        else onEnemyFlagFall?.Invoke();
    }

    private void UpdateDisplay(bool force)
    {
        // Format mm:ss
        string p = Format(_playerBank);
        string e = Format(_enemyBank);

        if (playerText) playerText.text = p;
        if (enemyText) enemyText.text = e;

        // Optional: color/flash the active side (simple highlight)
        if (playerText) playerText.color = _playerTurn ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        if (enemyText) enemyText.color = !_playerTurn ? Color.white : new Color(1f, 1f, 1f, 0.6f);
    }

    private static string Format(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int s = Mathf.FloorToInt(seconds);
        int m = s / 60;
        int r = s % 60;
        return $"{m:00}:{r:00}";
    }

    // Public getters in case you need them
    public int PlayerSeconds => Mathf.CeilToInt(_playerBank);
    public int EnemySeconds => Mathf.CeilToInt(_enemyBank);
    public bool IsRunning => _running;
    public bool IsPlayerTurn => _playerTurn;
}
