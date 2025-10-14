using UnityEngine;
using System.Collections.Generic;

namespace MechBattle
{
    [CreateAssetMenu(menuName = "MechBattle/DroneCatalog")]
    public class DroneCatalog : ScriptableObject
    {
        [Header("Drone Collection")]
        public List<Drone> allDrones = new List<Drone>();

        private Dictionary<string, Drone> droneDict = new Dictionary<string, Drone>();
        private Dictionary<DroneType, Drone> typeDict = new Dictionary<DroneType, Drone>();

        public void Init()
        {
            droneDict.Clear();
            typeDict.Clear();

            foreach (var drone in allDrones)
            {
                if (drone != null)
                {
                    droneDict[drone.droneCode] = drone;
                    typeDict[drone.droneType] = drone;
                }
            }

            Debug.Log($"[DroneCatalog] Initialized with {allDrones.Count} drones");
        }

        public Drone Get(string droneCode)
        {
            if (droneDict.Count == 0) Init();
            return droneDict.GetValueOrDefault(droneCode);
        }

        public Drone GetByType(DroneType type)
        {
            if (typeDict.Count == 0) Init();
            return typeDict.GetValueOrDefault(type);
        }

        public Drone GetByCommandString(string command)
        {
            // Parse chat commands like "titan", "striker", "underdog"
            command = command.ToLower().Trim();

            switch (command)
            {
                case "titan": return GetByType(DroneType.Titan);
                case "striker": return GetByType(DroneType.Striker);
                case "arclight": return GetByType(DroneType.Arclight);
                case "heartcore": return GetByType(DroneType.HeartCore);
                case "solus": return GetByType(DroneType.Solus);
                case "underdog":
                case "special":
                case "commemorative": return GetByType(DroneType.Underdog);
                default: return null;
            }
        }

        public List<Drone> GetAllDrones()
        {
            return new List<Drone>(allDrones);
        }

        public bool IsValidCommand(string command)
        {
            return GetByCommandString(command) != null;
        }
    }
}