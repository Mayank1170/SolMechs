using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MechBattle;

public class UIManager : MonoBehaviour
{
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

    public void RenderActionButtons(MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        ClearButtonListeners();
        int buttonIndex = 0;
        if (unit != null && unit.modules != null)
        {
            foreach (var kvp in unit.modules)
            {
                ModuleSlot slot = kvp.Key;
                ModuleData module = kvp.Value;

                if (unit.IsPartBroken(slot) || buttonIndex >= actionButtons.Length) continue;

                var btn = actionButtons[buttonIndex];
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
        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    public void RenderTargetButtons(MechUnit enemyUnit, System.Action<ModuleSlot> onTargetSelected)
    {
        ClearButtonListeners();
        var targets = new List<(string name, ModuleSlot slot)>
        {
            ("Opponent's Right Arm", ModuleSlot.RightArm),
            ("Opponent's Left Arm", ModuleSlot.LeftArm),
            ("Opponent's Lower Body", ModuleSlot.LowerBody)
        };
        if (enemyUnit?.CanAttackMatrix() ?? false)
            targets.Add(("Opponent's Matrix", ModuleSlot.Matrix));

        int buttonIndex = 0;
        for (int i = 0; i < targets.Count && buttonIndex < actionButtons.Length; i++)
        {
            var btn = actionButtons[buttonIndex];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.GetComponentInChildren<Text>().text = targets[i].name;

            ModuleSlot capturedSlot = targets[i].slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));

            buttonIndex++;
        }
        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
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

    public void ClearButtonListeners()
    {
        if (actionButtons == null) return;
        foreach (var btn in actionButtons)
        {
            if (btn != null)
                btn.onClick.RemoveAllListeners();
        }
    }

    public void DisableAllButtons()
    {
        foreach (var btn in actionButtons)
            if (btn != null)
                btn.interactable = false;
    }
}