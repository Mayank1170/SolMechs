using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechBattle;

public class UIManager : MonoBehaviour
{
    // =========================== Core UI ===========================
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

    // =========================== Light FX ==========================
    [Header("Battle FX (optional)")]
    public bool fxEnabled = true;                 // master switch
    public RectTransform battleRoot;              // used for a small screen shake
    public GameObject floatingTextPrefab;         // prefab with Text (or TMP) + CanvasGroup

    [Range(0.05f, 0.6f)] public float hpTweenDuration = 0.2f;
    [Range(0.02f, 0.3f)] public float flashDuration = 0.08f;
    [Range(1f, 20f)] public float shakeAmplitude = 6f;
    [Range(0.05f, 0.5f)] public float shakeDuration = 0.15f;

    public Color damageColor = new Color(1f, 0.35f, 0.35f);
    public Color healColor = new Color(0.35f, 1f, 0.55f);

    // Internal flag to avoid FX during initial layout
    private bool _isInitializing = false;

    // ========================= Buff System =========================
    [Header("Buff System")]
    public GameObject buffChipPrefab;

    [Header("Buff Icons (Up/Down per stat)")]
    public Sprite iconATK_Up;
    public Sprite iconATK_Down;

    public Sprite iconDEF_Up;
    public Sprite iconDEF_Down;

    public Sprite iconENG_Up;
    public Sprite iconENG_Down;

    public Sprite iconSYS_Up;
    public Sprite iconSYS_Down;

    public Sprite iconSPD_Up;
    public Sprite iconSPD_Down;

    [Header("Buff Containers - Player")]
    public Transform playerMatrixBuffs;
    public Transform playerRightBuffs;
    public Transform playerLeftBuffs;
    public Transform playerLowerBuffs;

    [Header("Buff Containers - Enemy")]
    public Transform enemyMatrixBuffs;
    public Transform enemyRightBuffs;
    public Transform enemyLeftBuffs;
    public Transform enemyLowerBuffs;

    [Header("Buff Colors (numbers only)")]
    public Color buffColor = new Color(0.08f, 0.95f, 0.59f, 1f);   // green (if you ever want numbers colored)
    public Color debuffColor = new Color(0.86f, 0.12f, 1f, 1f);    // magenta

    // ======================= PaperDoll Init ========================
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

        // Try common field/property names first
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
    // ===================== Health Bars =====================
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

    // ====================== Action Buttons ======================
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

    // ========================== Log / Result ==========================
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

    // ============================ FX =============================
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

    // Helpers to get target images for flash anchoring
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

