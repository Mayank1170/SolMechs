// Main controller for the Battle Tower PvE mode
// Handles floor progression, mode selection, and battle coordination

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using MechBattle;
using System.Linq;

public class BattleTowerManager : MonoBehaviour
{
    [Header("Tower Configuration")]
    [Tooltip("Array of 13 floor configurations (floors 1-13)")]
    public TowerFloorData[] floorConfigs = new TowerFloorData[13];

    [Header("UI Panels")]
    public GameObject towerUI;
    public GameObject modeSelectionPanel;
    public GameObject towerMenuPanel;

    [Header("Mode Selection Buttons")]
    public Button oneClimbButton;
    public Button battleTowerButton;
    public Button backToModeButton;

    [Header("Tower Menu UI")]
    public Button startFloorButton;
    public Button selectFloorButton; // For Battle Tower mode
    public Text floorDisplayText;
    public Text progressText;

    [Header("Game System References")]
    public GameController originalGameController;
    public GameObject playerMechObject;
    public GameObject enemyMechObject;

    [Header("Fallback Parts (Drag from Project)")]
    [Tooltip("Assign default parts here in case Resources loading fails")]
    public Matrix fallbackMatrix;
    public MechPart fallbackRightArm;
    public MechPart fallbackLeftArm;
    public MechPart fallbackLowerBody;

    [Header("Tower Progression")]
    public TowerProgressionHandler progressionHandler;

    // Tower progression state
    private int currentFloor = 1;
    private bool isOneClimbMode = false;
    private bool towerActive = false;

    // Progress tracking
    private Dictionary<int, bool> floorsCompleted = new Dictionary<int, bool>();

    // Cached system references
    private BattleManager battleManager;
    private UIManager uiManager;

    // Resource cache
    private Dictionary<string, Matrix> matrixCache = new Dictionary<string, Matrix>();
    private Dictionary<string, MechPart> partCache = new Dictionary<string, MechPart>();

    private void Start()
    {
        battleManager = originalGameController.GetComponent<BattleManager>();
        uiManager = originalGameController.GetComponent<UIManager>();

        SetupTowerUI();
        LoadProgress();
        LoadAllResources();
        SetupDefaultFloors();
        ShowTowerModeSelection();
    }

    private void LoadAllResources()
    {
        Debug.Log("=== Loading Tower Resources ===");

        // Load all matrices
        Matrix[] allMatrices = Resources.LoadAll<Matrix>("Matrices");
        Debug.Log($"Found {allMatrices.Length} matrices in Resources/Matrices");
        foreach (var matrix in allMatrices)
        {
            if (matrix != null)
            {
                matrixCache[matrix.name] = matrix;
                Debug.Log($"  - Cached matrix: {matrix.name}");
            }
        }

        // Load all right arms
        MechPart[] rightArms = Resources.LoadAll<MechPart>("Parts/RightArm");
        Debug.Log($"Found {rightArms.Length} right arms in Resources/Parts/RightArm");
        foreach (var part in rightArms)
        {
            if (part != null)
            {
                partCache["RA_" + part.name] = part;
                Debug.Log($"  - Cached right arm: {part.name}");
            }
        }

        // Load all left arms  
        MechPart[] leftArms = Resources.LoadAll<MechPart>("Parts/LeftArm");
        Debug.Log($"Found {leftArms.Length} left arms in Resources/Parts/LeftArm");
        foreach (var part in leftArms)
        {
            if (part != null)
            {
                partCache["LA_" + part.name] = part;
                Debug.Log($"  - Cached left arm: {part.name}");
            }
        }

        // Load all lower bodies
        MechPart[] lowerBodies = Resources.LoadAll<MechPart>("Parts/LowerBody");
        Debug.Log($"Found {lowerBodies.Length} lower bodies in Resources/Parts/LowerBody");
        foreach (var part in lowerBodies)
        {
            if (part != null)
            {
                partCache["LO_" + part.name] = part;
                Debug.Log($"  - Cached lower body: {part.name}");
            }
        }

        // Try alternative paths if nothing found
        if (allMatrices.Length == 0)
        {
            Debug.LogWarning("No matrices found in Resources/Matrices, trying alternate paths...");

            // Try without subfolder
            allMatrices = Resources.LoadAll<Matrix>("");
            Debug.Log($"Found {allMatrices.Length} matrices in root Resources");
            foreach (var matrix in allMatrices)
            {
                if (matrix != null)
                {
                    matrixCache[matrix.name] = matrix;
                    Debug.Log($"  - Found matrix in root: {matrix.name}");
                }
            }
        }

        if (rightArms.Length == 0 && leftArms.Length == 0 && lowerBodies.Length == 0)
        {
            Debug.LogWarning("No parts found in Resources/Parts subfolders, trying alternate paths...");

            // Try loading all MechParts from root
            MechPart[] allParts = Resources.LoadAll<MechPart>("");
            Debug.Log($"Found {allParts.Length} mech parts in root Resources");
            foreach (var part in allParts)
            {
                if (part != null)
                {
                    partCache[part.name] = part;
                    Debug.Log($"  - Found part in root: {part.name}");
                }
            }
        }

        Debug.Log("=== Resource Loading Complete ===");
    }

