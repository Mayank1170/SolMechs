using UnityEngine;
using System.Collections.Generic;
using MechBattle;

public class BattleManager : MonoBehaviour
{
    public MechUnit playerUnit;
    public MechUnit enemyUnit;
    private AttackData selectedAttack;
    private ModuleSlot selectedSourceSlot;
    private UIManager uiManager;
    public Dictionary<ModuleSlot, int> playerMaxHPs = new Dictionary<ModuleSlot, int>();
    public Dictionary<ModuleSlot, int> enemyMaxHPs = new Dictionary<ModuleSlot, int>();
    private enum BattleState { SelectingAttack, SelectingTarget, EnemyTurn, Victory, Defeat }
    private BattleState currentState = BattleState.SelectingAttack;
    private bool playerHasSelected = false;
    private AttackData playerSelectedAttack;
    private ModuleSlot playerSelectedSourceSlot;
    private ModuleSlot playerSelectedTargetSlot;
    private AttackData enemySelectedAttack;
    private ModuleSlot enemySelectedSourceSlot;
    private ModuleSlot enemySelectedTargetSlot;
    private int turnCounter = 0;

    public void Initialize(MechUnit player, MechUnit enemy, UIManager ui)
    {
        playerUnit = player;
        enemyUnit = enemy;
        uiManager = ui;
        InitializeMaxHPs();
    }

    private void InitializeMaxHPs()
    {
        if (playerUnit != null)
        {
            playerMaxHPs[ModuleSlot.Matrix] = playerUnit.matrixHP;
            foreach (var slot in playerUnit.partStatuses.Keys)
            {
                playerMaxHPs[slot] = playerUnit.partStatuses[slot].currentHP;
            }
        }
        if (enemyUnit != null)
        {
            enemyMaxHPs[ModuleSlot.Matrix] = enemyUnit.matrixHP;
            foreach (var slot in enemyUnit.partStatuses.Keys)
            {
                enemyMaxHPs[slot] = enemyUnit.partStatuses[slot].currentHP;
            }
        }
    }

    public void StartTurn()
    {
        playerHasSelected = false;
        turnCounter++;
        uiManager.LogMessage($"Turn {turnCounter}\n");
        uiManager.ScrollToBottom();

        // Enemy selects action simultaneously
        EnemySelectAction();

        // Player selects action
        currentState = BattleState.SelectingAttack;
        uiManager.RenderActionButtons(playerUnit, SelectAttack);
    }

    private void EnemySelectAction()
    {
        var validModules = new List<ModuleSlot>();
        foreach (var kvp in enemyUnit.modules)
            if (!enemyUnit.IsPartBroken(kvp.Key)) validModules.Add(kvp.Key);

        if (validModules.Count == 0) return;

        enemySelectedSourceSlot = validModules[Random.Range(0, validModules.Count)];
        enemySelectedAttack = enemyUnit.modules[enemySelectedSourceSlot].attack;
        enemySelectedTargetSlot = ChooseTargetSlot(playerUnit, enemySelectedAttack);
        if (enemySelectedTargetSlot == ModuleSlot.Matrix && !playerUnit.CanAttackMatrix())
            enemySelectedTargetSlot = ModuleSlot.LowerBody;
    }

    public void SelectAttack(ModuleSlot sourceSlot, AttackData attack)
    {
        if (playerUnit == null || playerUnit.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{sourceSlot} is broken!");
            return;
        }

        playerSelectedSourceSlot = sourceSlot;
        playerSelectedAttack = attack;
        MechPart part = FindMechPartByAttack(playerUnit, attack);
        if (part != null && part.moves[0].targetType == MechBattle.TargetType.Self)
        {
            playerSelectedTargetSlot = sourceSlot; // Self-target on source slot
            playerHasSelected = true;
            uiManager.DisableAllButtons(); // Prevent multiple clicks
            ResolveTurn();
        }
        else
        {
            currentState = BattleState.SelectingTarget;
            uiManager.RenderTargetButtons(enemyUnit, ConfirmTarget);
        }
    }

