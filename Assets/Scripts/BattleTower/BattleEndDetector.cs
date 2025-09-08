// Monitors battle state and notifies the progression handler when battle ends.
// Add this to the GameController GameObject alongside BattleManager.
using UnityEngine;

public class BattleEndDetector : MonoBehaviour
{
    [Header("System References")]
    public BattleManager battleManager;
    public GameObject progressionHandlerObject; // Reference via GameObject to avoid hard dep

    private bool battleEnded = false;
    private bool lastCheckPlayerAlive = true;
    private bool lastCheckEnemyAlive = true;

    void Start()
    {
        // Auto-find components if not assigned
        if (battleManager == null)
            battleManager = GetComponent<BattleManager>();

        if (progressionHandlerObject == null)
        {
            var handler = FindObjectOfType<TowerProgressionHandler>();
            if (handler != null)
                progressionHandlerObject = handler.gameObject;
        }
    }

    void Update()
    {
        if (battleManager == null || progressionHandlerObject == null)
            return;

        if (battleEnded) return; // Only check during active battles

        CheckBattleState();
    }

    private void CheckBattleState()
    {
        // Use HP arrays via the shim
        bool playerAlive = IsPlayerAlive();
        bool enemyAlive = IsEnemyAlive();

        // Detect state change
        if (lastCheckPlayerAlive && !playerAlive)
        {
            // Player just died
            OnBattleEnd(false);
        }
        else if (lastCheckEnemyAlive && !enemyAlive && playerAlive)
        {
            // Enemy just died (and player is still alive)
            OnBattleEnd(true);
        }

        lastCheckPlayerAlive = playerAlive;
        lastCheckEnemyAlive = enemyAlive;
    }

    private bool IsPlayerAlive()
    {
        if (!battleManager) return false;
        var p = TowerBMHelper.GetPlayerCurrentHPs(battleManager);
        if (p == null || p.Length == 0) return false;
        for (int i = 0; i < p.Length; i++)
            if (p[i] > 0) return true;
        return false;
    }

    private bool IsEnemyAlive()
    {
        if (!battleManager) return false;
        var e = TowerBMHelper.GetEnemyCurrentHPs(battleManager);
        if (e == null || e.Length == 0) return false;
        for (int i = 0; i < e.Length; i++)
            if (e[i] > 0) return true;
        return false;
    }

    private void OnBattleEnd(bool playerWon)
    {
        if (battleEnded) return; // Prevent duplicate calls
        battleEnded = true;

        // Optional safety: hide battle result (in case it’s still visible)
        var ui = FindObjectOfType<UIManager>();
        if (ui) ui.HideBattleResult();

        Debug.Log($"[BattleEndDetector] Battle ended! Player won: {playerWon}");

        // Notify progression handler using SendMessage (non-breaking dependency)
        if (progressionHandlerObject != null)
            progressionHandlerObject.SendMessage("OnBattleEnd", playerWon, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Reset when a new battle starts.
    /// </summary>
    public void ResetDetector()
    {
        battleEnded = false;
        lastCheckPlayerAlive = true;
        lastCheckEnemyAlive = true;
    }
}
