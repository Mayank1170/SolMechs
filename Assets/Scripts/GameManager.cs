using UnityEngine;
using System;
using System.Collections.Generic;
using MechBattle; // Required for Drone class

[Serializable]
public class ViewerData
{
    public string userId;
    public string username;
    public int droneId;
    public string selectedDroneType; // "drone1", "drone2", "drone3", "drone4", "drone5", "drone6"
}

[Serializable]
public class GameData
{
    public string gameId;
    public string role; // "streamer" or "viewer"
    public List<ViewerData> viewers;
}

public class GameManager : MonoBehaviour
{
    [Header("Drone Swarm Loader")]
    public DroneSwarmLoader droneSwarmLoader; // Reference to EnemyDrones DroneSwarmLoader

    [Header("Drone Assets - All 6 Types (from Assets/Data/Drones)")]
    public Drone drone1Asset; // D01 - Assign in Inspector
    public Drone drone2Asset; // D02 - Assign in Inspector
    public Drone drone3Asset; // D03 - Assign in Inspector
    public Drone drone4Asset; // D04 - Assign in Inspector
    public Drone drone5Asset; // D05 - Assign in Inspector
    public Drone drone6Asset; // D06 - Assign in Inspector

    private GameData currentGameData;

    void Start()
    {
        Debug.Log("[GameManager] GameManager Started - Ready to receive viewer data");

        // Verify setup
        if (droneSwarmLoader == null)
        {
            Debug.LogError("[GameManager] ❌ SETUP ERROR: DroneSwarmLoader not assigned!");
        }

        if (drone1Asset == null || drone2Asset == null || drone3Asset == null ||
            drone4Asset == null || drone5Asset == null || drone6Asset == null)
        {
            Debug.LogError("[GameManager] ❌ SETUP ERROR: Not all 6 drone assets assigned!");
            Debug.LogError($"[GameManager] Assets status: D1={drone1Asset != null}, D2={drone2Asset != null}, D3={drone3Asset != null}, D4={drone4Asset != null}, D5={drone5Asset != null}, D6={drone6Asset != null}");
        }
        else
        {
            Debug.Log("[GameManager] ✅ All 6 drone assets loaded successfully");
        }
    }