    // ======================== Buff Chips API ========================
    /// <summary>
    /// Rebuilds the chips for a specific slot (player or enemy). Only non-zero stages are shown.
    /// It also disables any background Images without sprites to avoid a white square.
    /// </summary>
    public void SetBuffChips(bool isPlayer, ModuleSlot slot, Dictionary<string, int> stages)
    {
        var container = GetBuffContainer(isPlayer, slot);
        if (!container) return;

        // Clear previous chips
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        if (!buffChipPrefab || stages == null) return;

        string[] order = { "ATK", "DEF", "ENG", "SYS", "SPD" };

        foreach (var stat in order)
        {
            if (!stages.TryGetValue(stat, out int val) || val == 0) continue;

            var go = Instantiate(buffChipPrefab, container);

            // Resolve intended icon and label; then hide any stray background Images
            Image icon; Text label;
            ResolveChipVisuals(go, out icon, out label);

            // Choose sprite by stat and sign
            if (icon)
            {
                var spr = GetBuffSprite(stat, val);
                icon.sprite = spr;
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = (spr != null);

                if (spr == null)
                    Debug.LogWarning($"[UIManager] Missing sprite for {stat} {(val > 0 ? "Up" : "Down")}.");
            }

            // Make sure no other Image in the prefab shows a white square
            HideBackgroundImagesExcept(go, icon);

            // Value text (+/-N)
            if (label)
            {
                label.text = (val > 0 ? "+" : "") + val.ToString();
                label.color = Color.white; // keep as pure white; colors are carried by sprites
                label.raycastTarget = false;
            }

            // Normalize chip rect size/scale in case the prefab has none
            var rt = go.transform as RectTransform;
            if (rt)
            {
                if (rt.sizeDelta == Vector2.zero || rt.sizeDelta.x > 64f)
                    rt.sizeDelta = new Vector2(22f, 22f);
                rt.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Find the correct icon and label inside the chip prefab.
    /// Prefers children named "Icon" / "Label"; falls back to the first Image/Text found.
    /// </summary>
    private void ResolveChipVisuals(GameObject go, out Image icon, out Text label)
    {
        icon = null;
        label = null;

        var images = go.GetComponentsInChildren<Image>(true);
        var texts = go.GetComponentsInChildren<Text>(true);

        foreach (var img in images)
            if (img.name.Equals("Icon", System.StringComparison.OrdinalIgnoreCase)) { icon = img; break; }
        if (!icon && images.Length > 0) icon = images[0];

        foreach (var t in texts)
            if (t.name.Equals("Label", System.StringComparison.OrdinalIgnoreCase)) { label = t; break; }
        if (!label && texts.Length > 0) label = texts[0];
    }

    /// <summary>
    /// Disable any Image components (other than the chosen icon) that have no sprite,
    /// so they don't render as a solid white square.
    /// </summary>
    private void HideBackgroundImagesExcept(GameObject go, Image keep)
    {
        var images = go.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img == keep) continue;

            if (img.sprite == null)
                img.enabled = false;

            img.raycastTarget = false;
        }
    }

    /// <summary>
    /// Get the right container for player/enemy and slot.
    /// </summary>
    private Transform GetBuffContainer(bool isPlayer, ModuleSlot slot)
    {
        if (isPlayer)
        {
            switch (slot)
            {
                case ModuleSlot.Matrix: return playerMatrixBuffs;
                case ModuleSlot.RightArm: return playerRightBuffs;
                case ModuleSlot.LeftArm: return playerLeftBuffs;
                case ModuleSlot.LowerBody: return playerLowerBuffs;
            }
        }
        else
        {
            switch (slot)
            {
                case ModuleSlot.Matrix: return enemyMatrixBuffs;
                case ModuleSlot.RightArm: return enemyRightBuffs;
                case ModuleSlot.LeftArm: return enemyLeftBuffs;
                case ModuleSlot.LowerBody: return enemyLowerBuffs;
            }
        }
        return null;
    }

    /// <summary>
    /// Return the Up/Down sprite for the given stat based on sign of stageValue.
    /// </summary>
    private Sprite GetBuffSprite(string stat, int stageValue)
    {
        bool up = stageValue > 0;
        switch (stat.ToUpper())
        {
            case "ATK": return up ? iconATK_Up : iconATK_Down;
            case "DEF": return up ? iconDEF_Up : iconDEF_Down;
            case "ENG": return up ? iconENG_Up : iconENG_Down;
            case "SYS": return up ? iconSYS_Up : iconSYS_Down;
            case "SPD": return up ? iconSPD_Up : iconSPD_Down;
            default:
                Debug.LogWarning($"[UIManager] Unknown stat: {stat}");
                return null;
        }
    }

    // =========================== FX: Attack Animations ===========================
    [Header("Attack FX")]
    public AttackFxLibrary attackFxLibrary;  // assign the ScriptableObject in inspector

    /// <summary>
    /// Plays an attack FX under the proper anchor (slot image).
    /// fromPlayer: true if attack originates from the left mech (player side).
    /// sourceSlot: which part launched (used to choose anchor image if you prefer).
    /// attackName: used to pick prefab from the AttackFxLibrary.
    /// </summary>
    public void PlayAttackFx(bool fromPlayer, ModuleSlot sourceSlot, string attackName)
    {
        if (!fxEnabled || attackFxLibrary == null) return;
        var prefab = attackFxLibrary.Get(attackName);
        if (prefab == null) return;

        Image anchorImg = fromPlayer ? GetPlayerImageFor(sourceSlot) : GetEnemyImageFor(sourceSlot);
        if (anchorImg == null) anchorImg = fromPlayer ? playerMatrixImg : enemyMatrixImg;

        var parent = anchorImg ? anchorImg.transform : this.transform;
        bool leftToRight = fromPlayer; // player→enemy: true ; enemy→player: false

        var fx = Instantiate(prefab);
        fx.PlayUnder(parent, leftToRight);
    }

    // =========================== NEW: Buff/Debuff FX & Destroyed Visual ===========================
    [Header("Destroyed Parts Visual")]
    [Range(0f, 1f)] public float destroyedAlpha = 0.35f;  // set in Inspector
    [Range(0f, 1f)] public float normalAlpha = 1f;        // set in Inspector

    [Header("Buff/Debuff FX (by name in library)")]
    public string defaultBuffFxAttackName = "Fortify";    // change to a name that exista na sua AttackFxLibrary
    public string defaultDebuffFxAttackName = "Fortify";  // pode usar o mesmo até ter um específico

    /// <summary>
    /// Applies a simple transparency change on a part image when destroyed/recovered.
    /// </summary>
    public void SetPartDestroyedVisual(bool isPlayer, ModuleSlot slot, bool destroyed)
    {
        Image img = isPlayer ? GetPlayerImageFor(slot) : GetEnemyImageFor(slot);
        if (!img) return;
        var c = img.color;
        c.a = destroyed ? Mathf.Clamp01(destroyedAlpha) : Mathf.Clamp01(normalAlpha);
        img.color = c;
    }

    /// <summary>
    /// Convenience: trigger an FX anchored on a specific slot by attackName/key from library.
    /// </summary>
    public void PlayFxOnSlot(string attackNameOrKey, bool isPlayer, ModuleSlot slot)
    {
        if (!fxEnabled || attackFxLibrary == null) return;
        var prefab = attackFxLibrary.Get(attackNameOrKey);
        if (prefab == null) return;

        Image anchorImg = isPlayer ? GetPlayerImageFor(slot) : GetEnemyImageFor(slot);
        if (anchorImg == null) anchorImg = isPlayer ? playerMatrixImg : enemyMatrixImg;

        var fx = Instantiate(prefab);
        fx.PlayUnder(anchorImg ? anchorImg.transform : transform, isPlayer);
    }

    /// <summary>
    /// Generic buff/debuff sparkle: chooses a prefab name from the library depending on the flag.
    /// </summary>
    public void PlayBuffDebuffFx(bool isPlayer, ModuleSlot slot, bool isBuff)
    {
        if (!fxEnabled || attackFxLibrary == null) return;
        string key = isBuff ? defaultBuffFxAttackName : defaultDebuffFxAttackName;
        if (string.IsNullOrEmpty(key)) return;

        var prefab = attackFxLibrary.Get(key);
        if (prefab == null) return;

        Image anchorImg = isPlayer ? GetPlayerImageFor(slot) : GetEnemyImageFor(slot);
        if (anchorImg == null) anchorImg = isPlayer ? playerMatrixImg : enemyMatrixImg;

        var fx = Instantiate(prefab);
        fx.PlayUnder(anchorImg ? anchorImg.transform : transform, isPlayer);
    }
}
