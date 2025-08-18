using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using System; // para reflection em TMP
using MechBattle;

public class UIManager : MonoBehaviour
{
    [Header("Combat Log & Actions")]
    public Text combatlogText;
    public Button[] actionButtons;
    public ScrollRect combatScroll;

    [Header("Player Health Bars")]
    public Slider playerMatrixHPBar;
    public Slider playerRightArmHPBar;
    public Slider playerLeftArmHPBar;
    public Slider playerLowerBodyHPBar;

    [Header("Enemy Health Bars")]
    public Slider enemyMatrixHPBar;
    public Slider enemyRightArmHPBar;
    public Slider enemyLeftArmHPBar;
    public Slider enemyLowerBodyHPBar;

    [Header("Player PaperDoll Images")]
    public Image playerMatrixImg;
    public Image playerRightArmImg;
    public Image playerLeftArmImg;
    public Image playerLowerImg;

    [Header("Enemy PaperDoll Images")]
    public Image enemyMatrixImg;
    public Image enemyRightArmImg;
    public Image enemyLeftArmImg;
    public Image enemyLowerImg;

    // =========================
    // PaperDoll init
    // =========================
    public void InitializePaperDolls(MechUnitLoader playerLoader, MechUnitLoader enemyLoader)
    {
        if (playerLoader != null)
        {
            SetImg(playerMatrixImg, BestSprite(playerLoader.ResolvedMatrix));
            SetImg(playerRightArmImg, BestSprite(playerLoader.ResolvedRightArm));
            SetImg(playerLeftArmImg, BestSprite(playerLoader.ResolvedLeftArm));
            SetImg(playerLowerImg, BestSprite(playerLoader.ResolvedLower));
        }

        if (enemyLoader != null)
        {
            SetImg(enemyMatrixImg, BestSprite(enemyLoader.ResolvedMatrix));
            SetImg(enemyRightArmImg, BestSprite(enemyLoader.ResolvedRightArm));
            SetImg(enemyLeftArmImg, BestSprite(enemyLoader.ResolvedLeftArm));
            SetImg(enemyLowerImg, BestSprite(enemyLoader.ResolvedLower));
        }
    }

    private void SetImg(Image img, Sprite s)
    {
        if (!img) return;
        img.sprite = s;
        img.enabled = (s != null);
        img.preserveAspect = true;
    }

