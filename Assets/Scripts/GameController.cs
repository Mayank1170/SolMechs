using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MechBattle;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    public Text combatlogText;
    public Button[] actionButtons;
    public ScrollRect combatScroll;
    public Slider playerMatrixHPBar;
    public Slider playerRightArmHPBar;
    public Slider playerLeftArmHPBar;
    public Slider playerLowerBodyHPBar;
    public Slider enemyMatrixHPBar;
    public Slider enemyRightArmHPBar;
    public Slider enemyLeftArmHPBar;
    public Slider enemyLowerBodyHPBar;

    [Header("Mech GameObjects")]
    public GameObject playerMechObject;
    public GameObject enemyMechObject;

    private MechUnit playerUnit;
    private MechUnit enemyUnit;
    private AttackData selectedAttack;
    private enum BattleState { SelectingAttack, SelectingTarget, EnemyTurn, Victory, Defeat }
    private BattleState currentState = BattleState.SelectingAttack;
    private int turnCounter = 0;
    private Dictionary<ModuleSlot, int> playerMaxHPs = new Dictionary<ModuleSlot, int>();
    private Dictionary<ModuleSlot, int> enemyMaxHPs = new Dictionary<ModuleSlot, int>();

    void Awake()
    {
        playerUnit = playerMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        enemyUnit = enemyMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        if (playerUnit == null || enemyUnit == null)
        {
            Debug.LogError("⚠️ MechUnit data not loaded! Check MechUnitLoader.");
        }
    }

    void Start()
    {
        InitializeMaxHPs();
        InitializeHealthBars();
        DisableSliderInteractabilityAndHandles();
        combatlogText.text = "Battle start!\n";
        combatlogText.text += $"{playerUnit?.Name ?? "Player"} vs {enemyUnit?.Name ?? "Enemy"}!\n\n";
        ScrollToBottom();
        RenderActionButtons();
    }

    void InitializeMaxHPs()
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

    void InitializeHealthBars()
    {
        UpdateHealthBar(playerMatrixHPBar, playerUnit?.matrixHP ?? 0, playerMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(playerRightArmHPBar, playerUnit?.partStatuses[ModuleSlot.RightArm]?.currentHP ?? 0, playerMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(playerLeftArmHPBar, playerUnit?.partStatuses[ModuleSlot.LeftArm]?.currentHP ?? 0, playerMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(playerLowerBodyHPBar, playerUnit?.partStatuses[ModuleSlot.LowerBody]?.currentHP ?? 0, playerMaxHPs[ModuleSlot.LowerBody]);
        UpdateHealthBar(enemyMatrixHPBar, enemyUnit?.matrixHP ?? 0, enemyMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(enemyRightArmHPBar, enemyUnit?.partStatuses[ModuleSlot.RightArm]?.currentHP ?? 0, enemyMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(enemyLeftArmHPBar, enemyUnit?.partStatuses[ModuleSlot.LeftArm]?.currentHP ?? 0, enemyMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(enemyLowerBodyHPBar, enemyUnit?.partStatuses[ModuleSlot.LowerBody]?.currentHP ?? 0, enemyMaxHPs[ModuleSlot.LowerBody]);
    }

    void DisableSliderInteractabilityAndHandles()
    {
        DisableSlider(playerMatrixHPBar);
        DisableSlider(playerRightArmHPBar);
        DisableSlider(playerLeftArmHPBar);
        DisableSlider(playerLowerBodyHPBar);
        DisableSlider(enemyMatrixHPBar);
        DisableSlider(enemyRightArmHPBar);
        DisableSlider(enemyLeftArmHPBar);
        DisableSlider(enemyLowerBodyHPBar);
    }

    void DisableSlider(Slider slider)
    {
        if (slider != null)
        {
            slider.interactable = false; // Desativa interação com mouse
            if (slider.handleRect != null)
            {
                slider.handleRect.gameObject.SetActive(false); // Esconde o handle (pontinho)
            }
        }
    }

    void UpdateHealthBar(Slider bar, int currentHP, int maxHP)
    {
        if (bar != null)
        {
            bar.maxValue = maxHP;
            bar.value = Mathf.Max(0, currentHP);
            if (bar.fillRect != null)
            {
                Image fillImage = bar.fillRect.GetComponent<Image>();
                fillImage.color = Color.Lerp(Color.red, Color.green, (float)currentHP / maxHP);
                if (currentHP <= 0)
                {
                    fillImage.color = Color.gray; // Cinza para indicar "inoperante"
                }
            }
        }
    }

    void RenderActionButtons()
    {
        ClearButtonListeners();
        if (currentState == BattleState.SelectingAttack)
        {
            turnCounter++;
            combatlogText.text += $"Turn {turnCounter}\n";
            ScrollToBottom();
            int buttonIndex = 0;
            if (playerUnit != null && playerUnit.modules != null)
            {
                foreach (var kvp in playerUnit.modules)
                {
                    ModuleSlot slot = kvp.Key;
                    ModuleData module = kvp.Value;

                    if (playerUnit.IsPartBroken(slot) || buttonIndex >= actionButtons.Length) continue;

                    var btn = actionButtons[buttonIndex];
                    btn.gameObject.SetActive(true);
                    btn.interactable = true;
                    string label = $"{slot} – {module?.attack?.attackName ?? "No Attack"}";
                    btn.GetComponentInChildren<Text>().text = label;

                    ModuleSlot capturedSlot = slot;
                    AttackData capturedAttack = module?.attack;
                    btn.onClick.AddListener(() => SelectAttack(capturedSlot, capturedAttack));

                    buttonIndex++;
                }
            }
            for (int i = buttonIndex; i < actionButtons.Length; i++)
                actionButtons[i].gameObject.SetActive(false);
        }
        else if (currentState == BattleState.SelectingTarget)
        {
            RenderTargetButtons();
        }
    }

    void SelectAttack(ModuleSlot sourceSlot, AttackData attack)
    {
        if (playerUnit == null || playerUnit.IsPartBroken(sourceSlot))
        {
            combatlogText.text += $"{sourceSlot} is broken!\n";
            ScrollToBottom();
            return;
        }

        selectedAttack = attack;
        currentState = BattleState.SelectingTarget;
        RenderActionButtons();
    }

    void RenderTargetButtons()
    {
        var targets = new List<(string name, ModuleSlot slot)>
        {
            ("Right Arm", ModuleSlot.RightArm),
            ("Left Arm", ModuleSlot.LeftArm),
            ("Lower Body", ModuleSlot.LowerBody)
        };
        if (enemyUnit?.CanAttackMatrix() ?? false)
            targets.Add(("Matrix", ModuleSlot.Matrix));

        for (int i = 0; i < actionButtons.Length && i < targets.Count; i++)
        {
            var btn = actionButtons[i];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.GetComponentInChildren<Text>().text = targets[i].name;

            ModuleSlot capturedSlot = targets[i].slot;
            btn.onClick.AddListener(() => ConfirmTarget(capturedSlot));
        }
        for (int i = targets.Count; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    void ConfirmTarget(ModuleSlot targetSlot)
    {
        if (targetSlot == ModuleSlot.Matrix && !(enemyUnit?.CanAttackMatrix() ?? false))
        {
            combatlogText.text += "Matrix locked!\n";
            ScrollToBottom();
            return;
        }

        int damage = CalculateDamage(selectedAttack, playerUnit, enemyUnit, targetSlot);
        int maxHP = enemyMaxHPs.ContainsKey(targetSlot) ? enemyMaxHPs[targetSlot] : 1;
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(enemyUnit, targetSlot);
        ApplyDamage(enemyUnit, targetSlot, damage);
        int newHP = GetCurrentHP(enemyUnit, targetSlot);
        ApplyEffect(playerUnit, targetSlot, selectedAttack, true);
        UpdateHealthBars(enemyUnit, targetSlot, newHP);

        string attackerName = playerUnit?.Name ?? "Player";
        string defenderName = enemyUnit?.Name ?? "Enemy";
        combatlogText.text += $"{attackerName} used {selectedAttack?.attackName ?? "Attack"} on {defenderName}'s {targetSlot}.\n";
        if (damage > 0) combatlogText.text += $"It dealt {damage} damage ({damagePercent:F1}%).\n";
        if (newHP == 0 && prevHP > 0) combatlogText.text += $"{defenderName}'s {targetSlot} is no longer combat-ready!\n";
        ScrollToBottom();

        if (enemyUnit?.matrixHP <= 0)
        {
            combatlogText.text += $"\n{defenderName}'s Matrix was destroyed!\n{attackerName} wins!\n";
            currentState = BattleState.Victory;
            EndBattle();
        }
        else
        {
            DisableAllButtons();
            Invoke(nameof(EnemyTurn), 1f);
        }
    }

    void EnemyTurn()
    {
        if (enemyUnit == null) return;

        var validModules = new List<ModuleSlot>();
        foreach (var kvp in enemyUnit.modules)
            if (!enemyUnit.IsPartBroken(kvp.Key)) validModules.Add(kvp.Key);

        if (validModules.Count == 0)
        {
            combatlogText.text += $"\n{enemyUnit.Name} can no longer fight!\n{playerUnit?.Name ?? "Player"} wins!\n";
            currentState = BattleState.Victory;
            EndBattle();
            return;
        }

        var chosenSlot = validModules[Random.Range(0, validModules.Count)];
        var attack = enemyUnit.modules[chosenSlot].attack;
        var target = ChooseTargetSlot(playerUnit, attack);
        if (target == ModuleSlot.Matrix && !playerUnit.CanAttackMatrix())
            target = ModuleSlot.LowerBody;

        int damage = CalculateDamage(attack, enemyUnit, playerUnit, target);
        int maxHP = playerMaxHPs.ContainsKey(target) ? playerMaxHPs[target] : 1;
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(playerUnit, target);
        ApplyDamage(playerUnit, target, damage);
        int newHP = GetCurrentHP(playerUnit, target);
        ApplyEffect(enemyUnit, target, attack, true);
        UpdateHealthBars(playerUnit, target, newHP);

        string attackerName = enemyUnit.Name;
        string defenderName = playerUnit?.Name ?? "Player";
        combatlogText.text += $"{attackerName} used {attack.attackName} on {defenderName}'s {target}.\n";
        if (damage > 0) combatlogText.text += $"It dealt {damage} damage ({damagePercent:F1}%).\n";
        if (newHP == 0 && prevHP > 0) combatlogText.text += $"{defenderName}'s {target} is no longer combat-ready!\n";
        ScrollToBottom();

        if (playerUnit?.matrixHP <= 0)
        {
            combatlogText.text += $"\n{defenderName}'s Matrix was destroyed!\n{attackerName} wins!\n";
            currentState = BattleState.Defeat;
            EndBattle();
        }
        else
        {
            combatlogText.text += "\n";
            ScrollToBottom();
            currentState = BattleState.SelectingAttack;
            RenderActionButtons();
        }
    }

    int GetCurrentHP(MechUnit unit, ModuleSlot slot)
    {
        if (unit == null) return 0;
        if (slot == ModuleSlot.Matrix) return unit.matrixHP;
        return unit.partStatuses.ContainsKey(slot) ? unit.partStatuses[slot].currentHP : 0;
    }

    void ApplyDamage(MechUnit unit, ModuleSlot slot, int damage)
    {
        if (unit == null) return;
        if (slot == ModuleSlot.Matrix)
            unit.matrixHP = Mathf.Max(0, unit.matrixHP - damage);
        else if (unit.partStatuses.ContainsKey(slot))
            unit.partStatuses[slot].currentHP = Mathf.Max(0, unit.partStatuses[slot].currentHP - damage);
    }

    void ApplyEffect(MechUnit attacker, ModuleSlot targetSlot, AttackData attack, bool isAttackerEffect)
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
                    ModuleSlot effectTarget = isAttackerEffect ?
                        (moveTargetType == MechBattle.TargetType.Self ? targetSlot : targetSlot) :
                        targetSlot;

                    if (attacker.partStatuses.ContainsKey(effectTarget))
                    {
                        var status = attacker.partStatuses[effectTarget];
                        status.buffs[stat] = isBuff ? value : -value;
                        string change = isBuff ? "rose" : "fell";
                        combatlogText.text += $"{attacker.Name}'s {effectTarget} {stat} {change} by {value}!\n";
                        ScrollToBottom();
                    }
                }
            }
        }
    }

    MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
    {
        foreach (var kvp in unit.modules)
        {
            if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Key.ToString()}");
        }
        return null;
    }

    int CalculateDamage(AttackData attack, MechUnit attacker, MechUnit defender, ModuleSlot targetSlot)
    {
        if (attack == null || attacker == null || defender == null || attacker.chassis == null || defender.chassis == null) return 0;
        float baseDamage = attack.damage;
        float multiplier = attack.type == "Physical"
            ? (attacker.chassis.baseStats?.ATK ?? 1) / Mathf.Max(1f, defender.chassis.baseStats?.DEF ?? 1)
            : (attacker.chassis.baseStats?.ENG ?? 1) / Mathf.Max(1f, defender.chassis.baseStats?.SYS ?? 1);

        if (attacker.partStatuses.ContainsKey(ModuleSlot.LowerBody))
        {
            var lowerStatus = attacker.partStatuses[ModuleSlot.LowerBody];
            if (lowerStatus.buffs.ContainsKey("ATK")) baseDamage += lowerStatus.buffs["ATK"];
        }
        if (defender.partStatuses.ContainsKey(targetSlot))
        {
            var targetStatus = defender.partStatuses[targetSlot];
            if (targetStatus.buffs.ContainsKey("DEF")) multiplier -= (targetStatus.buffs["DEF"] * 0.01f);
        }

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier));
    }

    ModuleSlot ChooseTargetSlot(MechUnit unit, AttackData attack)
    {
        if (unit == null) return ModuleSlot.LowerBody;
        var options = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (unit.CanAttackMatrix()) options.Add(ModuleSlot.Matrix);
        return options[Random.Range(0, options.Count)];
    }

    void UpdateHealthBars(MechUnit unit, ModuleSlot slot, int newHP)
    {
        if (unit == playerUnit)
        {
            if (slot == ModuleSlot.Matrix) UpdateHealthBar(playerMatrixHPBar, newHP, playerMaxHPs[ModuleSlot.Matrix]);
            else if (slot == ModuleSlot.RightArm) UpdateHealthBar(playerRightArmHPBar, newHP, playerMaxHPs[ModuleSlot.RightArm]);
            else if (slot == ModuleSlot.LeftArm) UpdateHealthBar(playerLeftArmHPBar, newHP, playerMaxHPs[ModuleSlot.LeftArm]);
            else if (slot == ModuleSlot.LowerBody) UpdateHealthBar(playerLowerBodyHPBar, newHP, playerMaxHPs[ModuleSlot.LowerBody]);
        }
        else if (unit == enemyUnit)
        {
            if (slot == ModuleSlot.Matrix) UpdateHealthBar(enemyMatrixHPBar, newHP, enemyMaxHPs[ModuleSlot.Matrix]);
            else if (slot == ModuleSlot.RightArm) UpdateHealthBar(enemyRightArmHPBar, newHP, enemyMaxHPs[ModuleSlot.RightArm]);
            else if (slot == ModuleSlot.LeftArm) UpdateHealthBar(enemyLeftArmHPBar, newHP, enemyMaxHPs[ModuleSlot.LeftArm]);
            else if (slot == ModuleSlot.LowerBody) UpdateHealthBar(enemyLowerBodyHPBar, newHP, enemyMaxHPs[ModuleSlot.LowerBody]);
        }
    }

    void DisableAllButtons()
    {
        foreach (var btn in actionButtons) btn.interactable = false;
    }

    void EndBattle()
    {
        DisableAllButtons();
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        combatScroll.verticalNormalizedPosition = 0f;
    }

    void ClearButtonListeners()
    {
        foreach (var btn in actionButtons) btn.onClick.RemoveAllListeners();
    }
}