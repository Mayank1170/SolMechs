// Handles progression between tower floors with full HP recovery.
// One Climb: Marathon mode starting from floor 1
// Battle Tower: Campaign mode with permanent progress

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using MechBattle;

public class TowerProgressionHandler : MonoBehaviour
{
    [Header("Tower Manager Reference")]
    public BattleTowerManager towerManager;

    [Header("Battle System References")]
    public BattleManager battleManager;
    public BattleManagerTowerExtension battleExtension;
    public UIManager uiManager;

    [Header("Progression UI")]
    public GameObject progressionPanel;
    public Text progressionTitle;
    public Text progressionMessage;
    public Button continueButton;
    public Button returnToMenuButton;
    public Button retryButton;

    [Header("One Climb UI")]
    public GameObject oneClimbResultPanel;
    public Text oneClimbFloorReached;
    public Text oneClimbBestScore;
    public Button oneClimbReturnButton;

    [Header("Battle Tower UI")]
    public GameObject floorSelectPanel;
    public Transform floorButtonContainer;
    public GameObject floorButtonPrefab;

    // Tower state
    private bool isInTowerBattle = false;
    private bool isOneClimbMode = false;
    private int currentFloor = 1;
    private int oneClimbCurrentRun = 0;
    private int oneClimbBestRun = 0;

    // Battle Tower progress (persistent)
    private HashSet<int> unlockedFloors = new HashSet<int>();
    private Dictionary<int, int> floorAttempts = new Dictionary<int, int>();
    private Dictionary<int, int> floorVictories = new Dictionary<int, int>();

    private void Start()
    {
        SetupUI();
        LoadProgress();
    }

    private void SetupUI()
    {
        if (progressionPanel) progressionPanel.SetActive(false);
        if (oneClimbResultPanel) oneClimbResultPanel.SetActive(false);
        if (floorSelectPanel) floorSelectPanel.SetActive(false);

        if (continueButton)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (returnToMenuButton)
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        if (retryButton)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (oneClimbReturnButton)
            oneClimbReturnButton.onClick.AddListener(OnOneClimbReturnClicked);
    }

    // Called by BattleTowerManager when starting a tower battle
    public void StartTowerBattle(bool oneClimb, int floor)
    {
        isInTowerBattle = true;
        isOneClimbMode = oneClimb;
        currentFloor = floor;

        // Track attempts for Battle Tower mode
        if (!oneClimb)
        {
            if (!floorAttempts.ContainsKey(floor))
                floorAttempts[floor] = 0;
            floorAttempts[floor]++;
        }

        Debug.Log($"[TowerProgressionHandler] Start Tower Battle - Mode: {(oneClimb ? "One Climb" : "Battle Tower")}, Floor: {floor}");
    }

    // === NEW: ensure Tower UI root is visible and above battle HUD ===
    private void EnsureTowerUIVisibleOnTop()
    {
        if (towerManager != null && towerManager.towerUI != null)
        {
            // 1) Make sure the Tower UI root is active
            if (!towerManager.towerUI.activeSelf)
                towerManager.towerUI.SetActive(true);

            // 2) Force canvas sorting above battle HUD
            var cv = towerManager.towerUI.GetComponent<Canvas>();
            if (cv == null) cv = towerManager.towerUI.GetComponentInParent<Canvas>();
            if (cv != null)
            {
                cv.overrideSorting = true;
                cv.sortingOrder = 50; // any value > HUD canvas order
            }

            // 3) Bring to front in the hierarchy
            towerManager.towerUI.transform.SetAsLastSibling();
        }
    }

    // === UPDATED: use coroutine to beat potential race with ShowBattleResult ===
    public void OnBattleEnd(bool playerWon)
    {
        if (!isInTowerBattle) return;
        StartCoroutine(ShowProgressionAfterBattle(playerWon));
    }

    private IEnumerator ShowProgressionAfterBattle(bool playerWon)
    {
        // Let BattleManager/UIManager finish showing its overlay (if any)
        yield return null;

        // Hide result overlay (safety) and bring Tower UI to front
        if (uiManager != null) uiManager.HideBattleResult();
        EnsureTowerUIVisibleOnTop();

        Debug.Log($"[TowerProgressionHandler] Battle Ended - Player Won: {playerWon}, Floor: {currentFloor}");

        if (isOneClimbMode)
            HandleOneClimbResult(playerWon);
        else
            HandleBattleTowerResult(playerWon);

        // Extra safety in the next frame (covers any late overlay call)
        yield return null;
        if (uiManager != null) uiManager.HideBattleResult();

        isInTowerBattle = false;
    }

