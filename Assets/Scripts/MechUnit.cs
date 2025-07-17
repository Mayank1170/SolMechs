using UnityEngine;
using System.Collections.Generic;
using MechBattle; // Para ModuleSlot, se necessário

namespace MechBattle
{
    [System.Serializable]
    public class MechUnit
    {
        public string Name;
        public Matrix chassis; // Agora referencia a definição externa
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
    }

    [System.Serializable]
    public class PartStatus
    {
        public string partName;
        public int maxHP;
        public int currentHP;
        public Dictionary<string, int> buffs = new Dictionary<string, int>(); // Tracks buffs (e.g., "DEF": 20)
        public bool IsDestroyed => currentHP <= 0;
    }
}