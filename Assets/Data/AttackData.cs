using UnityEngine;
using MechBattle;

namespace MechBattle
{
    [System.Serializable]
    public class AttackData
    {
        public string attackName;
        public int damage;
        public string type;
        public MechBattle.TargetType target;
    }
}