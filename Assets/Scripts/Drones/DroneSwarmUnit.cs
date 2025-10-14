using System.Collections.Generic;

namespace MechBattle
{
    [System.Serializable]
    public class DroneSwarmUnit : MechUnit
    {
        // All 4 drone slots are always targetable (no Matrix lock)
        public new bool CanAttackMatrix()
        {
            return true;
        }

        // === NEW: Victory condition for Fan Swarm ===
        /// <summary>
        /// Checks if all 4 drones are destroyed.
        /// First 3 drones use partStatuses, 4th uses matrixHP.
        /// </summary>
        public bool AreAllDronesDestroyed()
        {
            bool drone1Dead = IsPartBroken(ModuleSlot.RightArm);
            bool drone2Dead = IsPartBroken(ModuleSlot.LeftArm);
            bool drone3Dead = IsPartBroken(ModuleSlot.LowerBody);
            bool drone4Dead = (matrixHP <= 0);

            return drone1Dead && drone2Dead && drone3Dead && drone4Dead;
        }

        // Optional: Store fan data for future features
        public List<FanData> fanOwners = new List<FanData>();

        // Optional: Track which wave this is
        public int waveNumber = 1;
    }
}