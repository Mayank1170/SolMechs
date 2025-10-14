using UnityEngine;

namespace MechBattle
{
    [CreateAssetMenu(fileName = "FanSwarmConfig", menuName = "MechBattle/FanSwarmConfig")]
    public class FanSwarmConfig : ScriptableObject
    {
        [Header("Recruitment Settings")]
        [Tooltip("Time in seconds for fans to join via chat")]
        public float recruitmentTimeSeconds = 30f;

        [Tooltip("Maximum fans per swarm (fixed at 4 for now)")]
        public int maxFansPerSwarm = 4;

        [Tooltip("Allow multiple fans to pick same drone type")]
        public bool allowDuplicateDroneTypes = true;

        [Header("Attack Sequencing")]
        [Tooltip("Delay between each drone attack for visual clarity")]
        public float delayBetweenDroneAttacks = 1.0f;

        [Tooltip("Show individual damage numbers with fan names")]
        public bool showIndividualDamageNumbers = true;

        [Tooltip("Show targeting indicators before attacks")]
        public bool showTargetingIndicators = true;

        [Header("Balance (4x Attack Multiplier)")]
        [Tooltip("Global scale for all drone damage (since 4 attack per turn)")]
        public float droneGlobalDamageScale = 1.0f;

        [Tooltip("Global scale for all drone HP")]
        public float droneGlobalHPScale = 1.0f;

        [Tooltip("Bonus to streamer mech stats per living fan")]
        public float streamerMechBonusPerLivingFan = 0.03f;

        [Header("Auto-Targeting Behavior")]
        [Tooltip("Drones automatically retarget Matrix when unlocked")]
        public bool dronesAutoRetargetMatrix = true;

        [Tooltip("Prefer targeting lowest HP parts")]
        public bool preferLowestHPTargets = true;

        [Tooltip("Allow all drones to focus Matrix simultaneously")]
        public bool allowMatrixFocusFire = true;

        [Header("Chat Commands & Aliases")]
        public string[] titanAliases = { "titan", "tank" };
        public string[] strikerAliases = { "striker", "attack", "dps" };
        public string[] arclightAliases = { "arclight", "energy", "arc" };
        public string[] heartcoreAliases = { "heartcore", "support", "heart" };
        public string[] solusAliases = { "solus", "balanced", "all" };
        public string[] underdogAliases = { "underdog", "special", "commemorative", "jam" };

        [Header("UI & Feedback")]
        [Tooltip("Max characters for fan display names")]
        public int maxFanNameLength = 12;

        [Tooltip("Duration to show recruitment UI messages")]
        public float recruitmentFeedbackDuration = 3f;

        public bool IsValidAlias(string command, DroneType type)
        {
            command = command.ToLower().Trim();

            switch (type)
            {
                case DroneType.Titan:
                    return System.Array.Exists(titanAliases, alias => alias == command);
                case DroneType.Striker:
                    return System.Array.Exists(strikerAliases, alias => alias == command);
                case DroneType.Arclight:
                    return System.Array.Exists(arclightAliases, alias => alias == command);
                case DroneType.HeartCore:
                    return System.Array.Exists(heartcoreAliases, alias => alias == command);
                case DroneType.Solus:
                    return System.Array.Exists(solusAliases, alias => alias == command);
                case DroneType.Underdog:
                    return System.Array.Exists(underdogAliases, alias => alias == command);
                default:
                    return false;
            }
        }
    }
}