using UnityEngine;
using System.Collections; // needed for IEnumerator/Coroutines
using System.Collections.Generic;
using MechBattle;
using System;

public class BattleManager : MonoBehaviour
{
    // === Loaders and optional UI reference ===
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

    // Guard to avoid double endings or late invokes
    private bool battleOver = false;

    // === NEW: Allow external scripts (like FanSwarmRules) to override victory conditions ===
    [HideInInspector] public bool skipDefaultVictoryCheck = false;

    // === TIMER ===
    [SerializeField] private BattleTurnTimer turnTimer;  // assign in Inspector

    // === FX pacing (minimal delays) ===
    [Header("FX Pacing")]
    [SerializeField] private float attackFxDuration = 0.80f; // wait this long after PlayAttackFx BEFORE applying damage
    [SerializeField] private float fxDestroyDelay = 0.12f;   // tiny extra wait after destruction visual

    // === Tower Integration (NEW) ===
    [Header("Tower Integration")]
    [Tooltip("Optional: Tower progression handler. If not set, will be auto-found at runtime.")]
    [SerializeField] private TowerProgressionHandler towerProgressionHandler;

    // Bootstrap
    private void Start()
    {
        uiManager = uiManager != null ? uiManager : (ui != null ? ui : FindObjectOfType<UIManager>());
        if (towerProgressionHandler == null) towerProgressionHandler = FindObjectOfType<TowerProgressionHandler>();

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

        // Set the sprites for both sides from the loaders
        uiManager.InitializePaperDolls(playerLoader, enemyLoader);

        // TIMER wire-up
        if (turnTimer)
        {
            turnTimer.Initialize();
            turnTimer.onPlayerFlagFall.AddListener(OnPlayerTimeout);
            turnTimer.onEnemyFlagFall.AddListener(OnEnemyTimeout);
        }

        // Player starts deciding → timer runs during decision
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

    // ================== StartTurn (player selects action) ==================
    public void StartTurn()
    {
        if (battleOver) return;

        // if player has no usable modules (3 broken parts / no actions) → immediate defeat
        if (!HasAnyUsableModule(playerUnit) || AllNonMatrixPartsBroken(playerUnit))
        {
            uiManager.LogMessage($"{playerUnit.Name} can no longer fight!\n{enemyUnit.Name} wins!");
            currentState = BattleState.Defeat;
            EndBattle(false);
            return;
        }

        currentState = BattleState.SelectingAttack;

        // TIMER: player's clock ticks while DECIDING
        if (turnTimer) turnTimer.BeginTurn(attackerIsPlayer: true);

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

        // Show Back button when entering target selection
        if (isSelfTarget)
        {
            currentState = BattleState.SelectingSelfTarget;
            uiManager.RenderSelfTargetButtons(playerUnit, ConfirmSelfTarget);
            uiManager.ShowBackButton(OnBackFromTargetSelection);
        }
        else
        {
            currentState = BattleState.SelectingTarget;
            uiManager.RenderTargetButtons(enemyUnit, ConfirmTarget);
            uiManager.ShowBackButton(OnBackFromTargetSelection);
        }
    }

    // Back button during target selection
    private void OnBackFromTargetSelection()
    {
        selectedAttack = null;
        selectedSourceSlot = ModuleSlot.Matrix; // default/unused
        currentState = BattleState.SelectingAttack;

        uiManager.HideBackButton();
        uiManager.RenderActionButtons(playerUnit, SelectAttack);
        uiManager.LogMessage("(Selection cancelled)");
    }

    public void ConfirmSelfTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingSelfTarget) return;

