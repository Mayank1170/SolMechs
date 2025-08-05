using UnityEngine;
using MechBattle;

namespace MechBattle
{
    [CreateAssetMenu(menuName = "MechBattle/Matrix")]
    public class Matrix : ScriptableObject
    {
        public string matrixCode;
        public string matrixName;
        public StatBlock baseStats;
        public string passive1;
        public string passive2;
    }
}