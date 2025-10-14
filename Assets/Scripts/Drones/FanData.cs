using UnityEngine;

namespace MechBattle
{
    [System.Serializable]
    public class FanData
    {
        public string userId;
        public string displayName;
        public DroneType droneType;
        public int slotIndex; // 0-3

        public FanData(string userId, string displayName, DroneType droneType, int slotIndex)
        {
            this.userId = userId;
            this.displayName = displayName;
            this.droneType = droneType;
            this.slotIndex = slotIndex;
        }
    }
}