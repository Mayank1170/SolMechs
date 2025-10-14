using UnityEngine;
using System.Collections.Generic;
using MechBattle;

public class DroneSwarmLoader : MonoBehaviour
{
    [Header("Drone Swarm Configuration")]
    public DroneCatalog droneCatalog;

    [Header("4 Drone Slots (Wave)")]
    [Tooltip("Drone in Right Arm position")]
    public Drone slot0Drone;

    [Tooltip("Drone in Left Arm position")]
    public Drone slot1Drone;

    [Tooltip("Drone in Lower Body position")]
    public Drone slot2Drone;

    [Tooltip("Drone in Matrix position (4th targetable drone)")]
    public Drone slot3Drone;

    // Exposed for UI sprites
    [HideInInspector] public Drone ResolvedSlot0;
    [HideInInspector] public Drone ResolvedSlot1;
    [HideInInspector] public Drone ResolvedSlot2;
    [HideInInspector] public Drone ResolvedSlot3;

    public DroneSwarmUnit GetUnitData()
    {
        Drone d0 = slot0Drone;
        Drone d1 = slot1Drone;
        Drone d2 = slot2Drone;
        Drone d3 = slot3Drone;

        // Cache for UI
        ResolvedSlot0 = d0;
        ResolvedSlot1 = d1;
        ResolvedSlot2 = d2;
        ResolvedSlot3 = d3;

        // Create dummy matrix (structural only)
        var dummyMatrix = ScriptableObject.CreateInstance<Matrix>();
        dummyMatrix.matrixName = "Fan Swarm";
        dummyMatrix.baseStats = new StatBlock
        {
            HP = d3 != null ? d3.statModifiers.HP : 100, // CHANGED: Use 4th drone's HP
            ATK = 20,
            DEF = 20,
            ENG = 20,
            SPD = 20,
            SYS = 20
        };

        var unit = new DroneSwarmUnit
        {
            Name = "Fan Swarm",
            chassis = dummyMatrix,
            matrixHP = d3 != null ? d3.statModifiers.HP : 100 // CHANGED: 4th drone HP goes here
        };

        // Map first 3 drones to arm/lower slots (use partStatuses)
        if (d0 != null)
        {
            unit.modules[ModuleSlot.RightArm] = d0.ToModuleData(ModuleSlot.RightArm);
            unit.partStatuses[ModuleSlot.RightArm] = d0.ToPartStatus();
        }

        if (d1 != null)
        {
            unit.modules[ModuleSlot.LeftArm] = d1.ToModuleData(ModuleSlot.LeftArm);
            unit.partStatuses[ModuleSlot.LeftArm] = d1.ToPartStatus();
        }

        if (d2 != null)
        {
            unit.modules[ModuleSlot.LowerBody] = d2.ToModuleData(ModuleSlot.LowerBody);
            unit.partStatuses[ModuleSlot.LowerBody] = d2.ToPartStatus();
        }

        // 4th drone in Matrix slot (use matrixHP, NOT partStatuses)
        if (d3 != null)
        {
            unit.modules[ModuleSlot.Matrix] = d3.ToModuleData(ModuleSlot.Matrix);
            // DO NOT create partStatuses[ModuleSlot.Matrix] - use matrixHP instead!
        }

        return unit;
    }
}