    private void SetupTowerUI()
    {
        oneClimbButton.onClick.AddListener(() => StartTowerMode(true));
        battleTowerButton.onClick.AddListener(() => StartTowerMode(false));
        startFloorButton.onClick.AddListener(StartCurrentFloor);
        backToModeButton.onClick.AddListener(BackToModeSelection);

        if (selectFloorButton != null)
        {
            selectFloorButton.onClick.AddListener(ShowFloorSelection);
            selectFloorButton.gameObject.SetActive(false); // Hidden by default
        }

        if (towerUI) towerUI.SetActive(false);
    }

    private void SetupDefaultFloors()
    {
        string[] mechNames = { "Titan", "Striker", "Arclight", "HeartCore" };

        for (int i = 0; i < floorConfigs.Length; i++)
        {
            if (floorConfigs[i] != null) continue;

            floorConfigs[i] = new TowerFloorData();

            if (i < 4)
            {
                int mechNumber = i + 1;
                string mechName = mechNames[i];

                floorConfigs[i].enemyName = $"{mechName} Mech";
                floorConfigs[i].chassisCode = $"MT{mechNumber:00}";
                floorConfigs[i].rightArmCode = $"RA{mechNumber:00}";
                floorConfigs[i].leftArmCode = $"LA{mechNumber:00}";
                floorConfigs[i].lowerBodyCode = $"LO{mechNumber:00}";
            }
            else if (i < 12)
            {
                floorConfigs[i].enemyName = $"Hybrid Mech {i - 3}";

                int chassis = ((i - 4) % 4) + 1;
                int rightArm = (((i - 4) + 1) % 4) + 1;
                int leftArm = (((i - 4) + 2) % 4) + 1;
                int lowerBody = (((i - 4) + 3) % 4) + 1;

                floorConfigs[i].chassisCode = $"MT{chassis:00}";
                floorConfigs[i].rightArmCode = $"RA{rightArm:00}";
                floorConfigs[i].leftArmCode = $"LA{leftArm:00}";
                floorConfigs[i].lowerBodyCode = $"LO{lowerBody:00}";
            }
            else
            {
                floorConfigs[i].enemyName = "Tower Guardian";
                floorConfigs[i].chassisCode = "MT01";
                floorConfigs[i].rightArmCode = "RA02";
                floorConfigs[i].leftArmCode = "LA03";
                floorConfigs[i].lowerBodyCode = "LO04";
                floorConfigs[i].isBossFloor = true;
                floorConfigs[i].statBoostMultiplier = 1.5f;
                floorConfigs[i].specialAbility = "Regeneration";
            }
        }

        Debug.Log("Tower floors configured:");
        for (int i = 0; i < floorConfigs.Length; i++)
        {
            Debug.Log($"  Floor {i + 1}: {floorConfigs[i].enemyName} - {floorConfigs[i].GetPartConfiguration()}");
        }
    }

