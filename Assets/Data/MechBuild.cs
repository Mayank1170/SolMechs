using UnityEngine;

namespace MechBattle
{
    /// <summary>
    /// Stores the selected parts by their codes (e.g., M01, RA01, LA01, IN01).
    /// Saved as JSON so any scene can read the player's mech.
    /// </summary>
    [System.Serializable]
    public class MechBuild
    {
        public string matrixId;     // e.g., "M01"
        public string rightArmId;   // e.g., "RA01"
        public string leftArmId;    // e.g., "LA01"
        public string lowerBodyId;  // e.g., "IN01"
    }
}
