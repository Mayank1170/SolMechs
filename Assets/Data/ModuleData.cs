using UnityEngine;
using MechBattle;

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
    }
}