    public void ShowTowerModeSelection()
    {
        towerUI.SetActive(true);
        modeSelectionPanel.SetActive(true);
        towerMenuPanel.SetActive(false);
        towerActive = false;
    }

    public void StartTowerMode(bool oneClimb)
    {
        isOneClimbMode = oneClimb;
        towerActive = true;

        if (oneClimb)
        {
            // One Climb always starts from floor 1
            currentFloor = 1;
            floorsCompleted.Clear();
            Debug.Log("Starting One Climb mode - Beginning at Floor 1");
        }
        else
        {
            // Battle Tower loads progress and sets to appropriate floor
            LoadProgress();
            int highestUnlocked = GetHighestUnlockedFloor();

            // Define currentFloor baseado no progresso
            if (highestUnlocked == 1 && !floorsCompleted.ContainsKey(1))
            {
                currentFloor = 1; // Primeiro jogo
            }
            else if (highestUnlocked < 13)
            {
                currentFloor = highestUnlocked + 1; // Próximo andar a desbloquear
            }
            else
            {
                currentFloor = 13; // Torre completa, deixa no último andar
            }

            Debug.Log($"Starting Battle Tower mode - Current floor set to: {currentFloor}, Highest unlocked: {highestUnlocked}");
        }

        ShowTowerMenu();
    }

    private void ShowTowerMenu()
    {
        modeSelectionPanel.SetActive(false);
        towerMenuPanel.SetActive(true);

        if (isOneClimbMode)
        {
            // One Climb - show run start info
            currentFloor = 1; // Always start at floor 1
        }
        else
        {
            // Battle Tower - immediately open floor selection so the player chooses next fight
            if (progressionHandler != null)
                progressionHandler.ShowFloorSelection();
        }

        UpdateTowerUI();
    }

    private void ShowFloorSelection()
    {
        if (progressionHandler != null)
        {
            progressionHandler.ShowFloorSelection();
        }
    }

    private void UpdateTowerUI()
    {
        // Show/hide floor selection button based on mode
        if (selectFloorButton != null)
        {
            selectFloorButton.gameObject.SetActive(!isOneClimbMode);
        }

        if (floorDisplayText)
        {
            if (isOneClimbMode)
            {
                floorDisplayText.text = $"One Climb - Floor {currentFloor}/13";
            }
            else
            {
                int unlocked = GetHighestUnlockedFloor();
                floorDisplayText.text = $"Battle Tower - Highest: {unlocked}/13";
            }
        }

        if (progressText)
        {
            string modeText = isOneClimbMode ? "One Climb Run" : "Battle Tower";

            if (isOneClimbMode)
            {
                string enemyName = GetCurrentFloorName();
                progressText.text = $"{modeText}\nNext Enemy: {enemyName}\nHP will be fully restored after each victory";
            }
            else
            {
                int unlocked = GetHighestUnlockedFloor();
                int total = 13;

                if (unlocked >= 13)
                {
                    progressText.text = $"{modeText}\nTower Complete! All floors unlocked.\nChoose any floor to replay.";
                }
                else
                {
                    progressText.text = $"{modeText}\nFloors Unlocked: {unlocked}/{total}\nNext Challenge: Floor {unlocked + 1}";
                }
            }
        }

        if (startFloorButton)
        {
            if (currentFloor > floorConfigs.Length)
            {
                startFloorButton.GetComponentInChildren<Text>().text = "Tower Complete!";
                startFloorButton.interactable = false;
            }
            else
            {
                string enemyName = GetCurrentFloorName();
                if (isOneClimbMode)
                {
                    startFloorButton.GetComponentInChildren<Text>().text = $"Start Floor {currentFloor}: {enemyName}";
                    startFloorButton.interactable = true;
                }
                else
                {
                    // Verificação mais segura de desbloqueio
                    bool isUnlocked = currentFloor == 1 ||
                        (floorsCompleted.TryGetValue(currentFloor - 1, out bool prevCompleted) && prevCompleted);

                    if (isUnlocked)
                    {
                        startFloorButton.GetComponentInChildren<Text>().text = $"Fight Floor {currentFloor}: {enemyName}";
                        startFloorButton.interactable = true;
                    }
                    else
                    {
                        startFloorButton.GetComponentInChildren<Text>().text = $"Floor {currentFloor} - LOCKED";
                        startFloorButton.interactable = false;
                    }
                }
            }
        }
    }

