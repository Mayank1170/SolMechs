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

        MechUnit player = playerMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();

        var droneLoader = enemyMechObject.GetComponent<DroneSwarmLoader>();
        DroneSwarmUnit enemy = droneLoader?.GetUnitData();

        if (player == null || enemy == null)
        {
            Debug.LogError("⚠️ [FanSwarm] Failed to load units!");
            return;
        }

        Debug.Log($"[FanSwarm] Loaded: {player.Name} vs {enemy.Name} (4 drones, all targetable)");

        battleManager.Initialize(player, enemy, uiManager);
        uiManager.InitializeHealthBars(player, enemy, battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
        uiManager.DisableSliderInteractabilityAndHandles();

        var playerLoader = playerMechObject.GetComponent<MechUnitLoader>();
        uiManager.InitializePaperDolls(playerLoader, null);

        InitializeDroneAnimations(droneLoader);
    }

    void Start()
    {
        uiManager.LogMessage("=== FAN SWARM BATTLE ===\n");
        uiManager.LogMessage($"{battleManager.playerUnit.Name} vs Fan Swarm!\n");
        uiManager.LogMessage("Destroy all 4 drones to win!\n\n");
        battleManager.StartTurn();
    }

    private void InitializeDroneAnimations(DroneSwarmLoader droneLoader)
    {
        if (droneLoader == null || uiManager == null) return;

        SetDroneAnimation(uiManager.enemyRightArmImg, droneLoader.ResolvedSlot0);
        SetDroneAnimation(uiManager.enemyLeftArmImg, droneLoader.ResolvedSlot1);
        SetDroneAnimation(uiManager.enemyLowerImg, droneLoader.ResolvedSlot2);
        SetDroneAnimation(uiManager.enemyMatrixImg, droneLoader.ResolvedSlot3);

        Debug.Log("[FanSwarm] Drone animations initialized");
    }

    private void SetDroneAnimation(UnityEngine.UI.Image img, Drone drone)
    {
        if (img == null) return;

        if (drone != null && drone.idleFrames != null && drone.idleFrames.Length > 0)
        {
            UISpriteAnimator animator = img.gameObject.GetComponent<UISpriteAnimator>();
            if (animator == null)
            {
                animator = img.gameObject.AddComponent<UISpriteAnimator>();
            }

            animator.SetAnimation(drone.idleFrames, drone.animationFPS);

            img.enabled = true;
            img.preserveAspect = true;

            Debug.Log($"[FanSwarm] Animation set for drone: {drone.droneName}");
        }
        else
        {
            img.enabled = false;
            Debug.LogWarning($"[FanSwarm] Drone {drone?.droneName} has no animation frames assigned");
        }
    }
}