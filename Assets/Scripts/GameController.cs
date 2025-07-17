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

    [Header("Mech GameObjects")]
    public GameObject playerMechObject;
    public GameObject enemyMechObject;

    private MechUnit playerUnit;
    private MechUnit enemyUnit;
    private AttackData selectedAttack;
    private enum BattleState { SelectingAttack, SelectingTarget, EnemyTurn, Victory }
    private BattleState currentState = BattleState.SelectingAttack;

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
        combatlogText.text = ">> Combat Initialized!\n";
        combatlogText.text += $">> {playerUnit?.Name ?? "Player"} vs {enemyUnit?.Name ?? "Enemy"}!\n";
        ScrollToBottom();
        RenderActionButtons();
    }

    void RenderActionButtons()
    {
        ClearButtonListeners();
        if (currentState == BattleState.SelectingAttack)
        {
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
            combatlogText.text += $"❌ {sourceSlot} está quebrado ou playerUnit nulo.\n";
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
            combatlogText.text += $"❌ Matrix só pode ser atacada após destruir um dos braços!\n";
            ScrollToBottom();
            return;
        }

        int damage = CalculateDamage(selectedAttack, playerUnit, enemyUnit, targetSlot);
        ApplyDamage(enemyUnit, targetSlot, damage);
        ApplyEffect(playerUnit, targetSlot, selectedAttack, true); // Apply to attacker

        combatlogText.text += $"▶ {playerUnit?.Name ?? "Player"} usou {selectedAttack?.attackName ?? "No Attack"} em {targetSlot}!\n";
        if (damage > 0) combatlogText.text += $"◀ {enemyUnit?.Name ?? "Enemy"} sofreu {damage} de dano.\n";
        ScrollToBottom();

        if (enemyUnit?.matrixHP <= 0)
        {
            combatlogText.text += $"🏁 {enemyUnit?.Name ?? "Enemy"} foi destruído!\n";
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
            combatlogText.text += $"🏁 {enemyUnit.Name} não pode mais atacar. Vitória!\n";
            EndBattle();
            return;
        }

        var chosenSlot = validModules[Random.Range(0, validModules.Count)];
        var attack = enemyUnit.modules[chosenSlot].attack;
        var target = ChooseTargetSlot(playerUnit, attack);
        if (target == ModuleSlot.Matrix && !playerUnit.CanAttackMatrix())
            target = ModuleSlot.LowerBody;

        int damage = CalculateDamage(attack, enemyUnit, playerUnit, target);
        ApplyDamage(playerUnit, target, damage);
        ApplyEffect(enemyUnit, target, attack, true); // Apply to attacker

        combatlogText.text += $"◀ {enemyUnit.Name} usou {attack.attackName} em {target}!\n";
        if (damage > 0) combatlogText.text += $"▶ {playerUnit?.Name ?? "Player"} sofreu {damage} de dano.\n";
        ScrollToBottom();

        if (playerUnit?.matrixHP <= 0)
        {
            combatlogText.text += $"🏁 {playerUnit?.Name ?? "Player"} foi destruído!\n";
            EndBattle();
        }
        else
        {
            currentState = BattleState.SelectingAttack;
            RenderActionButtons();
        }
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
                    string stat = effectParts[1].Split(' ')[0].ToLower(); // e.g., "DEF", "HP", "Evasion"
                    bool isBuff = effect.StartsWith("+");

                    // Explicit cast to MechBattle.TargetType and use enum comparison
                    MechBattle.TargetType moveTargetType = part.moves[0].targetType;
                    ModuleSlot effectTarget = isAttackerEffect ?
                        (moveTargetType == MechBattle.TargetType.Self ? targetSlot : targetSlot) :
                        targetSlot;

                    if (attacker.partStatuses.ContainsKey(effectTarget))
                    {
                        var status = attacker.partStatuses[effectTarget];
                        if (isBuff)
                        {
                            if (stat == "def" || stat == "hp" || stat == "evasion" || stat == "spd")
                            {
                                status.buffs[stat.ToUpper()] = value; // Store buff until part is destroyed
                                combatlogText.text += $"▶ {attacker.Name} gained +{value} {stat.ToUpper()} on {effectTarget}.\n";
                            }
                        }
                        else // Debuff (e.g., "-10 DEF")
                        {
                            if (stat == "def" || stat == "hp" || stat == "evasion" || stat == "spd")
                            {
                                status.buffs[stat.ToUpper()] = -value; // Store debuff
                                combatlogText.text += $"▶ {attacker.Name} lost {value} {stat.ToUpper()} on {effectTarget}.\n";
                            }
                        }
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

        // Apply buffs/debuffs to damage calculation, prioritizing lower body buffs
        if (attacker.partStatuses.ContainsKey(ModuleSlot.LowerBody))
        {
            var lowerStatus = attacker.partStatuses[ModuleSlot.LowerBody];
            if (lowerStatus.buffs.ContainsKey("ATK")) baseDamage += lowerStatus.buffs["ATK"];
        }
        if (defender.partStatuses.ContainsKey(targetSlot))
        {
            var targetStatus = defender.partStatuses[targetSlot];
            if (targetStatus.buffs.ContainsKey("DEF")) multiplier -= (targetStatus.buffs["DEF"] * 0.01f); // 1% DEF reduction per point
        }

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier)); // Minimum 10% damage
    }

    ModuleSlot ChooseTargetSlot(MechUnit unit, AttackData attack)
    {
        if (unit == null) return ModuleSlot.LowerBody;
        var options = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (unit.CanAttackMatrix()) options.Add(ModuleSlot.Matrix);
        return options[Random.Range(0, options.Count)];
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