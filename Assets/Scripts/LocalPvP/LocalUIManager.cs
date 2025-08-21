using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MechBattle;
using System;

public class LocalUIManager : MonoBehaviour
{
    public Text combatlogText;
    [Header("Player 1 Action Buttons")]
    public Button[] player1ActionButtons;
    [Header("Player 2 Action Buttons")]
    public Button[] player2ActionButtons;

    public ScrollRect combatScroll;
    public Slider playerMatrixHPBar;
    public Slider playerRightArmHPBar;
    public Slider playerLeftArmHPBar;
    public Slider playerLowerBodyHPBar;
    public Slider enemyMatrixHPBar;
    public Slider enemyRightArmHPBar;
    public Slider enemyLeftArmHPBar;
    public Slider enemyLowerBodyHPBar;

    public void InitializeHealthBars(MechUnit playerUnit, MechUnit enemyUnit, Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs)
    {
        UpdateHealthBar(playerMatrixHPBar, playerUnit.matrixHP, playerMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(playerRightArmHPBar, playerUnit.partStatuses[ModuleSlot.RightArm].currentHP, playerMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(playerLeftArmHPBar, playerUnit.partStatuses[ModuleSlot.LeftArm].currentHP, playerMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(playerLowerBodyHPBar, playerUnit.partStatuses[ModuleSlot.LowerBody].currentHP, playerMaxHPs[ModuleSlot.LowerBody]);
        UpdateHealthBar(enemyMatrixHPBar, enemyUnit.matrixHP, enemyMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(enemyRightArmHPBar, enemyUnit.partStatuses[ModuleSlot.RightArm].currentHP, enemyMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(enemyLeftArmHPBar, enemyUnit.partStatuses[ModuleSlot.LeftArm].currentHP, enemyMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(enemyLowerBodyHPBar, enemyUnit.partStatuses[ModuleSlot.LowerBody].currentHP, enemyMaxHPs[ModuleSlot.LowerBody]);
    }

    public void UpdateHealthBar(Slider bar, int currentHP, int maxHP)
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
                    fillImage.color = Color.gray;
                }
            }
        }
    }

    public void UpdateHealthBars(MechUnit unit, ModuleSlot slot, int newHP, Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs, MechUnit playerUnit, MechUnit enemyUnit)
    {
        bool isPlayer = unit == playerUnit;
        int maxHP = isPlayer ? playerMaxHPs[slot] : enemyMaxHPs[slot];
        Slider bar = null;
        if (isPlayer)
        {
            switch (slot)
            {
                case ModuleSlot.Matrix:
                    bar = playerMatrixHPBar;
                    break;
                case ModuleSlot.RightArm:
                    bar = playerRightArmHPBar;
                    break;
                case ModuleSlot.LeftArm:
                    bar = playerLeftArmHPBar;
                    break;
                case ModuleSlot.LowerBody:
                    bar = playerLowerBodyHPBar;
                    break;
            }
        }
        else
        {
            switch (slot)
            {
                case ModuleSlot.Matrix:
                    bar = enemyMatrixHPBar;
                    break;
                case ModuleSlot.RightArm:
                    bar = enemyRightArmHPBar;
                    break;
                case ModuleSlot.LeftArm:
                    bar = enemyLeftArmHPBar;
                    break;
                case ModuleSlot.LowerBody:
                    bar = enemyLowerBodyHPBar;
                    break;
            }
        }
        UpdateHealthBar(bar, newHP, maxHP);
    }

    public void DisableSliderInteractabilityAndHandles()
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

    private void DisableSlider(Slider slider)
    {
        if (slider != null)
        {
            slider.interactable = false;
            if (slider.handleRect != null)
            {
                slider.handleRect.gameObject.SetActive(false);
            }
        }
    }

    public void RenderActionButtonsPlayer1(MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        RenderButtons(player1ActionButtons, unit, onAttackSelected);
        DisableButtons(player2ActionButtons);
    }

    public void RenderActionButtonsPlayer2(MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        RenderButtons(player2ActionButtons, unit, onAttackSelected);
        DisableButtons(player1ActionButtons);
    }

    private void RenderButtons(Button[] buttons, MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        ClearButtonListeners(buttons);
        int buttonIndex = 0;
        if (unit != null && unit.modules != null)
        {
            foreach (var kvp in unit.modules)
            {
                ModuleSlot slot = kvp.Key;
                ModuleData module = kvp.Value;
                if (unit.IsPartBroken(slot) || buttonIndex >= buttons.Length) continue;

                var btn = buttons[buttonIndex];
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                string label = $"{slot} – {module?.attack?.attackName ?? "No Attack"}";
                btn.GetComponentInChildren<Text>().text = label;

                ModuleSlot capturedSlot = slot;
                AttackData capturedAttack = module?.attack;
                btn.onClick.AddListener(() => onAttackSelected(capturedSlot, capturedAttack));
                buttonIndex++;
            }
        }
        for (int i = buttonIndex; i < buttons.Length; i++)
        {
            buttons[i].gameObject.SetActive(false);
        }
    }

    public void RenderSelfTargetButtons(MechUnit userUnit, System.Action<ModuleSlot> onTargetSelected)
    {
        // This method needs to be updated to handle which player's buttons to render on
        // You would need to pass a player identifier to it
        RenderTargetButtons(player1ActionButtons, userUnit, onTargetSelected, true);
    }

    public void RenderTargetButtons(MechUnit targetUnit, System.Action<ModuleSlot> onTargetSelected)
    {
        // This method needs to be updated to handle which player's buttons to render on
        // You would need to pass a player identifier to it
        RenderTargetButtons(player1ActionButtons, targetUnit, onTargetSelected, false);
    }

    private void RenderTargetButtons(Button[] buttons, MechUnit unit, System.Action<ModuleSlot> onTargetSelected, bool isSelf)
    {
        ClearButtonListeners(buttons);
        int buttonIndex = 0;
        var slots = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (unit.CanAttackMatrix()) slots.Add(ModuleSlot.Matrix);

        foreach (var slot in slots)
        {
            if (slot != ModuleSlot.Matrix && unit.IsPartBroken(slot)) continue;
            if (buttonIndex >= buttons.Length) break;
            var btn = buttons[buttonIndex];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.GetComponentInChildren<Text>().text = isSelf ? $"My {slot}" : $"Opponent's {slot}";
            ModuleSlot capturedSlot = slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));
            buttonIndex++;
        }
        for (int i = buttonIndex; i < buttons.Length; i++)
            buttons[i].gameObject.SetActive(false);
    }

    public void LogMessage(string message)
    {
        combatlogText.text += message + "\n";
        ScrollToBottom();
    }

    public void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        combatScroll.verticalNormalizedPosition = 0f;
    }

    private void ClearButtonListeners(Button[] buttons)
    {
        foreach (var btn in buttons)
            btn.onClick.RemoveAllListeners();
    }

    public void DisableAllButtons()
    {
        DisableButtons(player1ActionButtons);
        DisableButtons(player2ActionButtons);
    }

    private void DisableButtons(Button[] buttons)
    {
        foreach (var btn in buttons)
            btn.interactable = false;
    }
}