    private void HandleOneClimbResult(bool playerWon)
    {
        if (playerWon)
        {
            oneClimbCurrentRun = currentFloor;

            if (currentFloor >= 13) // Tower complete!
            {
                ShowOneClimbComplete();
            }
            else
            {
                // Continue to next floor with full HP
                ShowOneClimbContinue();
            }
        }
        else
        {
            // One Climb run ended
            oneClimbCurrentRun = Mathf.Max(0, currentFloor - 1); // Floor reached before losing
            ShowOneClimbDefeat();
        }
    }

    private void ShowOneClimbContinue()
    {
        // === PROTEÇÃO COMPLETA CONTRA NULL ===
        if (progressionPanel == null)
        {
            Debug.LogWarning("[TowerProgressionHandler] ProgressionPanel is null - using auto-continue fallback");
            StartCoroutine(AutoContinueWithoutUI());
            return;
        }

        progressionPanel.SetActive(true);

        // Proteger cada componente UI individualmente
        if (progressionTitle != null)
            progressionTitle.text = $"FLOOR {currentFloor} CLEARED!";

        if (progressionMessage != null)
            progressionMessage.text = $"Well done! Your HP has been fully restored.\nReady for Floor {currentFloor + 1}?";

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            var buttonText = continueButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = $"Continue to Floor {currentFloor + 1}";
        }

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (returnToMenuButton != null)
        {
            returnToMenuButton.gameObject.SetActive(true);
            var buttonText = returnToMenuButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Give Up (End Run)";
        }

