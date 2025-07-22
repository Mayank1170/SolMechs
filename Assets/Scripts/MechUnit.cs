using UnityEngine;
using System.Collections.Generic;
using MechBattle;
using static MechBattle.PartStatus;

namespace MechBattle
{
    [System.Serializable]
    public class MechUnit
    {
        public string Name;
        public Matrix chassis;
        public int matrixHP;
        public Dictionary<ModuleSlot, ModuleData> modules = new();
        public Dictionary<ModuleSlot, PartStatus> partStatuses = new();

        public bool IsPartBroken(ModuleSlot slot)
        {
            return partStatuses.ContainsKey(slot) && partStatuses[slot].currentHP <= 0;
        }

        public bool CanAttackMatrix()
        {
            return IsPartBroken(ModuleSlot.RightArm) || IsPartBroken(ModuleSlot.LeftArm);
        }

        // Enhanced method to apply a buff/debuff to a slot
        public void ApplyBuff(ModuleSlot slot, string stat, int value, int duration)
        {
            if (partStatuses.ContainsKey(slot))
            {
                partStatuses[slot].buffs[stat] = new Buff { value = value, duration = duration };
            }
        }

        // New method for applying self-effects from a move
        public void ApplySelfEffect(ModuleSlot sourceSlot, AttackData attack)
        {
            if (attack == null || string.IsNullOrEmpty(attack.attackName)) return;
            MechPart part = FindMechPartByAttack(this, attack);
            if (part == null || part.moves == null || part.moves.Count == 0) return;

            string effect = part.moves[0].effect;
            if (!string.IsNullOrEmpty(effect))
            {
                if (effect.StartsWith("+") || effect.StartsWith("-"))
                {
                    string[] effectParts = effect.Split(new[] { ' ' }, 2);
                    if (effectParts.Length == 2)
                    {
                        string valueStr = effectParts[0].Replace("+", "").Replace("-", "");
                        int value = int.Parse(valueStr);
                        string stat = effectParts[1].Split(' ')[0].ToUpper();
                        bool isBuff = effect.StartsWith("+");

                        int buffValue = isBuff ? value : -value;
                        ApplyBuff(sourceSlot, stat, buffValue, 3); // Default duration 3 turns
                    }
                }
            }
        }

        // Helper method to find MechPart (moved from GameController for encapsulation)
        private MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
        {
            foreach (var kvp in unit.modules)
            {
                if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                    return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Key.ToString()}");
            }
            return null;
        }
    }

    [System.Serializable]
    public class PartStatus
    {
        public string partName;
        public int maxHP;
        public int currentHP;
        public Dictionary<string, Buff> buffs = new Dictionary<string, Buff>(); // Updated to Buff struct
        public bool IsDestroyed => currentHP <= 0;

        // Nested struct for buff data
        [System.Serializable]
        public struct Buff
        {
            public int value;
            public int duration;
        }
    }
}