using UnityEngine;
using System.Collections.Generic;
using MechBattle;
using System;

public class BattleManager : MonoBehaviour
{
    // NEW: Loaders and optional UI reference
    public MechUnitLoader playerLoader;
    public MechUnitLoader enemyLoader;
    public UIManager ui; // optional; if null we'll FindObjectOfType

    public MechUnit playerUnit;
    public MechUnit enemyUnit;
    private AttackData selectedAttack;
    private ModuleSlot selectedSourceSlot;
    private UIManager uiManager;
    public Dictionary<ModuleSlot, int> playerMaxHPs = new Dictionary<ModuleSlot, int>();
    public Dictionary<ModuleSlot, int> enemyMaxHPs = new Dictionary<ModuleSlot, int>();
    private Dictionary<MechUnit, Dictionary<string, int>> matrixBuffsDict = new Dictionary<MechUnit, Dictionary<string, int>>();
    private enum BattleState { SelectingAttack, SelectingTarget, SelectingSelfTarget, EnemyTurn, Victory, Defeat }
    private BattleState currentState;

    // Bootstrap
    private void Start()
    {
        uiManager = uiManager != null ? uiManager : (ui != null ? ui : FindObjectOfType<UIManager>());

        if (playerLoader != null) playerUnit = playerLoader.GetUnitData();
        if (enemyLoader != null) enemyUnit = enemyLoader.GetUnitData();

        if (playerUnit == null || enemyUnit == null || uiManager == null)
        {
            Debug.LogError("[BattleManager] Missing units or UIManager. Check loaders/UI references.");
            return;
        }

        Initialize(playerUnit, enemyUnit, uiManager);
        uiManager.DisableSliderInteractabilityAndHandles();
        uiManager.InitializeHealthBars(playerUnit, enemyUnit, playerMaxHPs, enemyMaxHPs);

        // set the sprites for both sides from the loaders
        uiManager.InitializePaperDolls(playerLoader, enemyLoader);

        // IMPORTANT: não chamar HideBattleResult() aqui — o GO está desativado por padrão

        StartTurn();
    }

    public void Initialize(MechUnit player, MechUnit enemy, UIManager uiM)
    {
        playerUnit = player;
        enemyUnit = enemy;
        uiManager = uiM;
        matrixBuffsDict[playerUnit] = new Dictionary<string, int>();
        matrixBuffsDict[enemyUnit] = new Dictionary<string, int>();
        InitializeMaxHPs();
        currentState = BattleState.SelectingAttack;
    }

    private void InitializeMaxHPs()
    {
        if (playerUnit != null)
        {
            playerMaxHPs[ModuleSlot.Matrix] = playerUnit.matrixHP;
            foreach (var slot in playerUnit.partStatuses.Keys)
                playerMaxHPs[slot] = playerUnit.partStatuses[slot].currentHP;
        }
        if (enemyUnit != null)
        {
            enemyMaxHPs[ModuleSlot.Matrix] = enemyUnit.matrixHP;
            foreach (var slot in enemyUnit.partStatuses.Keys)
                enemyMaxHPs[slot] = enemyUnit.partStatuses[slot].currentHP;
        }
    }

    public void StartTurn()
    {
        // NOVO: se o jogador não pode atacar (3 partes quebradas / sem módulos utilizáveis) → derrota imediata
        if (!HasAnyUsableModule(playerUnit) || AllNonMatrixPartsBroken(playerUnit))
        {
            uiManager.LogMessage($"{playerUnit.Name} can no longer fight!\n{enemyUnit.Name} wins!");
            currentState = BattleState.Defeat;
            EndBattle(false);
            return;
        }

        currentState = BattleState.SelectingAttack;
        uiManager.RenderActionButtons(playerUnit, SelectAttack);
    }

    public void SelectAttack(ModuleSlot sourceSlot, AttackData attack)
    {
        if (currentState != BattleState.SelectingAttack) return;

        if (playerUnit == null || playerUnit.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{sourceSlot} is broken!");
            return;
        }

        selectedAttack = attack;
        selectedSourceSlot = sourceSlot;
        bool isSelfTarget = attack.target == MechBattle.TargetType.Self;

        if (isSelfTarget)
        {
            currentState = BattleState.SelectingSelfTarget;
            uiManager.RenderSelfTargetButtons(playerUnit, ConfirmSelfTarget);
        }
        else
        {
            currentState = BattleState.SelectingTarget;
            uiManager.RenderTargetButtons(enemyUnit, ConfirmTarget);
        }
    }