    public void StartCurrentFloor()
    {
        if (currentFloor > floorConfigs.Length)
        {
            CompleteTower();
            return;
        }

        StartCoroutine(InitializeTowerBattle());
    }

    public void StartSpecificFloor(int floor)
    {
        currentFloor = floor;
        StartCurrentFloor();
    }

    public void ContinueOneClimbWithFullHP(int floor)
    {
        currentFloor = floor;
        // Simply start the next floor - HP will be full automatically
        StartCurrentFloor();
    }

    public void SetCurrentFloor(int floor)
    {
        currentFloor = floor;
        UpdateTowerUI();
    }

    public void ReturnToTowerMenu()
    {
        towerUI.SetActive(true);
        ShowTowerMenu();
    }

    public void ReturnToModeSelection()
    {
        towerUI.SetActive(true);
        ShowTowerModeSelection();
    }

    public void BackToModeSelection()
    {
        towerActive = false;
        ShowTowerModeSelection();
    }

    // Public methods for TowerProgressionHandler integration
    public void OnFloorUnlocked(int floor)
    {
        floorsCompleted[floor] = true;
        if (floor < 13)
        {
            // In Battle Tower mode, unlock next floor
            floorsCompleted[floor + 1] = true;
        }
        SaveProgress();
        UpdateTowerUI();
    }

    private int GetHighestUnlockedFloor()
    {
        int highest = 1;
        for (int i = 1; i <= 13; i++)
        {
            if (floorsCompleted.ContainsKey(i) && floorsCompleted[i])
                highest = i;
        }
        return highest;
    }

    private Matrix LoadMatrix(string code)
    {
        // Try exact match first
        if (matrixCache.ContainsKey(code))
            return matrixCache[code];

        // Try without leading zeros (MT01 -> MT1)
        string simplified = code.Replace("MT0", "MT");
        if (matrixCache.ContainsKey(simplified))
            return matrixCache[simplified];

        // Try just the number (MT01 -> 01 or 1)
        string numberOnly = code.Replace("MT", "").TrimStart('0');
        if (matrixCache.ContainsKey(numberOnly))
            return matrixCache[numberOnly];

        // Try common variations
        string[] variations = {
            $"Matrix_{code}",
            $"Matrix{numberOnly}",
            $"Titan", // For MT01
            $"Striker", // For MT02
            $"Arclight", // For MT03
            $"HeartCore" // For MT04
        };

        foreach (var variant in variations)
        {
            if (matrixCache.ContainsKey(variant))
                return matrixCache[variant];
        }

        // Try any matrix that contains the number
        var matchingKey = matrixCache.Keys.FirstOrDefault(k =>
            k.Contains(numberOnly) || k.Contains(code));
        if (!string.IsNullOrEmpty(matchingKey))
            return matrixCache[matchingKey];

        Debug.LogWarning($"Could not find matrix for code: {code}");
        return null;
    }

    private MechPart LoadPart(string code, string partType)
    {
        // Try with prefix first
        string prefixedKey = partType + "_" + code;
        if (partCache.ContainsKey(prefixedKey))
            return partCache[prefixedKey];

        // Try exact match
        if (partCache.ContainsKey(code))
            return partCache[code];

        // Try without leading zeros (RA01 -> RA1)
        string simplified = code.Replace($"{partType}0", partType);
        if (partCache.ContainsKey(simplified))
            return partCache[simplified];

        // Try just the number
        string numberOnly = code.Replace(partType, "").TrimStart('0');
        if (partCache.ContainsKey(numberOnly))
            return partCache[numberOnly];

        // Try common variations
        string[] variations = {
            $"{partType}_{code}",
            $"{partType}{numberOnly}",
            $"Part_{code}",
            $"Part{numberOnly}"
        };

        foreach (var variant in variations)
        {
            if (partCache.ContainsKey(variant))
                return partCache[variant];
        }

        // Try any part that contains the code
        var matchingKey = partCache.Keys.FirstOrDefault(k =>
            k.Contains(code) || k.Contains(numberOnly));
        if (!string.IsNullOrEmpty(matchingKey))
            return partCache[matchingKey];

        Debug.LogWarning($"Could not find part for code: {code} (type: {partType})");
        return null;
    }

