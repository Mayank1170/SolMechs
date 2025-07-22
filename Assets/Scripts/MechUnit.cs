using UnityEngine;
using System.Collections.Generic;

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

        public void ApplyBuff(ModuleSlot slot, string stat, int stageDelta)
        {
            if (partStatuses.ContainsKey(slot))
            {
                var status = partStatuses[slot];
                status.buffs[stat] = status.buffs.GetValueOrDefault(stat, 0) + stageDelta;
            }
        }

        public void ResetPartBuffs(ModuleSlot slot)
        {
            if (partStatuses.ContainsKey(slot))
            {
                partStatuses[slot].buffs.Clear();
            }
        }
    }

    [System.Serializable]
    public class PartStatus
    {
        public string partName;
        public int maxHP;
        public int currentHP;
        public Dictionary<string, int> buffs = new Dictionary<string, int>(); // Stages (e.g., "DEF": 1 for +1 DEF stage)
        public bool IsDestroyed => currentHP <= 0;
    }
}