        // Auto-continue after delay for better flow (One Climb is uninterrupted)
        StartCoroutine(AutoContinueOneClimb());
    }

    // Fallback quando UI não está configurada
    private IEnumerator AutoContinueWithoutUI()
    {
        Debug.Log($"Floor {currentFloor} cleared! Auto-advancing to Floor {currentFloor + 1}...");
        yield return new WaitForSeconds(2.5f);
        OnContinueClicked();
    }

    private IEnumerator AutoContinueOneClimb()
    {
        yield return new WaitForSeconds(2.5f);
        if (progressionPanel && progressionPanel.activeSelf && isOneClimbMode)
            OnContinueClicked();
    }

    private void ShowOneClimbComplete()
    {
        if (oneClimbResultPanel == null)
        {
            Debug.LogWarning("[TowerProgressionHandler] OneClimbResultPanel is null - logging completion");
            Debug.Log("🏆 PERFECT RUN! All 13 Floors Conquered! 🏆");
            return;
        }

        oneClimbBestRun = 13;
        SaveProgress(); // persist the perfect run

        oneClimbResultPanel.SetActive(true);

        if (oneClimbFloorReached != null)
            oneClimbFloorReached.text = "🏆 PERFECT RUN! 🏆";

        if (oneClimbBestScore != null)
            oneClimbBestScore.text = "All 13 Floors Conquered!\nYou are a ONE CLIMB LEGEND!";
    }

    private void ShowOneClimbDefeat()
    {
        if (oneClimbResultPanel == null)
        {
            Debug.LogWarning("[TowerProgressionHandler] OneClimbResultPanel is null - logging defeat");
            Debug.Log($"One Climb run ended. Floors cleared: {oneClimbCurrentRun}");
            return;
        }

        if (oneClimbCurrentRun > oneClimbBestRun)
        {
            oneClimbBestRun = oneClimbCurrentRun;
            SaveProgress();
        }

        oneClimbResultPanel.SetActive(true);

        if (oneClimbFloorReached != null)
            oneClimbFloorReached.text = $"Run Ended at Floor {currentFloor}";

        if (oneClimbBestScore != null)
        {
            oneClimbBestScore.text = $"Floors Cleared: {oneClimbCurrentRun}\nPersonal Best: {oneClimbBestRun} floors";

            if (oneClimbCurrentRun >= 10) oneClimbBestScore.text += "\nIncredible run!";
            else if (oneClimbCurrentRun >= 7) oneClimbBestScore.text += "\nGreat progress!";
            else if (oneClimbCurrentRun >= 4) oneClimbBestScore.text += "\nGood effort!";
            else oneClimbBestScore.text += "\nKeep trying!";
        }
    }

    private void HandleBattleTowerResult(bool playerWon)
    {
        if (progressionPanel == null)
        {
            Debug.LogWarning("[TowerProgressionHandler] ProgressionPanel is null - using console fallback");
            Debug.Log($"Battle Tower Floor {currentFloor}: {(playerWon ? "VICTORY!" : "DEFEATED")}");
            return;
        }

        progressionPanel.SetActive(true);

        if (playerWon)
        {
            // Track victory
            if (!floorVictories.ContainsKey(currentFloor))
                floorVictories[currentFloor] = 0;
            floorVictories[currentFloor]++;

            // Unlock this floor and the next one
            unlockedFloors.Add(currentFloor);
            if (currentFloor < 13)
                unlockedFloors.Add(currentFloor + 1);

            // Persist using the SAME keys that BattleTowerManager expects
            SaveProgress();

            // Update tower manager (also updates its own SaveProgress)
            if (towerManager != null)
                towerManager.OnFloorUnlocked(currentFloor);

            if (currentFloor >= 13)
            {
                if (progressionTitle != null)
                    progressionTitle.text = "🎊 TOWER COMPLETE! 🎊";

                if (progressionMessage != null)
                    progressionMessage.text = "Congratulations! You've conquered the Battle Tower!\nAll floors are now unlocked for replay.";

                if (continueButton != null)
                    continueButton.gameObject.SetActive(false);
                if (retryButton != null)
                    retryButton.gameObject.SetActive(false);

                if (returnToMenuButton != null)
                {
                    returnToMenuButton.gameObject.SetActive(true);
                    var buttonText = returnToMenuButton.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.text = "Return to Tower Menu";
                }
            }
            else
            {
                if (progressionTitle != null)
                    progressionTitle.text = "VICTORY!";

                if (progressionMessage != null)
                    progressionMessage.text = $"Floor {currentFloor} Conquered!\nFloor {currentFloor + 1} is now unlocked!";

                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                    var buttonText = continueButton.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.text = $"Challenge Floor {currentFloor + 1}";
                }

                if (retryButton != null)
                {
                    retryButton.gameObject.SetActive(true);
                    var buttonText = retryButton.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.text = $"Replay Floor {currentFloor}";
                }

                if (returnToMenuButton != null)
                {
                    returnToMenuButton.gameObject.SetActive(true);
                    var buttonText = returnToMenuButton.GetComponentInChildren<Text>();
                    if (buttonText != null)
                        buttonText.text = "Tower Menu";
                }
            }
        }
        else
        {
            if (progressionTitle != null)
                progressionTitle.text = "DEFEATED";

            int attempts = floorAttempts.ContainsKey(currentFloor) ? floorAttempts[currentFloor] : 1;

            if (progressionMessage != null)
                progressionMessage.text = $"Floor {currentFloor} proved too challenging.\nAttempts: {attempts}\nAdjust your strategy and try again!";

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
                var buttonText = retryButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = $"Retry Floor {currentFloor}";
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.gameObject.SetActive(true);
                var buttonText = returnToMenuButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = "Tower Menu (Change Mech)";
            }
        }
    }

    public void ShowFloorSelection()
    {
        if (floorSelectPanel == null) return;
        floorSelectPanel.SetActive(true);
        RefreshFloorButtons();
    }

    private void RefreshFloorButtons()
    {
        // Clear existing buttons
        foreach (Transform child in floorButtonContainer)
            Destroy(child.gameObject);

        // Create button for each floor
        for (int floor = 1; floor <= 13; floor++)
        {
            GameObject btnObj = floorButtonPrefab != null
                ? Instantiate(floorButtonPrefab, floorButtonContainer)
                : CreateFloorButton();

            Button btn = btnObj.GetComponent<Button>();
            Text btnText = btnObj.GetComponentInChildren<Text>();

            bool isUnlocked = unlockedFloors.Contains(floor) || floor == 1;
            int victories = floorVictories.ContainsKey(floor) ? floorVictories[floor] : 0;

            if (isUnlocked)
            {
                if (btnText != null)
                {
                    btnText.text = $"Floor {floor}";
                    if (victories > 0) btnText.text += $" ✓ ({victories})";
                }

                btn.interactable = true;
                int floorToPlay = floor;
                btn.onClick.AddListener(() => SelectFloor(floorToPlay));
            }
            else
            {
                if (btnText != null)
                    btnText.text = $"Floor {floor} 🔒";
                btn.interactable = false;
            }
        }
    }

    private GameObject CreateFloorButton()
    {
        GameObject btnObj = new GameObject($"FloorButton");
        btnObj.transform.SetParent(floorButtonContainer);

        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "Floor";
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 40);

        return btnObj;
    }

    private void SelectFloor(int floor)
    {
        if (floorSelectPanel) floorSelectPanel.SetActive(false);
        currentFloor = floor;

        if (towerManager != null)
            towerManager.StartSpecificFloor(floor);
    }

    // === MÉTODO ATUALIZADO: Usar corrotina para reinicializar batalha ===
    private void OnContinueClicked()
    {
        if (progressionPanel) progressionPanel.SetActive(false);
        currentFloor++;

        StartCoroutine(RestartBattleForNextFloor());
    }

    // === NOVO: Reinicialização completa da batalha ===
    private IEnumerator RestartBattleForNextFloor()
    {
        // 1. Limpar UIs
        if (uiManager != null)
        {
            uiManager.HideBattleResult();
            uiManager.DisableAllButtons();
        }

        yield return null;

        // 2. Reset do BattleManager
        if (battleManager != null)
        {
            // Usar reflection para acessar battleOver se necessário
            var battleOverField = typeof(BattleManager).GetField("battleOver",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (battleOverField != null)
                battleOverField.SetValue(battleManager, false);

            RestoreFullHP();
            battleManager.Initialize(battleManager.playerUnit, battleManager.enemyUnit, uiManager);

            if (uiManager != null)
            {
                uiManager.InitializeHealthBars(battleManager.playerUnit, battleManager.enemyUnit,
                    battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
            }
        }

        yield return null;

        // 3. Configurar novo inimigo
        if (towerManager != null)
        {
            if (isOneClimbMode)
                towerManager.ContinueOneClimbWithFullHP(currentFloor);
            else
                towerManager.StartSpecificFloor(currentFloor);
        }

        yield return null;

        // 4. Iniciar nova batalha
        if (battleManager != null)
            battleManager.StartTurn();

        isInTowerBattle = true;
    }

    // === NOVO: Restaurar HP completo ===
    private void RestoreFullHP()
    {
        if (battleManager == null) return;

        // Player HP
        if (battleManager.playerUnit != null)
        {
            if (battleManager.playerMaxHPs.ContainsKey(ModuleSlot.Matrix))
                battleManager.playerUnit.matrixHP = battleManager.playerMaxHPs[ModuleSlot.Matrix];

            foreach (var slot in battleManager.playerUnit.partStatuses.Keys)
            {
                if (battleManager.playerMaxHPs.ContainsKey(slot))
                    battleManager.playerUnit.partStatuses[slot].currentHP = battleManager.playerMaxHPs[slot];
            }
        }

        // Enemy HP
        if (battleManager.enemyUnit != null)
        {
            if (battleManager.enemyMaxHPs.ContainsKey(ModuleSlot.Matrix))
                battleManager.enemyUnit.matrixHP = battleManager.enemyMaxHPs[ModuleSlot.Matrix];

            foreach (var slot in battleManager.enemyUnit.partStatuses.Keys)
            {
                if (battleManager.enemyMaxHPs.ContainsKey(slot))
                    battleManager.enemyUnit.partStatuses[slot].currentHP = battleManager.enemyMaxHPs[slot];
            }
        }
    }

    private void OnReturnToMenuClicked()
    {
        if (progressionPanel) progressionPanel.SetActive(false);
        isInTowerBattle = false;

        if (isOneClimbMode)
        {
            // End the One Climb run
            ShowOneClimbDefeat();
        }
        else
        {
            // Return to Battle Tower menu
            if (towerManager != null)
                towerManager.ReturnToTowerMenu();
        }
    }

    private void OnOneClimbReturnClicked()
    {
        if (oneClimbResultPanel) oneClimbResultPanel.SetActive(false);
        oneClimbCurrentRun = 0;

        if (towerManager != null)
            towerManager.ReturnToModeSelection();
    }

    // === MÉTODO ATUALIZADO: Usar corrotina para retry ===
    private void OnRetryClicked()
    {
        if (progressionPanel) progressionPanel.SetActive(false);
        StartCoroutine(RestartBattleForRetry());
    }

    // === NOVO: Reinicialização para retry ===
    private IEnumerator RestartBattleForRetry()
    {
        // Limpar UI
        if (uiManager != null)
        {
            uiManager.HideBattleResult();
            uiManager.DisableAllButtons();
        }

        yield return null;

        // Reset battle manager
        if (battleManager != null)
        {
            var battleOverField = typeof(BattleManager).GetField("battleOver",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (battleOverField != null)
                battleOverField.SetValue(battleManager, false);

            RestoreFullHP();
            battleManager.Initialize(battleManager.playerUnit, battleManager.enemyUnit, uiManager);

            if (uiManager != null)
            {
                uiManager.InitializeHealthBars(battleManager.playerUnit, battleManager.enemyUnit,
                    battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
            }
        }

        yield return null;

        // Start same floor
        if (towerManager != null)
            towerManager.StartSpecificFloor(currentFloor);

        yield return null;

        if (battleManager != null)
            battleManager.StartTurn();

        isInTowerBattle = true;
    }

    // -------------------- Persistence (keys aligned with BattleTowerManager) --------------------

    private void SaveProgress()
    {
        // Save One Climb best
        PlayerPrefs.SetInt("OneClimb_BestRun", oneClimbBestRun);

        // Save Battle Tower progress using the SAME keys as BattleTowerManager
        foreach (int floor in unlockedFloors)
            PlayerPrefs.SetInt($"BattleTower_Floor_{floor}_Unlocked", 1);

        PlayerPrefs.SetInt("BattleTower_CompletedFloors", unlockedFloors.Count);

        int highest = 1;
        foreach (int f in unlockedFloors) if (f > highest) highest = f;
        PlayerPrefs.SetInt("BattleTower_CurrentFloor", Mathf.Clamp(highest, 1, 13));

        // (Optional analytics)
        foreach (var kvp in floorVictories)
            PlayerPrefs.SetInt($"BattleTower_Floor{kvp.Key}_Victories", kvp.Value);
        foreach (var kvp in floorAttempts)
            PlayerPrefs.SetInt($"BattleTower_Floor{kvp.Key}_Attempts", kvp.Value);

        PlayerPrefs.Save();
        Debug.Log($"[TowerProgressionHandler] Progress saved. Unlocked: {unlockedFloors.Count}, Highest: {highest}");
    }

    private void LoadProgress()
    {
        // Load One Climb best
        oneClimbBestRun = PlayerPrefs.GetInt("OneClimb_BestRun", 0);

        // Load Battle Tower flags
        unlockedFloors.Clear();
        unlockedFloors.Add(1); // Floor 1 always unlocked

        for (int floor = 1; floor <= 13; floor++)
        {
            if (PlayerPrefs.GetInt($"BattleTower_Floor_{floor}_Unlocked", 0) == 1)
            {
                unlockedFloors.Add(floor);
            }

            int v = PlayerPrefs.GetInt($"BattleTower_Floor{floor}_Victories", 0);
            if (v > 0) floorVictories[floor] = v;

            int a = PlayerPrefs.GetInt($"BattleTower_Floor{floor}_Attempts", 0);
            if (a > 0) floorAttempts[floor] = a;
        }

        int ptr = PlayerPrefs.GetInt("BattleTower_CurrentFloor", 1);
        currentFloor = Mathf.Clamp(ptr, 1, 13);

        Debug.Log($"[TowerProgressionHandler] Progress loaded. Unlocked: {unlockedFloors.Count}, Pointer: {currentFloor}");
    }

    /// <summary>
    /// Optional polling-based status check. Prefer BattleEndDetector for event-like behavior.
    /// </summary>
    public void CheckBattleStatus()
    {
        if (!isInTowerBattle) return;
        if (battleManager == null) return;

        bool enemyDefeated = false;
        bool playerDefeated = false;

        // Prefer extension component if present
        if (battleExtension != null)
        {
            try
            {
                enemyDefeated = battleExtension.IsEnemyDefeated();
                playerDefeated = battleExtension.IsPlayerDefeated();
            }
            catch { /* fall back */ }
        }

        // Fallback via HP arrays shim
        if (!enemyDefeated && !playerDefeated)
        {
            var e = TowerBMHelper.GetEnemyCurrentHPs(battleManager);
            var p = TowerBMHelper.GetPlayerCurrentHPs(battleManager);

            enemyDefeated = TowerBMHelper.IsTeamDefeated(e);
            playerDefeated = TowerBMHelper.IsTeamDefeated(p);
        }

        if (enemyDefeated && !playerDefeated)
            OnBattleEnd(true);
        else if (playerDefeated)
            OnBattleEnd(false);
    }
}