    public void ConfirmTarget(ModuleSlot targetSlot)
    {
        playerSelectedTargetSlot = targetSlot;
        playerHasSelected = true;
        uiManager.DisableAllButtons(); // Prevent multiple clicks
        ResolveTurn();
    }

    private void ResolveTurn()
    {
        int playerSpeed = CalculateSpeed(playerUnit);
        int enemySpeed = CalculateSpeed(enemyUnit);

        uiManager.LogMessage($"{playerUnit.Name} SPD: {playerSpeed}, {enemyUnit.Name} SPD: {enemySpeed}\n");
        uiManager.ScrollToBottom();

        MechUnit firstAttacker = playerSpeed >= enemySpeed ? playerUnit : enemyUnit;
        MechUnit secondAttacker = firstAttacker == playerUnit ? enemyUnit : playerUnit;

        // Resolve first attacker
        if (firstAttacker == playerUnit)
        {
            ProcessAttack(playerUnit, enemyUnit, playerSelectedSourceSlot, playerSelectedAttack, playerSelectedTargetSlot);
        }
        else
        {
            ProcessAttack(enemyUnit, playerUnit, enemySelectedSourceSlot, enemySelectedAttack, enemySelectedTargetSlot);
        }

        if (currentState == BattleState.Victory || currentState == BattleState.Defeat) return;

        // Resolve second attacker
        if (secondAttacker == playerUnit)
        {
            ProcessAttack(playerUnit, enemyUnit, playerSelectedSourceSlot, playerSelectedAttack, playerSelectedTargetSlot);
        }
        else
        {
            ProcessAttack(enemyUnit, playerUnit, enemySelectedSourceSlot, enemySelectedAttack, enemySelectedTargetSlot);
        }

        if (currentState == BattleState.Victory || currentState == BattleState.Defeat) return;

        uiManager.LogMessage("\n");
        uiManager.ScrollToBottom();
        StartTurn();
    }

    private int CalculateSpeed(MechUnit unit)
    {
        int speed = unit.chassis.baseStats.SPD;
        foreach (var slot in unit.modules.Keys)
        {
            if (!unit.IsPartBroken(slot))
                speed += unit.modules[slot].SPD;
        }
        return speed;
    }

    private void ProcessAttack(MechUnit attacker, MechUnit defender, ModuleSlot sourceSlot, AttackData attack, ModuleSlot targetSlot)
    {
        int damage = CalculateDamage(attack, attacker, defender, targetSlot);
        int maxHP = GetMaxHP(defender, targetSlot);
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(defender, targetSlot);
        ApplyDamage(defender, targetSlot, damage);
        int newHP = GetCurrentHP(defender, targetSlot);
        ApplyEffect(attacker, targetSlot, attack, true);
        uiManager.UpdateHealthBars(defender, targetSlot, newHP, playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);

        string attackerName = attacker == playerUnit ? playerUnit.Name : enemyUnit.Name;
        string defenderName = defender == playerUnit ? playerUnit.Name : enemyUnit.Name;
        uiManager.LogMessage($"{attackerName} used {attack.attackName} on {defenderName}'s {targetSlot}.\n");
        if (damage > 0) uiManager.LogMessage($"It dealt {damage} damage ({damagePercent:F1}%).\n");
        if (newHP == 0 && prevHP > 0) uiManager.LogMessage($"{defenderName}'s {targetSlot} is no longer combat-ready!\n");
        uiManager.ScrollToBottom();

        if (defender.matrixHP <= 0)
        {
            uiManager.LogMessage($"\n{defenderName}'s Matrix was destroyed!\n{attackerName} wins!\n");
            currentState = BattleState.Victory;
            EndBattle();
        }
    }

