using UnityEngine;
using System.Collections.Generic;
using MechBattle;

/// <summary>
/// Handles the battle logic for local multiplayer (Player 1 vs Player 2).
/// Controls turns, damage, effects, and win conditions.
/// </summary>
public class LocalBattleManager : MonoBehaviour
{
    public MechUnit player1Unit;
    public MechUnit player2Unit;

    private AttackData selectedAttack;
    private ModuleSlot selectedSourceSlot;

    private LocalUIManager uiManager;

    public Dictionary<ModuleSlot, int> player1MaxHPs = new Dictionary<ModuleSlot, int>();
    public Dictionary<ModuleSlot, int> player2MaxHPs = new Dictionary<ModuleSlot, int>();

    private Dictionary<MechUnit, Dictionary<string, int>> matrixBuffsDict = new Dictionary<MechUnit, Dictionary<string, int>>();

    private enum BattleState { Player1Turn, Player2Turn, Victory, Defeat }
    private BattleState currentState;

    // ==================== INITIALIZATION ====================

    public void Initialize(MechUnit player1, MechUnit player2, LocalUIManager ui)
    {
        player1Unit = player1;
        player2Unit = player2;
        uiManager = ui;

        matrixBuffsDict[player1Unit] = new Dictionary<string, int>();
        matrixBuffsDict[player2Unit] = new Dictionary<string, int>();

        InitializeMaxHPs();

        currentState = BattleState.Player1Turn; // Player 1 starts
    }

    private void InitializeMaxHPs()
    {
        if (player1Unit != null)
        {
            player1MaxHPs[ModuleSlot.Matrix] = player1Unit.matrixHP;
            foreach (var slot in player1Unit.partStatuses.Keys)
            {
                player1MaxHPs[slot] = player1Unit.partStatuses[slot].currentHP;
            }
        }
        if (player2Unit != null)
        {
            player2MaxHPs[ModuleSlot.Matrix] = player2Unit.matrixHP;
            foreach (var slot in player2Unit.partStatuses.Keys)
            {
                player2MaxHPs[slot] = player2Unit.partStatuses[slot].currentHP;
            }
        }
    }

    // ==================== TURN HANDLING ====================

    public void StartTurn()
    {
        if (currentState == BattleState.Player1Turn)
        {
            uiManager.LogMessage($"It’s {player1Unit.Name}’s turn.");
            uiManager.RenderActionButtonsPlayer1(player1Unit, (slot, attack) => SelectAttack(player1Unit, player2Unit, slot, attack));
        }
        else if (currentState == BattleState.Player2Turn)
        {
            uiManager.LogMessage($"It’s {player2Unit.Name}’s turn.");
            uiManager.RenderActionButtonsPlayer2(player2Unit, (slot, attack) => SelectAttack(player2Unit, player1Unit, slot, attack));
        }
    }

    public void SelectAttack(MechUnit attacker, MechUnit defender, ModuleSlot sourceSlot, AttackData attack)
    {
        if ((currentState == BattleState.Player1Turn && attacker != player1Unit) ||
            (currentState == BattleState.Player2Turn && attacker != player2Unit))
            return;

        if (attacker == null || attacker.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{sourceSlot} is broken!");
            return;
        }

        selectedAttack = attack;
        selectedSourceSlot = sourceSlot;

        MechPart part = FindMechPartByAttack(attacker, attack);
        bool isSelfTarget = part != null && part.moves != null && part.moves.Count > 0 &&
                            part.moves[0].targetType == MechBattle.TargetType.Self;

        if (isSelfTarget)
        {
            uiManager.RenderSelfTargetButtons(attacker, (targetSlot) => ConfirmSelfTarget(attacker, targetSlot));
        }
        else
        {
            uiManager.RenderTargetButtons(defender, (targetSlot) => ConfirmTarget(attacker, defender, targetSlot));
        }
    }

