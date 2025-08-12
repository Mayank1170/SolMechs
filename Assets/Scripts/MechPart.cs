// MechPart.cs
using UnityEngine;
using System.Collections.Generic;

namespace MechBattle
{
    [CreateAssetMenu(menuName = "MechBattle/Part")]
    public class MechPart : ScriptableObject
    {
        public string partCode;
        public string partName;
        public StatBlock statModifiers = new StatBlock();
        public List<MoveDefinition> moves = new List<MoveDefinition>();

        [TextArea(5, 10)]
        public string rawData;

        // --- NEW: single sprite used by the editor (and can be reused elsewhere) ---
        [Header("UI / Paper Doll")]
        [Tooltip("Single sprite for this part (used by EditorController.TryGetSprite).")]
        public Sprite editorSprite;

        public ModuleData ToModuleData(ModuleSlot slot)
        {
            if (moves == null || moves.Count == 0)
            {
                Debug.LogError($"⚠️ MechPart '{partName}' has no Move defined!");
                return null;
            }

            return new ModuleData
            {
                moduleName = partName,
                slot = slot,
                ATK = statModifiers.ATK,
                ENG = statModifiers.ENG,
                SPD = statModifiers.SPD,
                partCode = partCode,
                attack = new AttackData
                {
                    attackName = moves[0].moveName,
                    damage = moves[0].baseDamage,
                    type = moves[0].damageType ?? "Physical",
                    target = (TargetType)moves[0].targetType,
                    effect = moves[0].effect
                }
            };
        }
    }

    [System.Serializable]
    public class MoveDefinition
    {
        public string moveName;
        public int baseDamage;
        public string damageType = "Physical";
        public TargetType targetType = TargetType.Single;
        public string effect;
    }

    public enum TargetType { Single, Self, AOE }
}