    private int GetMaxHP(MechUnit unit, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix) return unit.matrixHP;
        return unit.partStatuses.ContainsKey(slot) ? unit.partStatuses[slot].maxHP : 1;
    }

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

    private void ApplyEffect(MechUnit attacker, ModuleSlot targetSlot, AttackData attack, bool isAttackerEffect)
    {
        if (attack == null || string.IsNullOrEmpty(attack.attackName)) return;
        MechPart part = FindMechPartByAttack(attacker, attack);
        if (part == null || part.moves == null || part.moves.Count == 0) return;

        string effect = part.moves[0].effect;
        if (!string.IsNullOrEmpty(effect))
        {
            if (effect.StartsWith("+") || effect.StartsWith("-"))
            {
                string[] effectParts = effect.Split(new[] { ' ' }, 2);
                if (effectParts.Length == 2)
                {
                    string valueStr = effectParts[0].Replace("+", "").Replace("-", "");
                    int value = int.Parse(valueStr);
                    string stat = effectParts[1].Split(' ')[0].ToUpper();
                    bool isBuff = effect.StartsWith("+");

                    MechBattle.TargetType moveTargetType = part.moves[0].targetType;
                    ModuleSlot effectTarget = (moveTargetType == MechBattle.TargetType.Self) ? selectedSourceSlot : targetSlot;

                    if (attacker.partStatuses.ContainsKey(effectTarget))
                    {
                        var status = attacker.partStatuses[effectTarget];
                        status.buffs[stat] = new PartStatus.Buff { value = isBuff ? value : -value, duration = 3 }; // Default 3 turns
                        string change = isBuff ? "rose" : "fell";
                        uiManager.LogMessage($"{attacker.Name}'s {effectTarget} {stat} {change} by {value} (3 turns)!");
                    }
                }
            }
        }
    }

    private int CalculateDamage(AttackData attack, MechUnit attacker, MechUnit defender, ModuleSlot targetSlot)
    {
        if (attack == null || attacker == null || defender == null || attacker.chassis == null || defender.chassis == null) return 0;
        float baseDamage = attack.damage;
        float multiplier = attack.type == "Physical"
            ? (attacker.chassis.baseStats?.ATK ?? 1) / Mathf.Max(1f, defender.chassis.baseStats?.DEF ?? 1)
            : (attacker.chassis.baseStats?.ENG ?? 1) / Mathf.Max(1f, defender.chassis.baseStats?.SYS ?? 1);

        if (attacker.partStatuses.ContainsKey(ModuleSlot.LowerBody))
        {
            var lowerStatus = attacker.partStatuses[ModuleSlot.LowerBody];
            if (lowerStatus.buffs.ContainsKey("ATK")) baseDamage += lowerStatus.buffs["ATK"].value;
        }
        if (defender.partStatuses.ContainsKey(targetSlot))
        {
            var targetStatus = defender.partStatuses[targetSlot];
            if (targetStatus.buffs.ContainsKey("DEF")) multiplier -= (targetStatus.buffs["DEF"].value * 0.01f);
        }

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier));
    }

    private ModuleSlot ChooseTargetSlot(MechUnit unit, AttackData attack)
    {
        if (unit == null) return ModuleSlot.LowerBody;
        var options = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (unit.CanAttackMatrix()) options.Add(ModuleSlot.Matrix);
        return options[Random.Range(0, options.Count)];
    }

    private MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
    {
        foreach (var kvp in unit.modules)
        {
            if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Key.ToString()}");
        }
        return null;
    }

    private void DecrementBuffDurations(MechUnit unit)
    {
        if (unit == null) return;
        foreach (var slot in unit.partStatuses.Keys)
        {
            var status = unit.partStatuses[slot];
            var expired = new List<string>();
            foreach (var kvp in status.buffs)
            {
                var buff = kvp.Value;
                buff.duration--;
                status.buffs[kvp.Key] = buff;
                if (buff.duration <= 0)
                {
                    expired.Add(kvp.Key);
                    uiManager.LogMessage($"{unit.Name}'s {slot} {kvp.Key} effect expired!");
                }
            }
            foreach (var key in expired)
            {
                status.buffs.Remove(key);
            }
        }
    }

    private void EndBattle()
    {
        uiManager.DisableAllButtons();
    }
}