    public void ConfirmSelfTarget(MechUnit attacker, ModuleSlot targetSlot)
    {
        if ((currentState == BattleState.Player1Turn && attacker != player1Unit) ||
            (currentState == BattleState.Player2Turn && attacker != player2Unit))
            return;

        if (targetSlot != ModuleSlot.Matrix && attacker.IsPartBroken(targetSlot))
        {
            uiManager.LogMessage($"{targetSlot} is broken!");
            return;
        }

        uiManager.LogMessage($"{attacker.Name} used {selectedAttack?.attackName ?? "Attack"} on itself ({targetSlot}).");
        ApplyEffect(attacker, attacker, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(attacker, targetSlot, GetCurrentHP(attacker, targetSlot), player1MaxHPs, player2MaxHPs, player1Unit, player2Unit);

        EndTurn();
    }

    public void ConfirmTarget(MechUnit attacker, MechUnit defender, ModuleSlot targetSlot)
    {
        if ((currentState == BattleState.Player1Turn && attacker != player1Unit) ||
            (currentState == BattleState.Player2Turn && attacker != player2Unit))
            return;

        if (targetSlot == ModuleSlot.Matrix && !defender.CanAttackMatrix())
        {
            uiManager.LogMessage("Matrix locked!");
            return;
        }

        int damage = CalculateDamage(selectedAttack, attacker, defender, targetSlot, selectedSourceSlot);
        int maxHP = (defender == player1Unit ? player1MaxHPs : player2MaxHPs).ContainsKey(targetSlot)
            ? (defender == player1Unit ? player1MaxHPs : player2MaxHPs)[targetSlot] : 1;

        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(defender, targetSlot);

        ApplyDamage(defender, targetSlot, damage);
        int newHP = GetCurrentHP(defender, targetSlot);

        uiManager.LogMessage($"{attacker.Name} used {selectedAttack?.attackName ?? "Attack"} on {defender.Name}'s {targetSlot}.");
        if (damage > 0) uiManager.LogMessage($"It dealt {damage} damage ({damagePercent:F1}%).");

        ApplyEffect(attacker, defender, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(defender, targetSlot, newHP, player1MaxHPs, player2MaxHPs, player1Unit, player2Unit);

        if (newHP == 0 && prevHP > 0)
        {
            uiManager.LogMessage($"{defender.Name}'s {targetSlot} is no longer combat-ready!");
            ResetBuffStages(defender, targetSlot);
        }
        if (defender.matrixHP <= 0)
        {
            uiManager.LogMessage($"{defender.Name}'s Matrix was destroyed!\n{attacker.Name} wins!");
            currentState = (attacker == player1Unit) ? BattleState.Victory : BattleState.Defeat;
            EndBattle();
            return;
        }

        EndTurn();
    }

    private void EndTurn()
    {
        if (currentState == BattleState.Player1Turn)
            currentState = BattleState.Player2Turn;
        else if (currentState == BattleState.Player2Turn)
            currentState = BattleState.Player1Turn;

        StartTurn();
    }

    private void EndBattle()
    {
        uiManager.DisableAllButtons();
    }

    // ==================== DAMAGE & EFFECTS ====================

    private int GetCurrentHP(MechUnit unit, ModuleSlot slot)
    {
        if (unit == null) return 0;
        if (slot == ModuleSlot.Matrix) return unit.matrixHP;
        return unit.partStatuses.ContainsKey(slot) ? unit.partStatuses[slot].currentHP : 0;
    }

    private void ApplyDamage(MechUnit unit, ModuleSlot slot, int damage)
    {
        if (unit == null) return;
        if (slot == ModuleSlot.Matrix)
            unit.matrixHP = Mathf.Max(0, unit.matrixHP - damage);
        else if (unit.partStatuses.ContainsKey(slot))
            unit.partStatuses[slot].currentHP = Mathf.Max(0, unit.partStatuses[slot].currentHP - damage);
    }

    private void ApplyEffect(MechUnit attacker, MechUnit effectUnit, ModuleSlot effectSlot, AttackData attack)
    {
        if (attack == null || string.IsNullOrEmpty(attack.attackName))
        {
            uiManager.LogMessage("Effect: No additional effect.");
            return;
        }
        MechPart part = FindMechPartByAttack(attacker, attack);
        if (part == null || part.moves == null || part.moves.Count == 0)
        {
            uiManager.LogMessage("Effect: No additional effect.");
            return;
        }

        string effect = part.moves[0].effect;
        if (string.IsNullOrEmpty(effect))
        {
            uiManager.LogMessage("Effect: No additional effect.");
            return;
        }

        if (effect.StartsWith("+") || effect.StartsWith("-"))
        {
            bool isBuff = effect.StartsWith("+");
            string stat = effect.Substring(1).Trim().ToUpper();
            int delta = isBuff ? 1 : -1;
            ApplyBuffStage(effectUnit, effectSlot, stat, delta);
            string change = isBuff ? "increased" : "decreased";
            uiManager.LogMessage($"Effect: {effectUnit.Name}'s {effectSlot} {change} by 1 stage.");
        }
        else
        {
            uiManager.LogMessage("Effect: No additional effect.");
        }
    }

    private int CalculateDamage(AttackData attack, MechUnit attacker, MechUnit defender, ModuleSlot targetSlot, ModuleSlot sourceSlot)
    {
        if (attack == null || attacker == null || defender == null || attacker.chassis == null || defender.chassis == null) return 0;

        string atkStat = attack.type == "Physical" ? "ATK" : "ENG";
        string defStat = attack.type == "Physical" ? "DEF" : "SYS";

        float baseAtk = attack.type == "Physical" ? (attacker.chassis.baseStats?.ATK ?? 1) : (attacker.chassis.baseStats?.ENG ?? 1);
        float baseDef = attack.type == "Physical" ? (defender.chassis.baseStats?.DEF ?? 1) : (defender.chassis.baseStats?.SYS ?? 1);

        int atkStage = GetBuffStage(attacker, sourceSlot, atkStat);
        int defStage = GetBuffStage(defender, targetSlot, defStat);

        float atkMult = GetStageMultiplier(atkStage);
        float defMult = GetStageMultiplier(defStage);

        float effectiveAtk = baseAtk * atkMult;
        float effectiveDef = Mathf.Max(1f, baseDef * defMult);

        float multiplier = effectiveAtk / effectiveDef;
        float baseDamage = attack.damage;

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier));
    }

