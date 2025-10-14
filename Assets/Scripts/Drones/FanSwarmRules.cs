using UnityEngine;
using UnityEngine.UI;
using MechBattle;
using System.Collections.Generic;

public class FanSwarmRules : MonoBehaviour
{
    private BattleManager battleManager;
    private UIManager uiManager;
    private DroneSwarmLoader droneLoader;
    private bool victoryOverridden = false;

    void Start()
    {
        battleManager = GetComponent<BattleManager>();
        uiManager = GetComponent<UIManager>();

        // === CRITICAL: Disable default victory checks for Fan Swarm ===
        if (battleManager != null)
        {
            battleManager.skipDefaultVictoryCheck = true;
            Debug.Log("[FanSwarmRules] Fan Swarm mode active - custom victory rules enabled");
        }
        else
        {
            Debug.LogError("[FanSwarmRules] BattleManager not found!");
        }

        var enemyMech = GameObject.Find("EnemyMech");
        if (enemyMech != null)
        {
            droneLoader = enemyMech.GetComponent<DroneSwarmLoader>();
        }
    }

    void LateUpdate()
    {
        // Override target buttons AFTER UIManager renders them
        OverrideTargetButtons();

        // Check for correct victory condition (all 4 drones destroyed)
        CheckFanSwarmVictory();
    }

    private void OverrideTargetButtons()
    {
        if (uiManager?.actionButtons == null || battleManager?.enemyUnit == null) return;

        var enemy = battleManager.enemyUnit as DroneSwarmUnit;
        if (enemy == null) return; // Not a drone swarm, skip

        // Check if we're in target selection mode
        bool isTargetSelection = false;
        foreach (var btn in uiManager.actionButtons)
        {
            if (!btn.gameObject.activeSelf) continue;
            var text = btn.GetComponentInChildren<Text>();
            if (text != null && text.text.StartsWith("Target:"))
            {
                isTargetSelection = true;
                break;
            }
        }

        if (!isTargetSelection) return;

        // === REBUILD TARGET BUTTONS WITH DRONE LOGIC ===
        RebuildDroneTargetButtons(enemy);
    }

    private void RebuildDroneTargetButtons(DroneSwarmUnit enemy)
    {
        if (uiManager?.actionButtons == null) return;

        // Define all 4 drone slots
        var droneSlots = new List<ModuleSlot>
        {
            ModuleSlot.RightArm,   // Drone 1
            ModuleSlot.LeftArm,    // Drone 2
            ModuleSlot.LowerBody,  // Drone 3
            ModuleSlot.Matrix      // Drone 4
        };

        int buttonIndex = 0;

        foreach (var slot in droneSlots)
        {
            if (buttonIndex >= uiManager.actionButtons.Length) break;

            // Check if drone is alive
            bool isAlive = IsDroneAlive(enemy, slot);

            if (!isAlive) continue; // Skip destroyed drones

            var btn = uiManager.actionButtons[buttonIndex];
            var text = btn.GetComponentInChildren<Text>();

            if (text != null)
            {
                // Set drone name
                string droneName = GetDroneName(GetDroneIndex(slot));
                text.text = $"Target: {droneName}";
            }

            btn.gameObject.SetActive(true);
            btn.interactable = true;

            // Ensure click listener is correct
            btn.onClick.RemoveAllListeners();
            ModuleSlot capturedSlot = slot;
            btn.onClick.AddListener(() => OnDroneTargetSelected(capturedSlot));

            buttonIndex++;
        }

        // Hide remaining buttons
        for (int i = buttonIndex; i < uiManager.actionButtons.Length; i++)
        {
            uiManager.actionButtons[i].gameObject.SetActive(false);
        }
    }

    private bool IsDroneAlive(DroneSwarmUnit enemy, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix)
        {
            // 4th drone uses matrixHP
            return enemy.matrixHP > 0;
        }
        else
        {
            // First 3 drones use partStatuses
            return !enemy.IsPartBroken(slot);
        }
    }

    private int GetDroneIndex(ModuleSlot slot)
    {
        switch (slot)
        {
            case ModuleSlot.RightArm: return 0;
            case ModuleSlot.LeftArm: return 1;
            case ModuleSlot.LowerBody: return 2;
            case ModuleSlot.Matrix: return 3;
            default: return 0;
        }
    }

    private void OnDroneTargetSelected(ModuleSlot targetSlot)
    {
        // Delegate back to BattleManager's ConfirmTarget
        if (battleManager != null)
        {
            var method = battleManager.GetType().GetMethod("ConfirmTarget",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(battleManager, new object[] { targetSlot });
            }
        }
    }

    private void CheckFanSwarmVictory()
    {
        if (battleManager?.enemyUnit == null || victoryOverridden) return;

        var enemy = battleManager.enemyUnit as DroneSwarmUnit;
        if (enemy == null) return;

        // Victory = ALL 4 drones destroyed
        bool allDestroyed = enemy.AreAllDronesDestroyed();

        if (allDestroyed && !victoryOverridden)
        {
            victoryOverridden = true;
            ForceVictory();
        }
    }

    private void ForceVictory()
    {
        Debug.Log("[FanSwarmRules] All 4 drones destroyed! Wave complete!");

        uiManager?.LogMessage("\n=== ALL DRONES DESTROYED! ===\n");
        uiManager?.LogMessage("Wave Complete!\n");

        // Disable buttons and show result
        if (uiManager != null)
        {
            uiManager.DisableAllButtons();
            uiManager.HideBackButton();
        }

        // Call the BattleManager's EndBattle through reflection to maintain proper state
        if (battleManager != null)
        {
            var method = battleManager.GetType().GetMethod("EndBattle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(battleManager, new object[] { true }); // playerWon = true
            }
        }
    }

    private string GetDroneName(int slotIndex)
    {
        if (droneLoader == null) return $"Drone {slotIndex + 1}";

        Drone drone = null;
        switch (slotIndex)
        {
            case 0: drone = droneLoader.ResolvedSlot0; break;
            case 1: drone = droneLoader.ResolvedSlot1; break;
            case 2: drone = droneLoader.ResolvedSlot2; break;
            case 3: drone = droneLoader.ResolvedSlot3; break;
        }

        return drone?.droneName ?? $"Drone {slotIndex + 1}";
    }
}