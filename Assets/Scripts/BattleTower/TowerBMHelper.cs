using MechBattle;
using System.Collections.Generic;

/// <summary>
/// Minimal compatibility layer to provide current HP arrays expected by Tower scripts,
/// using the real BattleManager (which stores HP inside MechUnit/partStatuses).
/// Order: [RightArm, LeftArm, LowerBody, Matrix].
/// </summary>
public static class TowerBMHelper
{
    private static readonly ModuleSlot[] Order =
        { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody, ModuleSlot.Matrix };

    /// <summary>Return player's current HPs in the expected order.</summary>
    public static int[] GetPlayerCurrentHPs(BattleManager bm) => BuildHps(bm?.playerUnit);

    /// <summary>Return enemy's current HPs in the expected order.</summary>
    public static int[] GetEnemyCurrentHPs(BattleManager bm) => BuildHps(bm?.enemyUnit);

    /// <summary>Team is defeated if all entries are <= 0.</summary>
    public static bool IsTeamDefeated(int[] hps)
    {
        if (hps == null || hps.Length == 0) return true;
        for (int i = 0; i < hps.Length; i++)
            if (hps[i] > 0) return false;
        return true;
    }

    /// <summary>True if player's team is defeated (all parts at 0).</summary>
    public static bool IsPlayerDefeated(BattleManager bm) => IsTeamDefeated(GetPlayerCurrentHPs(bm));

    /// <summary>True if enemy's team is defeated (all parts at 0).</summary>
    public static bool IsEnemyDefeated(BattleManager bm) => IsTeamDefeated(GetEnemyCurrentHPs(bm));

    // ---------- internal ----------
    private static int[] BuildHps(MechUnit unit)
    {
        var arr = new int[4];
        if (unit == null) return arr; // zeros

        for (int i = 0; i < Order.Length; i++)
            arr[i] = GetHP(unit, Order[i]);

        return arr;
    }

    private static int GetHP(MechUnit unit, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix) return unit.matrixHP;
        if (unit.partStatuses != null && unit.partStatuses.TryGetValue(slot, out var status))
            return status.currentHP;
        return 0;
    }
}
