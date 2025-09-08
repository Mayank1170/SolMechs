using UnityEngine;
using MechBattle;

/// <summary>
/// Minimal extension that lives on the SAME GameObject as BattleManager
/// and exposes simple "defeated?" checks for Tower systems.
/// </summary>
[DisallowMultipleComponent]
public class BattleManagerTowerExtension : MonoBehaviour
{
    private BattleManager battleManager;

    private void Start()
    {
        // This expects to be on the same GameObject as BattleManager.
        battleManager = GetComponent<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("[BattleManagerTowerExtension] BattleManager not found on this GameObject.");
            enabled = false;
        }
    }

    /// <summary>
    /// Enemy is defeated if Matrix HP <= 0 or all non-matrix parts are broken (can't act).
    /// </summary>
    public bool IsEnemyDefeated()
    {
        if (battleManager == null) return false;
        return IsUnitDefeated(battleManager.enemyUnit);
    }

    /// <summary>
    /// Player is defeated if Matrix HP <= 0 or all non-matrix parts are broken (can't act).
    /// </summary>
    public bool IsPlayerDefeated()
    {
        if (battleManager == null) return false;
        return IsUnitDefeated(battleManager.playerUnit);
    }

    // -------- helpers --------

    private bool IsUnitDefeated(MechUnit unit)
    {
        if (unit == null) return true;

        // Matrix KO means defeated.
        if (unit.matrixHP <= 0) return true;

        // If *any* combat part still has HP, unit is not defeated.
        if (HasHp(unit, ModuleSlot.RightArm)) return false;
        if (HasHp(unit, ModuleSlot.LeftArm)) return false;
        if (HasHp(unit, ModuleSlot.LowerBody)) return false;

        // All non-matrix parts are at 0 → defeated.
        return true;
    }

    private bool HasHp(MechUnit unit, ModuleSlot slot)
    {
        if (unit == null) return false;
        if (!unit.partStatuses.ContainsKey(slot)) return false;
        return unit.partStatuses[slot].currentHP > 0;
    }
}
