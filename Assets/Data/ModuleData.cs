using UnityEngine;
using System.Collections.Generic;

namespace MechBattle
{
    [System.Serializable]
    public class ModuleData
    {
        public string moduleName;
        public ModuleSlot slot;
        public int ATK;
        public int ENG;
        public int SPD;
        public AttackData attack;
        public string partCode; // Adicionado para carregar o MechPart corretamente
    }
}