    // ==================== BUFFS ====================

    private MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
    {
        if (attack == null) return null;
        foreach (var kvp in unit.modules)
        {
            if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Key.ToString()}");
        }
        return null;
    }

    private float GetStageMultiplier(int stage)
    {
        stage = Mathf.Clamp(stage, -6, 6);
        float[] multipliers = { 0.25f, 0.2857f, 0.3333f, 0.4f, 0.5f, 0.6667f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };
        return multipliers[stage + 6];
    }

    private int GetBuffStage(MechUnit unit, ModuleSlot slot, string stat)
    {
        if (slot == ModuleSlot.Matrix)
        {
            if (matrixBuffsDict.ContainsKey(unit))
                return matrixBuffsDict[unit].GetValueOrDefault(stat, 0);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            return unit.partStatuses[slot].buffs.GetValueOrDefault(stat, 0);
        }
        return 0;
    }

    private void ApplyBuffStage(MechUnit unit, ModuleSlot slot, string stat, int delta)
    {
        int current = GetBuffStage(unit, slot, stat);
        int newStage = Mathf.Clamp(current + delta, -6, 6);
        if (slot == ModuleSlot.Matrix)
        {
            if (!matrixBuffsDict.ContainsKey(unit)) matrixBuffsDict[unit] = new Dictionary<string, int>();
            matrixBuffsDict[unit][stat] = newStage;
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            unit.partStatuses[slot].buffs[stat] = newStage;
        }
    }

    private void ResetBuffStages(MechUnit unit, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix)
        {
            if (matrixBuffsDict.ContainsKey(unit)) matrixBuffsDict[unit].Clear();
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            unit.partStatuses[slot].buffs.Clear();
        }
    }
}