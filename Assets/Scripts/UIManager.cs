using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    [Header("Result Panel")]
    public BattleResultPanel resultPanel;

    // ===== Battle FX (optional, lightweight) =====
    [Header("Battle FX (optional)")]
    public bool fxEnabled = true;                 // master switch
    public RectTransform battleRoot;              // for small screen shake (assign a top-level RectTransform)
    public GameObject floatingTextPrefab;         // prefab with Text (or TMP) + CanvasGroup

    [Range(0.05f, 0.6f)] public float hpTweenDuration = 0.2f;
    [Range(0.02f, 0.3f)] public float flashDuration = 0.08f;
    [Range(1f, 20f)] public float shakeAmplitude = 6f;
    [Range(0.05f, 0.5f)] public float shakeDuration = 0.15f;

    public Color damageColor = new Color(1f, 0.35f, 0.35f);
    public Color healColor = new Color(0.35f, 1f, 0.55f);

    // Internal flag to avoid FX during initial layout
    private bool _isInitializing = false;

    // ===== PaperDoll init =====
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

        // Try common names first
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

        // Fallback via Resources by code
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
    // ===== end PaperDoll init =====

    // ===== Health Bars =====
    public void InitializeHealthBars(MechUnit playerUnit, MechUnit enemyUnit,
        Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs)
    {
        _isInitializing = true;

        UpdateHealthBar(playerMatrixHPBar, playerUnit.matrixHP, playerMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(playerRightArmHPBar, playerUnit.partStatuses[ModuleSlot.RightArm].currentHP, playerMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(playerLeftArmHPBar, playerUnit.partStatuses[ModuleSlot.LeftArm].currentHP, playerMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(playerLowerBodyHPBar, playerUnit.partStatuses[ModuleSlot.LowerBody].currentHP, playerMaxHPs[ModuleSlot.LowerBody]);

        UpdateHealthBar(enemyMatrixHPBar, enemyUnit.matrixHP, enemyMaxHPs[ModuleSlot.Matrix]);
        UpdateHealthBar(enemyRightArmHPBar, enemyUnit.partStatuses[ModuleSlot.RightArm].currentHP, enemyMaxHPs[ModuleSlot.RightArm]);
        UpdateHealthBar(enemyLeftArmHPBar, enemyUnit.partStatuses[ModuleSlot.LeftArm].currentHP, enemyMaxHPs[ModuleSlot.LeftArm]);
        UpdateHealthBar(enemyLowerBodyHPBar, enemyUnit.partStatuses[ModuleSlot.LowerBody].currentHP, enemyMaxHPs[ModuleSlot.LowerBody]);

        _isInitializing = false;
    }

    public void UpdateHealthBar(Slider bar, int currentHP, int maxHP)
    {
        if (bar == null) return;

        bar.maxValue = maxHP;

        // Tween value only outside initialization
        if (fxEnabled && !_isInitializing && bar.gameObject.activeInHierarchy)
            TweenHP(bar, Mathf.Max(0, currentHP));
        else
            bar.value = Mathf.Max(0, currentHP);

        // Fill color feedback
        if (bar.fillRect != null)
        {
            Image fillImage = bar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (currentHP <= 0) fillImage.color = Color.gray;
                else fillImage.color = Color.Lerp(Color.red, Color.green, (float)currentHP / Mathf.Max(1, maxHP));
            }
        }
    }

    public void UpdateHealthBars(MechUnit unit, ModuleSlot slot, int newHP,
        Dictionary<ModuleSlot, int> playerMaxHPs, Dictionary<ModuleSlot, int> enemyMaxHPs,
        MechUnit playerUnit, MechUnit enemyUnit)
    {
        bool isPlayer = unit == playerUnit;
        int maxHP = isPlayer ? playerMaxHPs[slot] : enemyMaxHPs[slot];

        // Pick the correct slider for this slot
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

        // Compute delta BEFORE updating the bar
        int prev = bar ? Mathf.RoundToInt(bar.value) : newHP;
        int delta = newHP - prev;

        // Update numeric and visual state
        UpdateHealthBar(bar, newHP, maxHP);

        // FX (skip during initialization)
        if (fxEnabled && !_isInitializing && delta != 0)
        {
            Image hitImg = isPlayer ? GetPlayerImageFor(slot) : GetEnemyImageFor(slot);
            RectTransform anchor = hitImg ? hitImg.rectTransform : (bar ? bar.GetComponent<RectTransform>() : null);

            if (delta < 0)
            {
                if (hitImg) Flash(hitImg, flashDuration);
                if (anchor) FloatNumber(anchor, delta, damageColor);
                Shake(); // small shake on any damage
            }
            else
            {
                if (anchor) FloatNumber(anchor, delta, healColor);
            }
        }
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

    // ===== Action Buttons =====
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

                string attackName = module?.attack?.attackName;
                bool hasAttack = !string.IsNullOrEmpty(attackName);

                // label shows only the attack name
                btn.GetComponentInChildren<Text>().text = hasAttack ? attackName : "(no attack)";
                btn.interactable = hasAttack;

                btn.onClick.RemoveAllListeners();
                if (hasAttack)
                {
                    ModuleSlot capturedSlot = slot;
                    AttackData capturedAttack = module.attack;
                    btn.onClick.AddListener(() => onAttackSelected(capturedSlot, capturedAttack));
                }

                buttonIndex++;
            }
        }

        // hide the remaining buttons
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

            var btn = actionButtons[buttonIndex];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.GetComponentInChildren<Text>().text = $"My {ShortSlotName(slot)}";
            ModuleSlot capturedSlot = slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));
            buttonIndex++;
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

            var btn = actionButtons[buttonIndex];
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.GetComponentInChildren<Text>().text = $"Target: {ShortSlotName(slot)}";
            ModuleSlot capturedSlot = slot;
            btn.onClick.AddListener(() => onTargetSelected(capturedSlot));
            buttonIndex++;
        }

        for (int i = buttonIndex; i < actionButtons.Length; i++)
            actionButtons[i].gameObject.SetActive(false);
    }

    private string ShortSlotName(ModuleSlot slot)
    {
        switch (slot)
        {
            case ModuleSlot.RightArm: return "R.Arm";
            case ModuleSlot.LeftArm: return "L.Arm";
            case ModuleSlot.LowerBody: return "Lower";
            case ModuleSlot.Matrix: return "Matrix";
            default: return slot.ToString();
        }
    }

    public void LogMessage(string message)
    {
        if (combatlogText)
        {
            combatlogText.text += message + "\n";
            ScrollToBottom();
        }
    }

    public void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (combatScroll) combatScroll.verticalNormalizedPosition = 0f;
    }

    public void ClearButtonListeners()
    {
        foreach (var btn in actionButtons)
            btn.onClick.RemoveAllListeners();
    }

    public void DisableAllButtons()
    {
        foreach (var btn in actionButtons)
            btn.interactable = false;
    }

    // ===== Result Panel API =====
    public void ShowBattleResult(bool playerWon, int points = 0)
    {
        if (!resultPanel)
            resultPanel = FindObjectOfType<BattleResultPanel>(true);
        if (resultPanel != null)
            resultPanel.Show(playerWon, points);
    }

    public void HideBattleResult()
    {
        if (!resultPanel)
            resultPanel = FindObjectOfType<BattleResultPanel>(true);
        if (!resultPanel) return;

        // If inactive, just ensure hidden state without coroutines
        if (!resultPanel.gameObject.activeInHierarchy)
        {
            resultPanel.HideImmediate();
            return;
        }
        resultPanel.Hide();
    }

    // ===== FX helpers =====
    public void Flash(Image img, float dur = -1f)
    {
        if (!fxEnabled || !img) return;
        StartCoroutine(FlashCo(img, dur > 0f ? dur : flashDuration));
    }
    private IEnumerator FlashCo(Image img, float d)
    {
        var c0 = img.color;
        img.color = Color.white;
        yield return new WaitForSecondsRealtime(d);
        img.color = c0;
    }

    public void FloatNumber(RectTransform anchor, int amount, Color col)
    {
        if (!fxEnabled || !floatingTextPrefab || !anchor) return;
        var go = Instantiate(floatingTextPrefab, anchor.transform.parent);
        var rt = go.transform as RectTransform;
        rt.anchorMin = anchor.anchorMin;
        rt.anchorMax = anchor.anchorMax;
        rt.anchoredPosition = anchor.anchoredPosition + new Vector2(0, 20f);

        var txt = go.GetComponentInChildren<Text>();
        if (txt) { txt.text = (amount > 0 ? "+" : "") + amount; txt.color = col; }

        var cg = go.GetComponent<CanvasGroup>();
        StartCoroutine(FloatCo(rt, cg));
    }
    private IEnumerator FloatCo(RectTransform t, CanvasGroup cg)
    {
        float t0 = 0f, dur = 0.6f;
        Vector2 start = t.anchoredPosition;
        while (t0 < dur)
        {
            t0 += Time.unscaledDeltaTime;
            float k = t0 / dur;
            t.anchoredPosition = start + new Vector2(0, Mathf.Lerp(0, 30f, k));
            if (cg) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(t.gameObject);
    }

    public void TweenHP(Slider bar, int to, float dur = -1f)
    {
        if (!fxEnabled || !bar || _isInitializing) { if (bar) bar.value = to; return; }
        StartCoroutine(TweenHPCo(bar, to, dur > 0f ? dur : hpTweenDuration));
    }
    private IEnumerator TweenHPCo(Slider bar, int to, float dur)
    {
        float from = bar.value, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            bar.value = Mathf.Lerp(from, to, k);
            yield return null;
        }
        bar.value = to;
    }

    public void Shake(float amp = -1f, float dur = -1f)
    {
        if (!fxEnabled || !battleRoot) return;
        StartCoroutine(ShakeCo(battleRoot, amp > 0f ? amp : shakeAmplitude, dur > 0f ? dur : shakeDuration));
    }
    private IEnumerator ShakeCo(RectTransform rt, float a, float d)
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - (t / d);
            rt.anchoredPosition = origin + Random.insideUnitCircle * (a * falloff);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ===== helpers to get target images =====
    private Image GetPlayerImageFor(ModuleSlot slot)
    {
        switch (slot)
        {
            case ModuleSlot.Matrix: return playerMatrixImg;
            case ModuleSlot.RightArm: return playerRightArmImg;
            case ModuleSlot.LeftArm: return playerLeftArmImg;
            case ModuleSlot.LowerBody: return playerLowerImg;
            default: return null;
        }
    }
    private Image GetEnemyImageFor(ModuleSlot slot)
    {
        switch (slot)
        {
            case ModuleSlot.Matrix: return enemyMatrixImg;
            case ModuleSlot.RightArm: return enemyRightArmImg;
            case ModuleSlot.LeftArm: return enemyLeftArmImg;
            case ModuleSlot.LowerBody: return enemyLowerImg;
            default: return null;
        }
    }
}