    // This method is called from JavaScript
    public void ReceiveGameData(string jsonData)
    {
        Debug.Log($"[GameManager] 📥 Received game data: {jsonData}");

        try
        {
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);

            Debug.Log($"[GameManager] Game ID: {currentGameData.gameId}");
            Debug.Log($"[GameManager] Role: {currentGameData.role}");
            Debug.Log($"[GameManager] Viewers: {currentGameData.viewers.Count}");

            // Assign viewer-selected drones to DroneSwarmLoader slots
            AssignDronesToLoader();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to parse game data: {e.Message}");
        }
    }

    private void AssignDronesToLoader()
    {
        if (droneSwarmLoader == null)
        {
            Debug.LogError("[GameManager] ❌ DroneSwarmLoader reference not assigned in Inspector!");
            return;
        }

        Debug.Log($"[GameManager] 🔄 Starting drone assignment for {currentGameData.viewers.Count} viewers");
        Debug.Log($"[GameManager] DroneSwarmLoader found: {droneSwarmLoader.name}");

        // Assign each viewer's selected drone to their slot
        foreach (var viewer in currentGameData.viewers)
        {
            Drone droneAsset = GetDroneAsset(viewer.selectedDroneType);

            if (droneAsset == null)
            {
                Debug.LogWarning($"[GameManager] No drone asset found for {viewer.selectedDroneType}, using drone1");
                droneAsset = drone1Asset;
            }

            // Get slot index (droneId is 1-4, array index is 0-3)
            int slotIndex = viewer.droneId - 1;

            // Assign to appropriate DroneSwarmLoader slot
            switch (slotIndex)
            {
                case 0:
                    droneSwarmLoader.slot0Drone = droneAsset;
                    Debug.Log($"[GameManager] ✅ Assigned {viewer.selectedDroneType} ({droneAsset.name}) to Slot 0 for {viewer.username}");
                    break;
                case 1:
                    droneSwarmLoader.slot1Drone = droneAsset;
                    Debug.Log($"[GameManager] ✅ Assigned {viewer.selectedDroneType} ({droneAsset.name}) to Slot 1 for {viewer.username}");
                    break;
                case 2:
                    droneSwarmLoader.slot2Drone = droneAsset;
                    Debug.Log($"[GameManager] ✅ Assigned {viewer.selectedDroneType} ({droneAsset.name}) to Slot 2 for {viewer.username}");
                    break;
                case 3:
                    droneSwarmLoader.slot3Drone = droneAsset;
                    Debug.Log($"[GameManager] ✅ Assigned {viewer.selectedDroneType} ({droneAsset.name}) to Slot 3 for {viewer.username}");
                    break;
                default:
                    Debug.LogWarning($"[GameManager] Invalid slot index {slotIndex} for viewer {viewer.username}");
                    break;
            }
        }

        Debug.Log($"[GameManager] ✅ All viewer drones assigned to DroneSwarmLoader!");

        // Log final slot configuration
        Debug.Log($"[GameManager] 📋 Final DroneSwarmLoader configuration:");
        Debug.Log($"[GameManager]   slot0Drone = {(droneSwarmLoader.slot0Drone != null ? droneSwarmLoader.slot0Drone.name : "null")}");
        Debug.Log($"[GameManager]   slot1Drone = {(droneSwarmLoader.slot1Drone != null ? droneSwarmLoader.slot1Drone.name : "null")}");
        Debug.Log($"[GameManager]   slot2Drone = {(droneSwarmLoader.slot2Drone != null ? droneSwarmLoader.slot2Drone.name : "null")}");
        Debug.Log($"[GameManager]   slot3Drone = {(droneSwarmLoader.slot3Drone != null ? droneSwarmLoader.slot3Drone.name : "null")}");

        Debug.Log("[GameManager] 🎯 About to call ReloadBattleWithNewDrones...");

        // Notify FanSwarmGameController to reload the drones
        ReloadBattleWithNewDrones();

        Debug.Log("[GameManager] ✅ ReloadBattleWithNewDrones call completed");
    }

    private void ReloadBattleWithNewDrones()
    {
        // Find FanSwarmGameController and tell it to reload drones
        var fanSwarmController = FindObjectOfType<FanSwarmGameController>();

        if (fanSwarmController != null)
        {
            Debug.Log("[GameManager] 🔄 Notifying FanSwarmGameController to reload drones...");
            fanSwarmController.ReloadEnemyDrones();
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ FanSwarmGameController not found - drones may not update in battle");
        }
    }

    private Drone GetDroneAsset(string droneType)
    {
        Debug.Log($"[GameManager] GetDroneAsset called with: {droneType}");

        switch (droneType)
        {
            case "drone1":
                Debug.Log("[GameManager] Returning drone1Asset (D01 - Assault)");
                return drone1Asset;
            case "drone2":
                Debug.Log("[GameManager] Returning drone2Asset (D02 - Tank)");
                return drone2Asset;
            case "drone3":
                Debug.Log("[GameManager] Returning drone3Asset (D03 - Speed)");
                return drone3Asset;
            case "drone4":
                Debug.Log("[GameManager] Returning drone4Asset (D04 - Artillery)");
                return drone4Asset;
            case "drone5":
                Debug.Log("[GameManager] Returning drone5Asset (D05 - Stealth)");
                return drone5Asset;
            case "drone6":
                Debug.Log("[GameManager] Returning drone6Asset (D06 - Berserker)");
                return drone6Asset;
            default:
                Debug.LogWarning($"[GameManager] Unknown drone type: {droneType}, defaulting to drone1");
                return drone1Asset;
        }
    }
}