    private IEnumerator InitializeTowerBattle()
    {
        // Hide Tower UI while fighting
        if (towerUI) towerUI.SetActive(false);

        var floorData = floorConfigs[currentFloor - 1];
        var enemyLoader = enemyMechObject.GetComponent<MechUnitLoader>();

        if (enemyLoader != null)
        {
            LoadEnemyMech(enemyLoader, floorData);
        }

        yield return new WaitForEndOfFrame();

        ResetBattleState();

        var playerUnit = playerMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        var enemyUnit = enemyMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();

        if (playerUnit != null && enemyUnit != null)
        {
            InitializeBattle(playerUnit, enemyUnit, floorData.enemyName);

            // Notify progression handler that tower battle started
            if (progressionHandler != null)
            {
                progressionHandler.StartTowerBattle(isOneClimbMode, currentFloor);
            }

            // Reset end detector so it watches this new battle
            var detector = FindObjectOfType<BattleEndDetector>();
            if (detector) detector.ResetDetector();

            // Log mode-specific info
            if (isOneClimbMode)
            {
                uiManager.LogMessage($"=== ONE CLIMB - FLOOR {currentFloor} ===\n");
                if (currentFloor > 1)
                    uiManager.LogMessage("Your HP has been fully restored!\n");
            }
            else
            {
                uiManager.LogMessage($"=== BATTLE TOWER - FLOOR {currentFloor} ===\n");
            }
        }
        else
        {
            Debug.LogError($"Failed to load mech units for tower battle! Player: {playerUnit != null}, Enemy: {enemyUnit != null}");

            // Try fallback parts if available
            if (fallbackMatrix != null && enemyUnit == null)
            {
                Debug.Log("Attempting to create enemy with fallback parts...");
                var enemyLoader2 = enemyMechObject.GetComponent<MechUnitLoader>();
                enemyLoader2.useSavedBuild = false;
                enemyLoader2.matrix = fallbackMatrix;
                enemyLoader2.rightArm = fallbackRightArm;
                enemyLoader2.leftArm = fallbackLeftArm;
                enemyLoader2.lower = fallbackLowerBody;

                yield return new WaitForEndOfFrame();
                enemyUnit = enemyLoader2.GetUnitData();

                if (playerUnit != null && enemyUnit != null)
                {
                    InitializeBattle(playerUnit, enemyUnit, "Fallback Enemy");

                    if (progressionHandler != null)
                    {
                        progressionHandler.StartTowerBattle(isOneClimbMode, currentFloor);
                    }

                    var detector2 = FindObjectOfType<BattleEndDetector>();
                    if (detector2) detector2.ResetDetector();

                    yield break;
                }
            }

            if (towerUI) towerUI.SetActive(true);
            ShowTowerMenu();
        }
    }

    private void LoadEnemyMech(MechUnitLoader enemyLoader, TowerFloorData floorData)
    {
        Debug.Log($"Loading enemy mech for floor {currentFloor}: {floorData.enemyName}");
        Debug.Log($"Configuration: {floorData.GetPartConfiguration()}");

        enemyLoader.useSavedBuild = false;

        // Load matrix
        Matrix loadedMatrix = LoadMatrix(floorData.chassisCode);
        if (loadedMatrix == null && fallbackMatrix != null)
        {
            Debug.LogWarning($"Using fallback matrix for {floorData.chassisCode}");
            loadedMatrix = fallbackMatrix;
        }
        enemyLoader.matrix = loadedMatrix;

        // Load right arm
        MechPart loadedRightArm = LoadPart(floorData.rightArmCode, "RA");
        if (loadedRightArm == null && fallbackRightArm != null)
        {
            Debug.LogWarning($"Using fallback right arm for {floorData.rightArmCode}");
            loadedRightArm = fallbackRightArm;
        }
        enemyLoader.rightArm = loadedRightArm;

        // Load left arm
        MechPart loadedLeftArm = LoadPart(floorData.leftArmCode, "LA");
        if (loadedLeftArm == null && fallbackLeftArm != null)
        {
            Debug.LogWarning($"Using fallback left arm for {floorData.leftArmCode}");
            loadedLeftArm = fallbackLeftArm;
        }
        enemyLoader.leftArm = loadedLeftArm;

        // Load lower body
        MechPart loadedLowerBody = LoadPart(floorData.lowerBodyCode, "LO");
        if (loadedLowerBody == null && fallbackLowerBody != null)
        {
            Debug.LogWarning($"Using fallback lower body for {floorData.lowerBodyCode}");
            loadedLowerBody = fallbackLowerBody;
        }
        enemyLoader.lower = loadedLowerBody;
    }