    public void ConfirmSelfTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingSelfTarget) return;

        if (targetSlot != ModuleSlot.Matrix && playerUnit.IsPartBroken(targetSlot))
        {
            uiManager.LogMessage($"{targetSlot} is broken!");
            return;
        }

        uiManager.LogMessage($"{playerUnit.Name} used {selectedAttack?.attackName ?? "Attack"} on itself ({targetSlot}).");
        ApplyEffect(playerUnit, playerUnit, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(playerUnit, targetSlot, GetCurrentHP(playerUnit, targetSlot), playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);
        currentState = BattleState.EnemyTurn;
        TriggerEnemyTurn();
    }

    public void ConfirmTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingTarget) return;

        if (targetSlot == ModuleSlot.Matrix && !enemyUnit.CanAttackMatrix())
        {
            uiManager.LogMessage("Matrix locked!");
            return;
        }

        int damage = CalculateDamage(selectedAttack, playerUnit, enemyUnit, targetSlot, selectedSourceSlot);
        int maxHP = enemyMaxHPs.ContainsKey(targetSlot) ? enemyMaxHPs[targetSlot] : 1;
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(enemyUnit, targetSlot);
        ApplyDamage(enemyUnit, targetSlot, damage);
        int newHP = GetCurrentHP(enemyUnit, targetSlot);
        uiManager.LogMessage($"{playerUnit.Name} used {selectedAttack?.attackName ?? "Attack"} on {enemyUnit.Name}'s {targetSlot}.");
        if (damage > 0) uiManager.LogMessage($"It dealt {damage} damage ({damagePercent:F1}%).");
        ApplyEffect(playerUnit, enemyUnit, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(enemyUnit, targetSlot, newHP, playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);

        if (newHP == 0 && prevHP > 0)
        {
            uiManager.LogMessage($"{enemyUnit.Name}'s {targetSlot} is no longer combat-ready!");
            ResetBuffStages(enemyUnit, targetSlot);
        }

        // Condição 1: Matrix destruída
        if (enemyUnit.matrixHP <= 0)
        {
            uiManager.LogMessage($"{enemyUnit.Name}'s Matrix was destroyed!\n{playerUnit.Name} wins!");
            currentState = BattleState.Victory;
            EndBattle(true);
            return;
        }

        // NOVO – Condição 2: 3 partes destruídas / não consegue mais atacar
        if (AllNonMatrixPartsBroken(enemyUnit) || !HasAnyUsableModule(enemyUnit))
        {
            uiManager.LogMessage($"{enemyUnit.Name} can no longer fight!\n{playerUnit.Name} wins!");
            currentState = BattleState.Victory;
            EndBattle(true);
            return;
        }

        currentState = BattleState.EnemyTurn;
        TriggerEnemyTurn();
    }

    public void EnemyTurn()
    {
        if (currentState != BattleState.EnemyTurn) return;
        if (enemyUnit == null) return;

        // Se o inimigo não pode atacar, vitória do player
        var validModules = new List<ModuleSlot>();
        foreach (var kvp in enemyUnit.modules)
            if (!enemyUnit.IsPartBroken(kvp.Key)) validModules.Add(kvp.Key);

        if (validModules.Count == 0)
        {
            uiManager.LogMessage($"{enemyUnit.Name} can no longer fight!\n{playerUnit.Name} wins!");
            currentState = BattleState.Victory;
            EndBattle(true);
            return;
        }

        var chosenSlot = validModules[UnityEngine.Random.Range(0, validModules.Count)];
        var attack = enemyUnit.modules[chosenSlot].attack;
        bool isSelfTarget = attack.target == MechBattle.TargetType.Self;

        if (isSelfTarget)
        {
            var selfOptions = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody, ModuleSlot.Matrix };
            selfOptions.RemoveAll(s => s != ModuleSlot.Matrix && enemyUnit.IsPartBroken(s));
            if (selfOptions.Count == 0)
            {
                uiManager.LogMessage("\n");
                currentState = BattleState.SelectingAttack;
                uiManager.RenderActionButtons(playerUnit, SelectAttack);
                return;
            }
            ModuleSlot selfTarget = selfOptions[UnityEngine.Random.Range(0, selfOptions.Count)];
            uiManager.LogMessage($"{enemyUnit.Name} used {attack.attackName} on itself ({selfTarget}).");
            ApplyEffect(enemyUnit, enemyUnit, selfTarget, attack);
            uiManager.UpdateHealthBars(enemyUnit, selfTarget, GetCurrentHP(enemyUnit, selfTarget), playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);
        }
        else
        {
            var target = ChooseTargetSlot(playerUnit, attack);
            if (target == ModuleSlot.Matrix && !playerUnit.CanAttackMatrix())
                target = ModuleSlot.LowerBody;

            int damage = CalculateDamage(attack, enemyUnit, playerUnit, target, chosenSlot);
            int maxHP = playerMaxHPs.ContainsKey(target) ? playerMaxHPs[target] : 1;
            float damagePercent = (damage / (float)maxHP) * 100f;
            int prevHP = GetCurrentHP(playerUnit, target);
            ApplyDamage(playerUnit, target, damage);
            int newHP = GetCurrentHP(playerUnit, target);
            uiManager.LogMessage($"{enemyUnit.Name} used {attack.attackName} on {playerUnit.Name}'s {target}.");
            if (damage > 0) uiManager.LogMessage($"It dealt {damage} damage ({damagePercent:F1}%).");
            ApplyEffect(enemyUnit, playerUnit, target, attack);
            uiManager.UpdateHealthBars(playerUnit, target, newHP, playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);

            if (newHP == 0 && prevHP > 0)
            {
                uiManager.LogMessage($"{playerUnit.Name}'s {target} is no longer combat-ready!");
                ResetBuffStages(playerUnit, target);
            }

            // Condição 1: Matrix do player destruída
            if (playerUnit.matrixHP <= 0)
            {
                uiManager.LogMessage($"{playerUnit.Name}'s Matrix was destroyed!\n{enemyUnit.Name} wins!");
                currentState = BattleState.Defeat;
                EndBattle(false);
                return;
            }

            // NOVO – Condição 2: 3 partes do player destruídas / sem módulos
            if (AllNonMatrixPartsBroken(playerUnit) || !HasAnyUsableModule(playerUnit))
            {
                uiManager.LogMessage($"{playerUnit.Name} can no longer fight!\n{enemyUnit.Name} wins!");
                currentState = BattleState.Defeat;
                EndBattle(false);
                return;
            }
        }

        uiManager.LogMessage("\n");
        currentState = BattleState.SelectingAttack;
        uiManager.RenderActionButtons(playerUnit, SelectAttack);
    }

    private void TriggerEnemyTurn() { Invoke(nameof(EnemyTurn), 1f); }

    // UPDATED: show result panel
    private void EndBattle(bool playerWon)
    {
        uiManager.DisableAllButtons();
        uiManager.ShowBattleResult(playerWon, 0);
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
        if (slot == ModuleSlot.Matrix) unit.matrixHP = Mathf.Max(0, unit.matrixHP - damage);
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
        MechBattle.MoveDefinition selectedMove = null;

        if (part != null && part.moves != null && part.moves.Count > 0)
        {
            selectedMove = part.moves.Find(m => m.moveName == attack.attackName);
            if (selectedMove == null && part.moves.Count == 1)
                selectedMove = part.moves[0];
        }

        string effect = selectedMove != null && !string.IsNullOrEmpty(selectedMove.effect)
            ? selectedMove.effect
            : attack.effect;

        if (string.IsNullOrEmpty(effect))
        {
            uiManager.LogMessage("Effect: No additional effect.");
            return;
        }

        List<string> effects = new List<string>(effect.Split(';'));
        foreach (string eff in effects)
            ApplySingleEffect(effectUnit, effectSlot, eff.Trim());
    }

    private void ApplySingleEffect(MechUnit unit, ModuleSlot slot, string effect, string casterName = "")
    {
        if (string.IsNullOrWhiteSpace(effect)) return;
        effect = effect.Trim();

        if (effect.StartsWith("+") || effect.StartsWith("-"))
        {
            bool isBuff = effect.StartsWith("+");
            string[] parts = effect.Substring(1).Trim().Split(' ');
            int amount = 1; string stat;

            if (parts.Length == 1) stat = parts[0].ToUpper();
            else if (parts.Length > 1 && int.TryParse(parts[0], out int parsed)) { amount = parsed; stat = parts[1].ToUpper(); }
            else return;

            int delta = isBuff ? amount : -amount;
            ApplyBuffStage(unit, slot, stat, delta);

            int finalStage = GetBuffStage(unit, slot, stat);
            string direction = finalStage > 0 ? "increased" : (finalStage < 0 ? "decreased" : "reset");
            uiManager.LogMessage($"{unit.Name}'s {slot} {stat} {direction} to stage {finalStage}.");
            return;
        }

        if (effect.ToLower().Contains("piercing"))
        {
            uiManager.LogMessage("Effect: Piercing – halves defense this turn.");
            return;
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

        MechPart part = FindMechPartByAttack(attacker, attack);
        if (part != null && part.moves != null && part.moves.Count > 0)
        {
            MechBattle.MoveDefinition selectedMove = part.moves.Find(m => m.moveName == attack.attackName);
            if (selectedMove == null) selectedMove = part.moves[0];
            if (selectedMove != null && selectedMove.effect.ToLower().Contains("piercing"))
                effectiveDef *= 0.5f;
        }

        float multiplier = effectiveAtk / effectiveDef;
        float baseDamage = attack.damage;

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier));
    }

    private ModuleSlot ChooseTargetSlot(MechUnit unit, AttackData attack)
    {
        if (unit == null) return ModuleSlot.LowerBody;
        var options = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (unit.CanAttackMatrix()) options.Add(ModuleSlot.Matrix);
        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    private MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
    {
        if (attack == null) return null;
        foreach (var kvp in unit.modules)
        {
            if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Value.partCode}");
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

    // -------- Helpers de condição de vitória/derrota --------
    private bool AllNonMatrixPartsBroken(MechUnit unit)
    {
        if (unit == null) return false;
        return unit.IsPartBroken(ModuleSlot.RightArm)
            && unit.IsPartBroken(ModuleSlot.LeftArm)
            && unit.IsPartBroken(ModuleSlot.LowerBody);
    }

    private bool HasAnyUsableModule(MechUnit unit)
    {
        if (unit == null || unit.modules == null) return false;
        foreach (var kvp in unit.modules)
        {
            // Consider only non-matrix module slots and check if part is not broken
            if (kvp.Key != ModuleSlot.Matrix && !unit.IsPartBroken(kvp.Key))
                return true;
        }
        return false;
    }
}
