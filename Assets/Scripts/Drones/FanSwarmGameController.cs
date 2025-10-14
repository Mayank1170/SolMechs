using UnityEngine;
using MechBattle;

public class FanSwarmGameController : MonoBehaviour
{
    [Header("Mech GameObjects")]
    public GameObject playerMechObject;
    public GameObject enemyMechObject;

    private UIManager uiManager;
    private BattleManager battleManager;

    void Awake()
    {
        uiManager = GetComponent<UIManager>();
        battleManager = GetComponent<BattleManager>();

        // Get player unit (normal loader)
        MechUnit player = playerMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();

        // Get enemy drones (DroneSwarmUnit)
        var droneLoader = enemyMechObject.GetComponent<DroneSwarmLoader>();
        DroneSwarmUnit enemy = droneLoader?.GetUnitData(); // Specific type

        if (player == null || enemy == null)
        {
            Debug.LogError("⚠️ [FanSwarm] Failed to load units!");
            return;
        }

        Debug.Log($"[FanSwarm] Loaded: {player.Name} vs {enemy.Name} (4 drones, all targetable)");

        // Initialize battle (accepts MechUnit, so DroneSwarmUnit works via polymorphism)
        battleManager.Initialize(player, enemy, uiManager);
        uiManager.InitializeHealthBars(player, enemy, battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
        uiManager.DisableSliderInteractabilityAndHandles();

        // Initialize player paper doll
        var playerLoader = playerMechObject.GetComponent<MechUnitLoader>();
        uiManager.InitializePaperDolls(playerLoader, null);

        // Initialize enemy drone sprites
        InitializeDroneSprites(droneLoader);
    }

    void Start()
    {
        uiManager.LogMessage("=== FAN SWARM BATTLE ===\n");
        uiManager.LogMessage($"{battleManager.playerUnit.Name} vs Fan Swarm!\n");
        uiManager.LogMessage("Destroy all 4 drones to win!\n\n");
        battleManager.StartTurn();
    }

    private void InitializeDroneSprites(DroneSwarmLoader droneLoader)
    {
        if (droneLoader == null || uiManager == null) return;

        SetDroneSprite(uiManager.enemyRightArmImg, droneLoader.ResolvedSlot0);
        SetDroneSprite(uiManager.enemyLeftArmImg, droneLoader.ResolvedSlot1);
        SetDroneSprite(uiManager.enemyLowerImg, droneLoader.ResolvedSlot2);
        SetDroneSprite(uiManager.enemyMatrixImg, droneLoader.ResolvedSlot3);

        Debug.Log("[FanSwarm] Drone sprites initialized");
    }

    private void SetDroneSprite(UnityEngine.UI.Image img, Drone drone)
    {
        if (img == null) return;

        if (drone != null && drone.editorSprite != null)
        {
            img.sprite = drone.editorSprite;
            img.enabled = true;
            img.preserveAspect = true;
        }
        else
        {
            img.enabled = false;
            Debug.LogWarning($"[FanSwarm] Drone {drone?.droneName} has no sprite assigned");
        }
    }
}