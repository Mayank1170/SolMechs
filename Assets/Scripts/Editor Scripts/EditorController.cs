// EditorController.cs — arrow-based, Unity 2021-safe
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MechBattle
{
    public class EditorController : MonoBehaviour
    {
        [Header("Catalogs & Options")]
        public MatrixCatalog matrixCatalog;
        public PartCatalog partCatalog;
        public bool lockByFamily = true;

        [Header("Arrow UI (no dropdowns)")]
        public Text matrixLabel;
        public Text rightArmLabel;
        public Text leftArmLabel;
        public Text lowerLabel;

        public Button matrixPrevButton, matrixNextButton;
        public Button rightArmPrevButton, rightArmNextButton;
        public Button leftArmPrevButton, leftArmNextButton;
        public Button lowerPrevButton, lowerNextButton;

        [Header("Persistence")]
        public Button saveButton;

        [Header("Paper Doll (optional sprites)")]
        public Image imgMatrix, imgRightArm, imgLeftArm, imgLower;

        [Header("Stats UI (sliders)")]
        public Slider hpBar, atkBar, defBar, engBar, spdBar, sysBar;

        [Header("Stats Value Texts (optional)")]
        public Text hpValueText, atkValueText, defValueText, engValueText, spdValueText, sysValueText;

        [Header("Stat Range Mode")]
        public bool autoConfigureStatMaxFromCatalogs = true;
        public int hpMax = 200, atkMax = 150, defMax = 150, engMax = 150, spdMax = 150, sysMax = 150;

        [Header("Move Detail Panel")]
        public Text moveNameText, movePartText, moveDamageText, moveTypeText, moveTargetText, moveDescText;

        [Header("Move Description Options")]
        [Tooltip("If true, uses part.rawData as a fallback description when a move has no effect.")]
        public bool usePartRawDataAsDescFallback = false;
        [Tooltip("Placeholder to use for labels when no data is available.")]
        public string emptyMoveLabel = "-";
        [Tooltip("Text to show as the description when the move has no effect (empty string = show nothing).")]
        public string emptyMoveDescription = "";

        [Header("Navigation (optional)")]
        public Button testBattleButton;
        public string battleSceneName = "Play";

        [Header("Debug")]
        public bool previewPlusOneATKIfNoEffect = false;
        public bool logStatsOnChange = false;

        // Visual setup — unify non-HP bars, keep HP separate
        [Header("Stat Bar Visual (HP separate)")]
        [Tooltip("If true, ATK/DEF/ENG/SYS/SPD share the same visual max; HP has its own.")]
        public bool unifyNonHPBars = true;
        [Range(0f, 0.25f)] public float nonHPHeadroom = 0.05f;
        public int nonHPRoundToMultiple = 10;
        public int nonHPMinVisualMax = 80;

        [Tooltip("Visual headroom just for the HP bar.")]
        [Range(0f, 0.25f)] public float hpHeadroom = 0.05f;
        public int hpRoundToMultiple = 10;
        public int hpMinVisualMax = 200;

        private MechBuild _build;
        private List<Matrix> _matrices = new List<Matrix>();
        private List<MechPart> _rightArms = new List<MechPart>();
        private List<MechPart> _leftArms = new List<MechPart>();
        private List<MechPart> _lowers = new List<MechPart>();

        private int _iMatrix = 0, _iRA = 0, _iLA = 0, _iIN = 0;

        // 0 = Right Arm, 1 = Left Arm, 2 = Lower (IN), -1 = none
        private int _selectedMoveIndex = 0;

        private void Awake()
        {
            if (matrixCatalog == null || partCatalog == null)
            {
                Debug.LogError("[EditorController] Assign MatrixCatalog and PartCatalog.");
                enabled = false; return;
            }

            matrixCatalog.Init();
            partCatalog.Init();

            _build = BuildService.LoadOrNull();
            if (_build == null)
            {
                _build = PartCodeUtil.BuildForFamily("01");
                BuildService.Save(_build);
            }

            if (autoConfigureStatMaxFromCatalogs) AutoConfigureStatMaxFromCatalogs();
            else ConfigureStatBarRanges();

            RebuildListsForCurrentMatrix();
            SyncIndicesFromBuild();

            // First load: build UI and let it auto-select a valid move
            ApplyBuildToUI(false);

            // Wire arrows
            if (matrixPrevButton) matrixPrevButton.onClick.AddListener(delegate { CycleMatrix(-1); });
            if (matrixNextButton) matrixNextButton.onClick.AddListener(delegate { CycleMatrix(+1); });
            if (rightArmPrevButton) rightArmPrevButton.onClick.AddListener(delegate { CycleRA(-1); });
            if (rightArmNextButton) rightArmNextButton.onClick.AddListener(delegate { CycleRA(+1); });
            if (leftArmPrevButton) leftArmPrevButton.onClick.AddListener(delegate { CycleLA(-1); });
            if (leftArmNextButton) leftArmNextButton.onClick.AddListener(delegate { CycleLA(+1); });
            if (lowerPrevButton) lowerPrevButton.onClick.AddListener(delegate { CycleIN(-1); });
            if (lowerNextButton) lowerNextButton.onClick.AddListener(delegate { CycleIN(+1); });

            if (saveButton) saveButton.onClick.AddListener(SaveCurrentBuild);
            if (testBattleButton) testBattleButton.onClick.AddListener(GoToBattleWithCurrentBuild);
        }

        private void CycleMatrix(int dir)
        {
            if (_matrices.Count == 0) return;
            _iMatrix = Wrap(_iMatrix + dir, _matrices.Count);
            _build.matrixId = _matrices[_iMatrix].matrixCode;

            RebuildListsForCurrentMatrix();
            SyncIndicesFromBuild();

            ApplyBuildToUI(true);
            OnSelectMove(_selectedMoveIndex);
        }

        private void CycleRA(int dir)
        {
            if (_rightArms.Count == 0) return;
            _iRA = Wrap(_iRA + dir, _rightArms.Count);
            _build.rightArmId = _rightArms[_iRA].partCode;

            ApplyBuildToUI(true);
            OnSelectMove(0);
        }

        private void CycleLA(int dir)
        {
            if (_leftArms.Count == 0) return;
            _iLA = Wrap(_iLA + dir, _leftArms.Count);
            _build.leftArmId = _leftArms[_iLA].partCode;

            ApplyBuildToUI(true);
            OnSelectMove(1);
        }

        private void CycleIN(int dir)
        {
            if (_lowers.Count == 0) return;
            _iIN = Wrap(_iIN + dir, _lowers.Count);
            _build.lowerBodyId = _lowers[_iIN].partCode;

            ApplyBuildToUI(true);
            OnSelectMove(2);
        }

        private static int Wrap(int i, int count)
        {
            if (count <= 0) return 0;
            if (i < 0) return (i % count + count) % count;
            if (i >= count) return i % count;
            return i;
        }

        private void RebuildListsForCurrentMatrix()
        {
            _matrices = matrixCatalog.matrices
                .Where(m => m != null && !string.IsNullOrEmpty(m.matrixCode))
                .OrderBy(m => m.matrixCode).ToList();

            string family = PartCodeUtil.FamilyOf(_build.matrixId);
            if (string.IsNullOrEmpty(family)) family = "01";

            IEnumerable<MechPart> all = partCatalog.parts.Where(p => p != null && !string.IsNullOrEmpty(p.partCode));

            _rightArms = all.Where(p => p.partCode.StartsWith("RA") &&
                                    (!lockByFamily || PartCodeUtil.FamilyOf(p.partCode) == family))
                            .OrderBy(p => p.partCode).ToList();

            _leftArms = all.Where(p => p.partCode.StartsWith("LA") &&
                                    (!lockByFamily || PartCodeUtil.FamilyOf(p.partCode) == family))
                            .OrderBy(p => p.partCode).ToList();

            _lowers = all.Where(p => p.partCode.StartsWith("IN") &&
                                    (!lockByFamily || PartCodeUtil.FamilyOf(p.partCode) == family))
                            .OrderBy(p => p.partCode).ToList();
        }

        private void SyncIndicesFromBuild()
        {
            _iMatrix = Mathf.Max(0, _matrices.FindIndex(m => m.matrixCode == _build.matrixId));
            _iRA = Mathf.Max(0, _rightArms.FindIndex(p => p.partCode == _build.rightArmId));
            _iLA = Mathf.Max(0, _leftArms.FindIndex(p => p.partCode == _build.leftArmId));
            _iIN = Mathf.Max(0, _lowers.FindIndex(p => p.partCode == _build.lowerBodyId));

            if (_rightArms.Count > 0 && _iRA >= _rightArms.Count) _iRA = 0;
            if (_leftArms.Count > 0 && _iLA >= _leftArms.Count) _iLA = 0;
            if (_lowers.Count > 0 && _iIN >= _lowers.Count) _iIN = 0;

            if (_matrices.Count > 0) _build.matrixId = _matrices[_iMatrix].matrixCode;
            if (_rightArms.Count > 0) _build.rightArmId = _rightArms[_iRA].partCode;
            if (_leftArms.Count > 0) _build.leftArmId = _leftArms[_iLA].partCode;
            if (_lowers.Count > 0) _build.lowerBodyId = _lowers[_iIN].partCode;
        }

        /// <summary>
        /// Rebuild labels, paper doll sprites, stat bars, and move panel.
        /// </summary>
        private void ApplyBuildToUI(bool preserveSelection)
        {
            Matrix m = matrixCatalog.Get(_build.matrixId);
            MechPart ra = partCatalog.Get(_build.rightArmId);
            MechPart la = partCatalog.Get(_build.leftArmId);
            MechPart lb = partCatalog.Get(_build.lowerBodyId);

            if (matrixLabel) matrixLabel.text = FormatLabel(m != null ? m.matrixCode : null, m != null ? m.matrixName : null);
            if (rightArmLabel) rightArmLabel.text = FormatLabel(ra != null ? ra.partCode : null, ra != null ? ra.partName : null);
            if (leftArmLabel) leftArmLabel.text = FormatLabel(la != null ? la.partCode : null, la != null ? la.partName : null);
            if (lowerLabel) lowerLabel.text = FormatLabel(lb != null ? lb.partCode : null, lb != null ? lb.partName : null);

            SetImage(imgMatrix, TryGetSprite(m));
            SetImage(imgRightArm, TryGetSprite(ra));
            SetImage(imgLeftArm, TryGetSprite(la));
            SetImage(imgLower, TryGetSprite(lb));

            StatBlock totals = CalculateCurrentStats();
            UpdateStatBars(totals);

            if (preserveSelection)
            {
                if (_selectedMoveIndex == 0 && HasMove(ra)) ShowMoveFrom(ra);
                else if (_selectedMoveIndex == 1 && HasMove(la)) ShowMoveFrom(la);
                else if (_selectedMoveIndex == 2 && HasMove(lb)) ShowMoveFrom(lb);
                else AutoSelectFirstAvailableMove(ra, la, lb);
            }
            else
            {
                AutoSelectFirstAvailableMove(ra, la, lb);
            }

            PreviewSelectedMoveEffect();
        }

        private string FormatLabel(string code, string name)
        {
            if (!string.IsNullOrEmpty(name)) return name;
            if (!string.IsNullOrEmpty(code)) return code;
            return "-";
        }

        private void AutoSelectFirstAvailableMove(MechPart ra, MechPart la, MechPart lb)
        {
            if (HasMove(ra)) { _selectedMoveIndex = 0; ShowMoveFrom(ra); PreviewSelectedMoveEffect(); return; }
            if (HasMove(la)) { _selectedMoveIndex = 1; ShowMoveFrom(la); PreviewSelectedMoveEffect(); return; }
            if (HasMove(lb)) { _selectedMoveIndex = 2; ShowMoveFrom(lb); PreviewSelectedMoveEffect(); return; }
            _selectedMoveIndex = -1;
            SetMovePanel(emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveDescription);
            PreviewSelectedMoveEffect();
        }

        private bool HasMove(MechPart p) { return p != null && p.moves != null && p.moves.Count > 0; }

        private void OnSelectMove(int index)
        {
            _selectedMoveIndex = index;

            MechPart ra = partCatalog.Get(_build.rightArmId);
            MechPart la = partCatalog.Get(_build.leftArmId);
            MechPart lb = partCatalog.Get(_build.lowerBodyId);

            if (index == 0 && HasMove(ra)) ShowMoveFrom(ra);
            else if (index == 1 && HasMove(la)) ShowMoveFrom(la);
            else if (index == 2 && HasMove(lb)) ShowMoveFrom(lb);
            else SetMovePanel(emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveDescription);

            PreviewSelectedMoveEffect();
        }

        private void ShowMoveFrom(MechPart part)
        {
            if (!HasMove(part))
            {
                SetMovePanel(emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveLabel, emptyMoveDescription);
                return;
            }

            MoveDefinition mv = part.moves[0];
            string typeDisplay = string.IsNullOrEmpty(mv.damageType) ? emptyMoveLabel : mv.damageType.ToUpperInvariant();

            string effectText;
            if (!string.IsNullOrWhiteSpace(mv.effect))
                effectText = mv.effect;
            else if (usePartRawDataAsDescFallback && !string.IsNullOrWhiteSpace(part.rawData))
                effectText = part.rawData;
            else
                effectText = emptyMoveDescription;

            SetMovePanel(
                string.IsNullOrEmpty(mv.moveName) ? emptyMoveLabel : mv.moveName,
                string.IsNullOrEmpty(part.partName) ? emptyMoveLabel : part.partName,
                mv.baseDamage.ToString(),
                typeDisplay,
                mv.targetType.ToString().ToUpperInvariant(),
                effectText
            );
        }

        private void SetMovePanel(string name, string part, string dmg, string type, string tgt, string desc)
        {
            if (moveNameText) moveNameText.text = name;
            if (movePartText) movePartText.text = part;
            if (moveDamageText) moveDamageText.text = dmg;
            if (moveTypeText) moveTypeText.text = type;
            if (moveTargetText) moveTargetText.text = tgt;
            if (moveDescText) moveDescText.text = desc;
        }

        // --- PaperDoll Helpers -------------------------------------------------

        private void SetImage(Image img, Sprite s)
        {
            if (!img) return;
            img.sprite = s;
            img.preserveAspect = true;
            img.enabled = s != null;
        }

        private Sprite TryGetSprite(object obj)
        {
            if (obj == null) return null;

            Sprite s = GetSpriteFromMember(obj, "editorSprite")
                    ?? GetSpriteFromMember(obj, "paperDollSprite")
                    ?? GetSpriteFromMember(obj, "sprite")
                    ?? GetSpriteFromMember(obj, "icon");
            if (s) return s;

            string key = null;
            if (obj is Matrix mm) key = mm.matrixCode;
            else if (obj is MechPart pp) key = pp.partCode;

            if (!string.IsNullOrEmpty(key))
            {
                var spr = Resources.Load<Sprite>($"PaperDoll/{key}");
                if (!spr) spr = Resources.Load<Sprite>(key);
                return spr;
            }

            return null;
        }

        private Sprite GetSpriteFromMember(object instance, string memberName)
        {
            if (instance == null) return null;
            var t = instance.GetType();

            var f = t.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
            {
                return (Sprite)f.GetValue(instance);
            }

            var p = t.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType))
            {
                return (Sprite)p.GetValue(instance, null);
            }

            return null;
        }

        // --- Stats -------------------------------------------------------------

        private StatBlock CalculateCurrentStats()
        {
            StatBlock total = new StatBlock();
            Matrix m = matrixCatalog.Get(_build.matrixId);
            MechPart ra = partCatalog.Get(_build.rightArmId);
            MechPart la = partCatalog.Get(_build.leftArmId);
            MechPart lb = partCatalog.Get(_build.lowerBodyId);

            Add(total, m != null ? m.baseStats : null);
            Add(total, ra != null ? ra.statModifiers : null);
            Add(total, la != null ? la.statModifiers : null);
            Add(total, lb != null ? lb.statModifiers : null);
            return total;
        }

        private void Add(StatBlock dst, StatBlock src)
        {
            if (src == null) return;
            dst.HP += src.HP; dst.ATK += src.ATK; dst.DEF += src.DEF;
            dst.ENG += src.ENG; dst.SPD += src.SPD; dst.SYS += src.SYS;
        }

        private void UpdateStatBars(StatBlock s)
        {
            // Use current totals to compute visual maxes (idempotent)
            EnsureVisualMaxes(s);

            SetBar(hpBar, hpValueText, s.HP);
            SetBar(atkBar, atkValueText, s.ATK);
            SetBar(defBar, defValueText, s.DEF);
            SetBar(engBar, engValueText, s.ENG);
            SetBar(spdBar, spdValueText, s.SPD);
            SetBar(sysBar, sysValueText, s.SYS);

            if (logStatsOnChange)
                Debug.Log($"[EditorController] Stats => HP:{s.HP} ATK:{s.ATK} DEF:{s.DEF} ENG:{s.ENG} SPD:{s.SPD} SYS:{s.SYS}");
        }

        // Compute visual maxes from current totals (no compounding)
        private void EnsureVisualMaxes(StatBlock totals)
        {
            if (hpBar)
            {
                float hpBase = Mathf.Max(hpMinVisualMax, totals.HP);
                hpBar.maxValue = MakeVisualMaxFromBase(hpBase, hpHeadroom, hpRoundToMultiple);
            }

            if (!unifyNonHPBars)
            {
                if (atkBar) atkBar.maxValue = MakeVisualMaxFromBase(Mathf.Max(nonHPMinVisualMax, totals.ATK), nonHPHeadroom, nonHPRoundToMultiple);
                if (defBar) defBar.maxValue = MakeVisualMaxFromBase(Mathf.Max(nonHPMinVisualMax, totals.DEF), nonHPHeadroom, nonHPRoundToMultiple);
                if (engBar) engBar.maxValue = MakeVisualMaxFromBase(Mathf.Max(nonHPMinVisualMax, totals.ENG), nonHPHeadroom, nonHPRoundToMultiple);
                if (spdBar) spdBar.maxValue = MakeVisualMaxFromBase(Mathf.Max(nonHPMinVisualMax, totals.SPD), nonHPHeadroom, nonHPRoundToMultiple);
                if (sysBar) sysBar.maxValue = MakeVisualMaxFromBase(Mathf.Max(nonHPMinVisualMax, totals.SYS), nonHPHeadroom, nonHPRoundToMultiple);
            }
            else
            {
                float nonHPBase = Mathf.Max(
                    nonHPMinVisualMax,
                    totals.ATK, totals.DEF, totals.ENG, totals.SPD, totals.SYS
                );
                float visual = MakeVisualMaxFromBase(nonHPBase, nonHPHeadroom, nonHPRoundToMultiple);

                if (atkBar) atkBar.maxValue = visual;
                if (defBar) defBar.maxValue = visual;
                if (engBar) engBar.maxValue = visual;
                if (spdBar) spdBar.maxValue = visual;
                if (sysBar) sysBar.maxValue = visual;
            }
        }

        private float MakeVisualMaxFromBase(float baseVal, float headroom, int roundToMultiple)
        {
            float v = baseVal * (1f + Mathf.Clamp01(headroom));
            if (roundToMultiple > 1)
                v = Mathf.Ceil(v / roundToMultiple) * roundToMultiple;
            return Mathf.Max(1f, v);
        }

        private void SetBar(Slider bar, Text valueText, int value)
        {
            if (!bar) return;
            bar.value = Mathf.Clamp(value, 0, Mathf.Max(1, (int)bar.maxValue));
            if (valueText) valueText.text = value.ToString();
        }

        private void ConfigureStatBarRanges()
        {
            if (hpBar) hpBar.maxValue = Mathf.Max(1, hpMax);
            if (atkBar) atkBar.maxValue = Mathf.Max(1, atkMax);
            if (defBar) defBar.maxValue = Mathf.Max(1, defMax);
            if (engBar) engBar.maxValue = Mathf.Max(1, engMax);
            if (spdBar) spdBar.maxValue = Mathf.Max(1, spdMax);
            if (sysBar) sysBar.maxValue = Mathf.Max(1, sysMax);
        }

        private void AutoConfigureStatMaxFromCatalogs()
        {
            int maxHP = 1, maxATK = 1, maxDEF = 1, maxENG = 1, maxSPD = 1, maxSYS = 1;

            foreach (Matrix m in matrixCatalog.matrices)
            {
                if (m == null || m.baseStats == null) continue;
                maxHP = Mathf.Max(maxHP, m.baseStats.HP);
                maxATK = Mathf.Max(maxATK, m.baseStats.ATK);
                maxDEF = Mathf.Max(maxDEF, m.baseStats.DEF);
                maxENG = Mathf.Max(maxENG, m.baseStats.ENG);
                maxSPD = Mathf.Max(maxSPD, m.baseStats.SPD);
                maxSYS = Mathf.Max(maxSYS, m.baseStats.SYS);
            }

            foreach (MechPart p in partCatalog.parts)
            {
                if (p == null || p.statModifiers == null) continue;
                maxHP = Mathf.Max(maxHP, p.statModifiers.HP);
                maxATK = Mathf.Max(maxATK, p.statModifiers.ATK);
                maxDEF = Mathf.Max(maxDEF, p.statModifiers.DEF);
                maxENG = Mathf.Max(maxENG, p.statModifiers.ENG);
                maxSPD = Mathf.Max(maxSPD, p.statModifiers.SPD);
                maxSYS = Mathf.Max(maxSYS, p.statModifiers.SYS);
            }

            if (hpBar) hpBar.maxValue = Mathf.Max(1, maxHP * 2);
            if (atkBar) atkBar.maxValue = Mathf.Max(1, maxATK * 2);
            if (defBar) defBar.maxValue = Mathf.Max(1, maxDEF * 2);
            if (engBar) engBar.maxValue = Mathf.Max(1, maxENG * 2);
            if (spdBar) spdBar.maxValue = Mathf.Max(1, maxSPD * 2);
            if (sysBar) sysBar.maxValue = Mathf.Max(1, maxSYS * 2);
        }

        private struct StatDelta { public int HP, ATK, DEF, ENG, SPD, SYS; }

        public void PreviewSelectedMoveEffect()
        {
            MoveDefinition mv = GetCurrentSelectedMove().mv;

            StatBlock baseStats = CalculateCurrentStats();
            if (mv == null) { UpdateStatBars(baseStats); return; }

            StatDelta delta = ParseEffectToDelta(mv.effect);
            StatBlock preview = ApplyDelta(baseStats, delta);
            UpdateStatBars(preview);
        }

        private struct MoveTuple { public MoveDefinition mv; public MechPart part; }
        private MoveTuple GetCurrentSelectedMove()
        {
            MechPart ra = partCatalog.Get(_build.rightArmId);
            MechPart la = partCatalog.Get(_build.leftArmId);
            MechPart lb = partCatalog.Get(_build.lowerBodyId);

            if (_selectedMoveIndex == 0 && ra != null && ra.moves != null && ra.moves.Count > 0) return new MoveTuple { mv = ra.moves[0], part = ra };
            if (_selectedMoveIndex == 1 && la != null && la.moves != null && la.moves.Count > 0) return new MoveTuple { mv = la.moves[0], part = la };
            if (_selectedMoveIndex == 2 && lb != null && lb.moves != null && lb.moves.Count > 0) return new MoveTuple { mv = lb.moves[0], part = lb };
            return new MoveTuple { mv = null, part = null };
        }

        private StatDelta ParseEffectToDelta(string effect)
        {
            StatDelta d = new StatDelta();
            if (string.IsNullOrWhiteSpace(effect))
            {
                if (previewPlusOneATKIfNoEffect) d.ATK += 1;
                return d;
            }

            string[] pieces = effect.Split(';');
            foreach (string raw in pieces)
            {
                string tok = raw.Trim();
                if (tok.Length == 0) continue;

                int sign = tok.StartsWith("-") ? -1 : (tok.StartsWith("+") ? 1 : 0);
                if (sign == 0) continue;

                string body = tok.Substring(1).Trim();
                int amount = 1; string stat;
                string[] seg = body.Split(' ');
                if (seg.Length == 1) stat = seg[0].ToUpperInvariant();
                else
                {
                    int n;
                    if (int.TryParse(seg[0], out n)) { amount = n; stat = seg[1].ToUpperInvariant(); }
                    else continue;
                }

                int val = sign * amount;
                switch (stat)
                {
                    case "HP": d.HP += val; break;
                    case "ATK": d.ATK += val; break;
                    case "DEF": d.DEF += val; break;
                    case "ENG": d.ENG += val; break;
                    case "SPD": d.SPD += val; break;
                    case "SYS": d.SYS += val; break;
                }
            }
            return d;
        }

        private StatBlock ApplyDelta(StatBlock s, StatDelta d)
        {
            StatBlock r = new StatBlock();
            r.HP = s.HP + d.HP;
            r.ATK = s.ATK + d.ATK;
            r.DEF = s.DEF + d.DEF;
            r.ENG = s.ENG + d.ENG;
            r.SPD = s.SPD + d.SPD;
            r.SYS = s.SYS + d.SYS;
            return r;
        }

        private void SaveCurrentBuild()
        {
            BuildService.Save(_build);
            Debug.Log("[EditorController] Build saved.");
        }

        private void GoToBattleWithCurrentBuild()
        {
            BuildService.Save(_build);
            if (!string.IsNullOrEmpty(battleSceneName))
                SceneManager.LoadScene(battleSceneName);
        }
    }
}
