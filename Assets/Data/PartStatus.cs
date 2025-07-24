using UnityEngine;
using System.Collections.Generic;

namespace MechBattle
{
    [System.Serializable]
    public class PartStatus
    {
        public string partName;
        public int maxHP;
        public int currentHP;
        public Dictionary<string, int> buffs = new Dictionary<string, int>(); // Stages (e.g., "DEF": 1 for +1 DEF stage)
        public Dictionary<StatusEffect, int> statusDurations = new Dictionary<StatusEffect, int>(); // Duração de status effects
        public bool IsDestroyed => currentHP <= 0;
    }
}