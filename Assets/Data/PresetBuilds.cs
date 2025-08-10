// Assets/Scripts/SolMechs/Data/PresetBuilds.cs
using UnityEngine;

namespace MechBattle
{
    [CreateAssetMenu(menuName = "SolMechs/Data/PresetBuilds")]
    public class PresetBuilds : ScriptableObject
    {
        public MechBuild Titan;     // family 01
        public MechBuild Striker;   // family 02
        public MechBuild Arclight;  // family 03
        public MechBuild HeartCore; // family 04
    }
}