        uiManager.HideBackButton(); // avoid double clicks
        StartCoroutine(ConfirmSelfTargetFlow(targetSlot));
    }

    private IEnumerator ConfirmSelfTargetFlow(ModuleSlot targetSlot)
    {
        // guard: if source part got destroyed before resolution, cancel the move
        if (playerUnit.IsPartBroken(selectedSourceSlot))
        {
            uiManager.LogMessage($"{playerUnit.Name}'s {selectedSourceSlot} was destroyed and cannot act!");
            if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn(); // safety
            currentState = BattleState.EnemyTurn;
            TriggerEnemyTurn();
            yield break;
        }

        if (targetSlot != ModuleSlot.Matrix && playerUnit.IsPartBroken(targetSlot))
        {
            uiManager.LogMessage($"{targetSlot} is broken!");
            yield break;
        }

        // Stop the clock as soon as the decision is locked
        if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

        uiManager.LogMessage($"{playerUnit.Name} used {selectedAttack?.attackName ?? "Attack"} on itself ({targetSlot}).");

        // Play attack FX first
        if (selectedAttack != null)
            uiManager.PlayAttackFx(true, true, targetSlot, selectedAttack.attackName);

        // wait FX (timer already stopped)
        yield return WaitFx(attackFxDuration);

        ApplyEffect(playerUnit, playerUnit, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(playerUnit, targetSlot, GetCurrentHP(playerUnit, targetSlot), playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);

        currentState = BattleState.EnemyTurn;
        TriggerEnemyTurn();
    }

    public void ConfirmTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingTarget) return;

        uiManager.HideBackButton(); // avoid double clicks
        StartCoroutine(ConfirmTargetFlow(targetSlot));
    }

    private IEnumerator ConfirmTargetFlow(ModuleSlot targetSlot)
    {
        // guard: if source part got destroyed before resolution, cancel the move
        if (playerUnit.IsPartBroken(selectedSourceSlot))
        {
            uiManager.LogMessage($"{playerUnit.Name}'s {selectedSourceSlot} was destroyed and cannot act!");
            if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn(); // safety
            currentState = BattleState.EnemyTurn;
            TriggerEnemyTurn();
            yield break;
        }

        // === MODIFIED: Skip Matrix lock check for DroneSwarmUnit ===
        bool isDroneSwarm = enemyUnit is DroneSwarmUnit;

        if (!isDroneSwarm && targetSlot == ModuleSlot.Matrix && !enemyUnit.CanAttackMatrix())
        {
            uiManager.LogMessage("Matrix locked!");
            yield break;
        }

        // Stop the player's clock as soon as the move is locked
        if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

        // 1) Play attack FX on target
        if (selectedAttack != null)
            uiManager.PlayAttackFx(true, false, targetSlot, selectedAttack.attackName);

        // 2) wait so we don't overlap explosion with the attack FX
        yield return WaitFx(attackFxDuration);

        // 3) Resolve damage and update UI
        int damage = CalculateDamage(selectedAttack, playerUnit, enemyUnit, targetSlot, selectedSourceSlot);
        int maxHP = enemyMaxHPs.ContainsKey(targetSlot) ? enemyMaxHPs[targetSlot] : 1;
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(enemyUnit, targetSlot);
        ApplyDamage(enemyUnit, targetSlot, damage);
        int newHP = GetCurrentHP(enemyUnit, targetSlot);

        // === MODIFIED: Custom message for drones ===
        string targetName = isDroneSwarm ? GetDroneNameForSlot(targetSlot) : $"{enemyUnit.Name}'s {targetSlot}";
        uiManager.LogMessage($"{playerUnit.Name} used {selectedAttack?.attackName ?? "Attack"} on {targetName}.");
        if (damage > 0) uiManager.LogMessage($"It dealt {damage} damage ({damagePercent:F1}%).");

        ApplyEffect(playerUnit, enemyUnit, targetSlot, selectedAttack);
        uiManager.UpdateHealthBars(enemyUnit, targetSlot, newHP, playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);

        // 4) If destroyed, play explosion AFTER attack FX finished
        if (newHP == 0 && prevHP > 0)
        {
            string destroyedName = isDroneSwarm ? GetDroneNameForSlot(targetSlot) : $"{enemyUnit.Name}'s {targetSlot}";
            uiManager.LogMessage($"{destroyedName} is no longer combat-ready!");
            ResetBuffStages(enemyUnit, targetSlot);
            yield return WaitFx(0.05f); // micro-gap
            HandlePartDestroyed(enemyUnit, /*isPlayer*/ false, targetSlot);
            yield return WaitFx(fxDestroyDelay); // readability
        }

        // === MODIFIED: Win conditions (with skipDefaultVictoryCheck support) ===
        if (!skipDefaultVictoryCheck)
        {
            if (enemyUnit.matrixHP <= 0)
            {
                uiManager.LogMessage($"{enemyUnit.Name}'s Matrix was destroyed!\n{playerUnit.Name} wins!");
                currentState = BattleState.Victory;
                EndBattle(true);
                yield break;
            }

            if (AllNonMatrixPartsBroken(enemyUnit) || !HasAnyUsableModule(enemyUnit))
            {
                uiManager.LogMessage($"{enemyUnit.Name} can no longer fight!\n{playerUnit.Name} wins!");
                currentState = BattleState.Victory;
                EndBattle(true);
                yield break;
            }
        }

        currentState = BattleState.EnemyTurn;
        TriggerEnemyTurn();
    }

    // === NEW: Helper to get drone name from slot ===
    private string GetDroneNameForSlot(ModuleSlot slot)
    {
        // Try to find DroneSwarmLoader to get actual drone names
        var droneLoader = GameObject.Find("EnemyMech")?.GetComponent<DroneSwarmLoader>();
        if (droneLoader != null)
        {
            Drone drone = null;
            switch (slot)
            {
                case ModuleSlot.RightArm: drone = droneLoader.ResolvedSlot0; break;
                case ModuleSlot.LeftArm: drone = droneLoader.ResolvedSlot1; break;
                case ModuleSlot.LowerBody: drone = droneLoader.ResolvedSlot2; break;
                case ModuleSlot.Matrix: drone = droneLoader.ResolvedSlot3; break;
            }
            if (drone != null) return drone.droneName;
        }

        // Fallback to generic names
        switch (slot)
        {
            case ModuleSlot.RightArm: return "Drone 1";
            case ModuleSlot.LeftArm: return "Drone 2";
            case ModuleSlot.LowerBody: return "Drone 3";
            case ModuleSlot.Matrix: return "Drone 4";
            default: return "Drone";
        }
    }

    // ================== Enemy flow (stop enemy clock before FX) ==================
    public void EnemyTurn()
    {
        if (battleOver) return;
        if (currentState != BattleState.EnemyTurn) return;
        if (enemyUnit == null) return;

        uiManager.HideBackButton(); // safety
        StartCoroutine(EnemyTurnFlow());
    }

    private IEnumerator EnemyTurnFlow()
    {
        // AI DECISION phase: start enemy clock
        if (turnTimer) turnTimer.BeginTurn(attackerIsPlayer: false);

        // If enemy cannot act, player wins
        var validModules = new List<ModuleSlot>();
        foreach (var kvp in enemyUnit.modules)
            if (!enemyUnit.IsPartBroken(kvp.Key)) validModules.Add(kvp.Key);

        if (validModules.Count == 0)
        {
            if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();
            uiManager.LogMessage($"{enemyUnit.Name} can no longer fight!\n{playerUnit.Name} wins!");
            currentState = BattleState.Victory;
            EndBattle(true);
            yield break;
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
                if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();
                uiManager.LogMessage("\n");
                // >>> Return to player's decision (and start his clock)
                StartTurn();
                yield break;
            }

            // guard: cancel if source part got destroyed before executing FX
            if (enemyUnit.IsPartBroken(chosenSlot))
            {
                if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();
                uiManager.LogMessage($"{enemyUnit.Name}'s {chosenSlot} was destroyed and cannot act!");
                uiManager.LogMessage("\n");
                // >>> Back to player's decision
                StartTurn();
                yield break;
            }

            // Enemy locks move → stop enemy clock
            if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

            ModuleSlot selfTarget = selfOptions[UnityEngine.Random.Range(0, selfOptions.Count)];
            uiManager.LogMessage($"{enemyUnit.Name} used {attack.attackName} on itself ({selfTarget}).");

            uiManager.PlayAttackFx(false, false, selfTarget, attack.attackName);
            yield return WaitFx(attackFxDuration);

            ApplyEffect(enemyUnit, enemyUnit, selfTarget, attack);
            uiManager.UpdateHealthBars(enemyUnit, selfTarget, GetCurrentHP(enemyUnit, selfTarget), playerMaxHPs, enemyMaxHPs, playerUnit, enemyUnit);
        }
        else
        {
            var target = ChooseTargetSlot(playerUnit, attack);
            if (target == ModuleSlot.Matrix && !playerUnit.CanAttackMatrix())
                target = ModuleSlot.LowerBody;

            if (enemyUnit.IsPartBroken(chosenSlot))
            {
                if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();
                uiManager.LogMessage($"{enemyUnit.Name}'s {chosenSlot} was destroyed and cannot act!");
                uiManager.LogMessage("\n");
                // >>> Back to player's decision
                StartTurn();
                yield break;
            }

            // Enemy locks move → stop enemy clock
            if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

            uiManager.PlayAttackFx(false, true, target, attack.attackName);
            yield return WaitFx(attackFxDuration);

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

                yield return WaitFx(0.05f);
                HandlePartDestroyed(playerUnit, /*isPlayer*/ true, target);
                yield return WaitFx(fxDestroyDelay);
            }

            // === MODIFIED: Defeat conditions (with skipDefaultVictoryCheck support) ===
            if (!skipDefaultVictoryCheck)
            {
                if (playerUnit.matrixHP <= 0)
                {
                    uiManager.LogMessage($"{playerUnit.Name}'s Matrix was destroyed!\n{enemyUnit.Name} wins!");
                    currentState = BattleState.Defeat;
                    EndBattle(false);
                    yield break;
                }

                if (AllNonMatrixPartsBroken(playerUnit) || !HasAnyUsableModule(playerUnit))
                {
                    uiManager.LogMessage($"{playerUnit.Name} can no longer fight!\n{enemyUnit.Name} wins!");
                    currentState = BattleState.Defeat;
                    EndBattle(false);
                    yield break;
                }
            }
        }

        uiManager.LogMessage("\n");

        // >>> Always return via StartTurn so the player's clock starts
        StartTurn();
    }

    private void TriggerEnemyTurn()
    {
        if (battleOver) return;
        Invoke(nameof(EnemyTurn), 1f);
    }

    // UPDATED: show result panel + guard
    private void EndBattle(bool playerWon)
    {
        if (battleOver) return;
        battleOver = true;

        CancelInvoke(nameof(EnemyTurn));

        if (turnTimer)
        {
            turnTimer.SetPaused(true);
            turnTimer.enabled = false;
            turnTimer.onPlayerFlagFall.RemoveListener(OnPlayerTimeout);
            turnTimer.onEnemyFlagFall.RemoveListener(OnEnemyTimeout);
        }

        uiManager.HideBackButton(); // safety
        uiManager.DisableAllButtons();
        uiManager.ShowBattleResult(playerWon, 0);

        // === NEW === ensure Tower gets notified (next frame) so it can override the overlay
        if (towerProgressionHandler == null) towerProgressionHandler = FindObjectOfType<TowerProgressionHandler>();
        if (towerProgressionHandler != null)
            StartCoroutine(NotifyTowerAfterOverlay(playerWon));
    }

    // === NEW: let UI overlay appear for one frame, then hand control to Tower ===
    private IEnumerator NotifyTowerAfterOverlay(bool playerWon)
    {
        yield return null; // wait one frame
        towerProgressionHandler.OnBattleEnd(playerWon);
    }

    // === TIMER: timeouts -> instant end ===
    public void OnPlayerTimeout()
    {
        if (battleOver) return;
        currentState = BattleState.Defeat;
        EndBattle(false);
    }

    public void OnEnemyTimeout()
    {
        if (battleOver) return;
        currentState = BattleState.Victory;
        EndBattle(true);
    }

    // ================== Helpers ==================

    private IEnumerator WaitFx(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSecondsRealtime(seconds);
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

            var (prevStage, newStage) = ApplyBuffStageDetailed(unit, slot, stat, delta);

            uiManager.PlayBuffDebuffFx(unit == playerUnit, slot, isBuff);

            if (newStage == 0)
            {
                if (prevStage != 0)
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} returned to normal.");
                else
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} remains normal.");
            }
            else
            {
                bool flipped = (Mathf.Sign(prevStage) != Mathf.Sign(newStage)) && prevStage != 0;
                if (flipped)
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} neutralized the opposite stage and moved to {newStage:+#;-#}.");
                else
                {
                    string dir = newStage > 0 ? "increased" : "decreased";
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} {dir} to stage {newStage:+#;-#}.");
                }
            }
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

    // ---------- MINIMAL CHANGE: call UI to rebuild chips ----------
    private void ApplyBuffStage(MechUnit unit, ModuleSlot slot, string stat, int delta)
    {
        ApplyBuffStageDetailed(unit, slot, stat, delta);
    }

    private (int prev, int now) ApplyBuffStageDetailed(MechUnit unit, ModuleSlot slot, string stat, int delta)
    {
        int prev = GetBuffStage(unit, slot, stat);
        int now = Mathf.Clamp(prev + delta, -6, 6);

        if (slot == ModuleSlot.Matrix)
        {
            if (!matrixBuffsDict.ContainsKey(unit)) matrixBuffsDict[unit] = new Dictionary<string, int>();
            if (now == 0) matrixBuffsDict[unit].Remove(stat);
            else matrixBuffsDict[unit][stat] = now;

            uiManager.SetBuffChips(unit == playerUnit, ModuleSlot.Matrix,
                matrixBuffsDict.ContainsKey(unit) ? matrixBuffsDict[unit] : null);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            var dict = unit.partStatuses[slot].buffs;
            if (now == 0) dict.Remove(stat);
            else dict[stat] = now;

            uiManager.SetBuffChips(unit == playerUnit, slot, dict);
        }

        return (prev, now);
    }

    private void ResetBuffStages(MechUnit unit, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix)
        {
            if (matrixBuffsDict.ContainsKey(unit)) matrixBuffsDict[unit].Clear();

            uiManager.SetBuffChips(unit == playerUnit, ModuleSlot.Matrix,
                matrixBuffsDict.ContainsKey(unit) ? matrixBuffsDict[unit] : null);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            unit.partStatuses[slot].buffs.Clear();

            uiManager.SetBuffChips(unit == playerUnit, slot, unit.partStatuses[slot].buffs);
        }
    }

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
            if (kvp.Key != ModuleSlot.Matrix && !unit.IsPartBroken(kvp.Key))
                return true;
        }
        return false;
    }

    // ====================== Destroyed-part helper ======================
    private void HandlePartDestroyed(MechUnit unit, bool isPlayer, ModuleSlot slot)
    {
        if (uiManager != null)
        {
            uiManager.PlayFxOnSlot("FX_Explosion", isPlayer, slot);
            uiManager.SetPartDestroyedVisual(isPlayer, slot, true);
        }
    }
}