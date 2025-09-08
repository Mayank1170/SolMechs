// Save as: Assets/Scripts/TowerFloorData.cs
// Data structure that defines the configuration for each tower floor
// Contains enemy mech setup and special modifiers for boss floors

using UnityEngine;

/// <summary>
/// Configuration data for a single tower floor
/// Defines the enemy mech composition and any special modifiers
/// Uses numbered part system: MT01, RA01, LA01, LO01, etc.
/// </summary>
[System.Serializable]
public class TowerFloorData
{
    [Header("Enemy Mech Configuration")]
    [Tooltip("Display name for this floor's enemy (shown in UI)")]
    public string enemyName = "Enemy Mech";

    [Tooltip("Matrix/chassis code (e.g., 'MT01' for Titan Matrix)")]
    public string chassisCode = "MT01";

    [Tooltip("Right arm module code (e.g., 'RA01' for Titan Right Arm)")]
    public string rightArmCode = "RA01";

    [Tooltip("Left arm module code (e.g., 'LA01' for Titan Left Arm)")]
    public string leftArmCode = "LA01";

    [Tooltip("Lower body module code (e.g., 'LO01' for Titan Lower Body)")]
    public string lowerBodyCode = "LO01";

    [Header("Special Floor Modifiers")]
    [Tooltip("Mark this as a boss floor for enhanced difficulty and rewards")]
    public bool isBossFloor = false;

    [Tooltip("Multiplier for all base stats (1.0 = normal, 1.5 = 50% boost, etc.)")]
    [Range(0.5f, 3.0f)]
    public float statBoostMultiplier = 1.0f;

    [Tooltip("Special ability description for this floor (currently for display only)")]
    public string specialAbility = "";

    [Tooltip("Special color scheme for boss floors (future feature)")]
    public Color bossColor = Color.white;

    /// <summary>
    /// Check if this floor has any special modifiers applied
    /// </summary>
    public bool HasSpecialModifiers
    {
        get
        {
            return isBossFloor ||
                   statBoostMultiplier != 1.0f ||
                   !string.IsNullOrEmpty(specialAbility);
        }
    }

    /// <summary>
    /// Get a formatted description of this floor's special features
    /// </summary>
    public string GetSpecialDescription()
    {
        if (!HasSpecialModifiers) return "Standard floor";

        string description = "";

        if (isBossFloor) description += "Boss Floor ";
        if (statBoostMultiplier != 1.0f) description += $"({statBoostMultiplier:P0} Stats) ";
        if (!string.IsNullOrEmpty(specialAbility)) description += $"[{specialAbility}]";

        return description.Trim();
    }

    /// <summary>
    /// Get complete part configuration as formatted string for debugging
    /// </summary>
    public string GetPartConfiguration()
    {
        return $"Matrix: {chassisCode}, RightArm: {rightArmCode}, LeftArm: {leftArmCode}, LowerBody: {lowerBodyCode}";
    }

    /// <summary>
    /// Validate that all part codes are properly formatted
    /// </summary>
    public bool IsValidConfiguration()
    {
        return !string.IsNullOrEmpty(chassisCode) &&
               !string.IsNullOrEmpty(rightArmCode) &&
               !string.IsNullOrEmpty(leftArmCode) &&
               !string.IsNullOrEmpty(lowerBodyCode) &&
               chassisCode.Length >= 4 &&
               rightArmCode.Length >= 4 &&
               leftArmCode.Length >= 4 &&
               lowerBodyCode.Length >= 4;
    }
}