    private void InitializeBattle(MechUnit player, MechUnit enemy, string floorName)
    {
        battleManager.Initialize(player, enemy, uiManager);
        uiManager.InitializeHealthBars(player, enemy, battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
        uiManager.InitializePaperDolls(
            playerMechObject.GetComponent<MechUnitLoader>(),
            enemyMechObject.GetComponent<MechUnitLoader>()
        );
        uiManager.DisableSliderInteractabilityAndHandles();

        uiManager.LogMessage($"Tower Battle: {floorName}\n{player.Name} vs {enemy.Name}!\n\n");
        battleManager.StartTurn();
    }

    private void ResetBattleState()
    {
        if (uiManager.combatlogText != null)
            uiManager.combatlogText.text = "";

        uiManager.HideBattleResult();
    }

    private void CompleteTower()
    {
        string completionBonus = isOneClimbMode
            ? "Tower Climber Title - Complete tower in single run!"
            : "Tower Master Achievement - All floors conquered!";

        uiManager.LogMessage($"Congratulations! Tower Complete!\n{completionBonus}");

        currentFloor = 1;
        if (isOneClimbMode)
        {
            floorsCompleted.Clear();
        }

        StartCoroutine(DelayedReturnToMenu());
    }

    // Wait and then return to menu
    private IEnumerator DelayedReturnToMenu()
    {
        yield return new WaitForSeconds(3f);
        if (towerUI) towerUI.SetActive(true);
        ShowTowerMenu();
    }

    private string GetCurrentFloorName()
    {
        if (currentFloor > floorConfigs.Length) return "Complete!";
        return floorConfigs[currentFloor - 1]?.enemyName ?? $"Floor {currentFloor}";
    }

    private void SaveProgress()
    {
        if (!isOneClimbMode)
        {
            // Save Battle Tower progress (persistent unlocks)
            PlayerPrefs.SetInt("BattleTower_CurrentFloor", currentFloor);
            PlayerPrefs.SetInt("BattleTower_CompletedFloors", floorsCompleted.Count);

            foreach (var kvp in floorsCompleted)
            {
                PlayerPrefs.SetInt($"BattleTower_Floor_{kvp.Key}_Unlocked", kvp.Value ? 1 : 0);
            }

            PlayerPrefs.Save();
            Debug.Log($"Battle Tower progress saved. Unlocked floors: {floorsCompleted.Count}");
        }
        // One Climb progress is handled by TowerProgressionHandler
    }

    private void LoadProgress()
    {
        if (!isOneClimbMode)
        {
            // Load Battle Tower progress
            currentFloor = PlayerPrefs.GetInt("BattleTower_CurrentFloor", 1);
            int completedCount = PlayerPrefs.GetInt("BattleTower_CompletedFloors", 0);

            floorsCompleted.Clear();
            floorsCompleted[1] = true; // Floor 1 always unlocked

            for (int i = 1; i <= 13; i++)
            {
                if (PlayerPrefs.GetInt($"BattleTower_Floor_{i}_Unlocked", 0) == 1)
                    floorsCompleted[i] = true;
            }

            Debug.Log($"Battle Tower progress loaded. Unlocked floors: {floorsCompleted.Count}");
        }
        // One Climb always starts fresh from floor 1
    }
}