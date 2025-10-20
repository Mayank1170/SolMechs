using UnityEngine;
using System.Collections.Generic;

namespace MechBattle
{
    [CreateAssetMenu(menuName = "MechBattle/Drone")]
    public class Drone : ScriptableObject
    {
        public string droneCode;
        public string droneName;
        public StatBlock statModifiers = new StatBlock();
        public List<MoveDefinition> moves = new List<MoveDefinition>();

        [TextArea(5, 10)]
        public string rawData;

        [Header("UI / Paper Doll")]
        [Tooltip("Animation frames (3 sprites for propeller rotation)")]
        public Sprite[] idleFrames = new Sprite[3];

        [Tooltip("Animation speed (frames per second)")]
        [Range(1f, 30f)]
        public float animationFPS = 10f;

        [Header("Drone Classification")]
        [Tooltip("What mech this drone is derived from")]
        public DroneType droneType;

        [Header("Fan Swarm Balance")]
        [Tooltip("Multiplier applied to base stats for balance")]
        public float balanceMultiplier = 1.0f;

        public ModuleData ToModuleData(ModuleSlot slot)
        {
            if (moves == null || moves.Count == 0)
            {
                Debug.LogError($"⚠️ Drone '{droneName}' has no Move defined!");
                return null;
            }

            return new ModuleData
            {
                moduleName = droneName,
                slot = slot,
                ATK = Mathf.RoundToInt(statModifiers.ATK * balanceMultiplier),
                ENG = Mathf.RoundToInt(statModifiers.ENG * balanceMultiplier),
                SPD = Mathf.RoundToInt(statModifiers.SPD * balanceMultiplier),
                partCode = droneCode,
                attack = new AttackData
                {
                    attackName = moves[0].moveName,
                    damage = Mathf.RoundToInt(moves[0].baseDamage * balanceMultiplier),
                    type = moves[0].damageType ?? "Physical",
                    target = (TargetType)moves[0].targetType,
                    effect = moves[0].effect
                }
            };
        }

        public PartStatus ToPartStatus()
        {
            return new PartStatus
            {
                partName = droneName,
                maxHP = Mathf.RoundToInt(statModifiers.HP * balanceMultiplier),
                currentHP = Mathf.RoundToInt(statModifiers.HP * balanceMultiplier),
                buffs = new Dictionary<string, int>()
            };
        }
    }

    public enum DroneType
    {
        Titan,
        Striker,
        Arclight,
        HeartCore,
        Solus,
        Underdog
    }
}