    private Sprite BestSprite(ScriptableObject so)
    {
        if (so == null) return null;
        var t = so.GetType();

        // tenta campos/propriedades comuns
        string[] names = { "battleSprite", "paperDollSprite", "editorSprite", "sprite", "icon" };
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
            {
                var val = f.GetValue(so) as Sprite;
                if (val) return val;
            }
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType))
            {
                var val = p.GetValue(so, null) as Sprite;
                if (val) return val;
            }
        }

        // Fallback por Resources key
        string key = null;
        if (so is Matrix mm) key = mm.matrixCode;
        else if (so is MechPart mp) key = mp.partCode;

        if (!string.IsNullOrEmpty(key))
        {
            var spr = Resources.Load<Sprite>($"PaperDoll/{key}");
            if (!spr) spr = Resources.Load<Sprite>(key);
            return spr;
        }
        return null;
    }

    // =========================
    // Health Bars
    // =========================
    public void InitializeHealthBars(MechUnit playerUnit, MechUnit enemyUnit,
        Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs)
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
                if (fillImage != null)
                {
                    fillImage.color = Color.Lerp(Color.red, Color.green, (float)currentHP / Mathf.Max(1, maxHP));
                    if (currentHP <= 0) fillImage.color = Color.gray;
                }
            }
        }
    }

    public void UpdateHealthBars(MechUnit unit, ModuleSlot slot, int newHP,
        Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs,
        MechUnit playerUnit, MechUnit enemyUnit)
    {
        bool isPlayer = unit == playerUnit;
        int maxHP = isPlayer ? playerMaxHPs[slot] : enemyMaxHPs[slot];
        Slider bar = null;

        if (isPlayer)
        {
            switch (slot)
            {
                case ModuleSlot.Matrix: bar = playerMatrixHPBar; break;
                case ModuleSlot.RightArm: bar = playerRightArmHPBar; break;
                case ModuleSlot.LeftArm: bar = playerLeftArmHPBar; break;
                case ModuleSlot.LowerBody: bar = playerLowerBodyHPBar; break;
            }
        }
        else
        {
            switch (slot)
            {
                case ModuleSlot.Matrix: bar = enemyMatrixHPBar; break;
                case ModuleSlot.RightArm: bar = enemyRightArmHPBar; break;
                case ModuleSlot.LeftArm: bar = enemyLeftArmHPBar; break;
                case ModuleSlot.LowerBody: bar = enemyLowerBodyHPBar; break;
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
                slider.handleRect.gameObject.SetActive(false);
        }
    }

    // =========================
    // Action Buttons
    // =========================

    // rótulos curtos para slots (cabem no botão)
    private static string ShortSlot(ModuleSlot slot)
    {
        switch (slot)
        {
            case ModuleSlot.RightArm: return "R. Arm";
            case ModuleSlot.LeftArm: return "L. Arm";
            case ModuleSlot.LowerBody: return "Lower";
            case ModuleSlot.Matrix: return "Matrix";
            default: return slot.ToString();
        }
    }

    // define texto tanto em Text (Legacy) quanto em TMP (sem precisar referenciar TMPro)
    private void SetButtonLabel(Button btn, string text)
    {
        var legacy = btn.GetComponentInChildren<Text>(true);
        if (legacy) { legacy.text = text; return; }

        // tenta achar TMP por reflexão
        var comps = btn.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            var type = c.GetType();
            if (type.Name == "TextMeshProUGUI")
            {
                var prop = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null) prop.SetValue(c, text, null);
                return;
            }
        }
    }

    // Só nome do ataque; desabilita se não houver
    public void RenderActionButtons(MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        ClearButtonListeners();
        int buttonIndex = 0;

        if (unit != null && unit.modules != null)
        {
            foreach (var kvp in unit.modules)
            {
                if (buttonIndex >= actionButtons.Length) break;

                ModuleSlot slot = kvp.Key;
                ModuleData module = kvp.Value;

                if (unit.IsPartBroken(slot)) continue;

                var btn = actionButtons[buttonIndex++];
                btn.gameObject.SetActive(true);

                string attackName = module?.attack?.attackName;
                bool hasAttack = !string.IsNullOrEmpty(attackName);

                SetButtonLabel(btn, hasAttack ? attackName : "(no attack)");
                btn.interactable = hasAttack;

                btn.onClick.RemoveAllListeners();
                if (hasAttack)
                {
                    ModuleSlot capturedSlot = slot;
                    AttackData capturedAttack = module.attack;
                    btn.onClick.AddListener(() => onAttackSelected(capturedSlot, capturedAttack));
                }
            }
        }

        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    public void RenderSelfTargetButtons(MechUnit userUnit, System.Action<ModuleSlot> onTargetSelected)
    {
        ClearButtonListeners();
        int buttonIndex = 0;
        var slots = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody, ModuleSlot.Matrix };

        foreach (var slot in slots)
        {
            if (slot != ModuleSlot.Matrix && userUnit.IsPartBroken(slot)) continue;
            if (buttonIndex >= actionButtons.Length) break;

            var btn = actionButtons[buttonIndex++];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            SetButtonLabel(btn, $"Self: {ShortSlot(slot)}");

            var capturedSlot = slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));
        }

        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    public void RenderTargetButtons(MechUnit targetUnit, System.Action<ModuleSlot> onTargetSelected)
    {
        ClearButtonListeners();
        int buttonIndex = 0;
        var slots = new List<ModuleSlot> { ModuleSlot.RightArm, ModuleSlot.LeftArm, ModuleSlot.LowerBody };
        if (targetUnit.CanAttackMatrix()) slots.Add(ModuleSlot.Matrix);

        foreach (var slot in slots)
        {
            if (slot != ModuleSlot.Matrix && targetUnit.IsPartBroken(slot)) continue;
            if (buttonIndex >= actionButtons.Length) break;

            var btn = actionButtons[buttonIndex++];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            SetButtonLabel(btn, ShortSlot(slot));

            var capturedSlot = slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));
        }

        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    // =========================
    // Log
    // =========================
    public void LogMessage(string message)
    {
        if (combatlogText != null)
            combatlogText.text += message + "\n";
        ScrollToBottom();
    }

    public void ScrollToBottom()
    {
        if (combatScroll == null) return;
        Canvas.ForceUpdateCanvases();
        combatScroll.verticalNormalizedPosition = 0f;
    }

    public void ClearButtonListeners()
    {
        if (actionButtons == null) return;
        foreach (var btn in actionButtons)
            if (btn) btn.onClick.RemoveAllListeners();
    }

    public void DisableAllButtons()
    {
        if (actionButtons == null) return;
        foreach (var btn in actionButtons)
            if (btn) btn.interactable = false;
    }
}
