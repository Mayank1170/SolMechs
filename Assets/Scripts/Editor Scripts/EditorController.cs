using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MechBattle
{
    /// <summary>
    /// Mech editor with selectable moves:
    /// - Loads/saves MechBuild via BuildService
    /// - Populates dropdowns (Matrix / RA / LA / IN) with optional family lock
    /// - Paper-doll preview (sprites optional; TryGetSprite returns null unless you add fields)
    /// - Aggregated stats (HP, ATK, DEF, ENG, SPD, SYS)
    /// - Move list: 3 buttons (RA -> LA -> IN). Clicking shows full move details.
    /// </summary>
    public class EditorController : MonoBehaviour
    {
        [Header("Catalogs & Options")]
        public MatrixCatalog matrixCatalog;
        public PartCatalog partCatalog;
        public bool lockByFamily = true;

        [Header("Selection UI")]
        public Dropdown matrixDropdown;
        public Dropdown rightArmDropdown;
        public Dropdown leftArmDropdown;
        public Dropdown lowerDropdown;
        public Button saveButton;

        [Header("Paper Doll (optional sprites)")]
        public Image imgMatrix;
        public Image imgRightArm;
        public Image imgLeftArm;
        public Image imgLower;

        [Header("Stats UI (optional)")]
        public Slider hpBar, atkBar, defBar, engBar, spdBar, sysBar;

        [Header("Move Detail Panel")]
        public Text moveNameText;    // ex: CRUSH GRIP
        public Text movePartText;    // ex: TITAN CLAW
        public Text moveDamageText;  // ex: 50
        public Text moveTypeText;    // PHYSICAL / ENERGY
        public Text moveTargetText;  // SINGLE / SELF / AOE
        public Text moveDescText;    // MoveDefinition.effect or part.rawData

        [Header("Move List (click to select)")]
        public Button move1Button;   // Right Arm
        public Button move2Button;   // Left Arm
        public Button move3Button;   // Lower Body

        // runtime
        private MechBuild _build;
        private readonly List<Matrix> _matrices = new();
        private List<MechPart> _rightArms = new();
        private List<MechPart> _leftArms = new();
        private List<MechPart> _lowers = new();

        // current selection (0 = RA, 1 = LA, 2 = IN, -1 = none)
        private int _selectedMoveIndex = 0;

        private void Awake()
        {
            if (matrixCatalog == null || partCatalog == null)
            {
                Debug.LogError("[EditorController] Assign MatrixCatalog and PartCatalog in Inspector.");
                enabled = false;
                return;
            }

            matrixCatalog.Init();
            partCatalog.Init();

            _build = BuildService.LoadOrNull();
            if (_build == null)
            {
                _build = PartCodeUtil.BuildForFamily("01");
                BuildService.Save(_build);
            }

            PopulateMatrixDropdown();
            PopulateModuleDropdowns();
            ApplyBuildToUI();

            // Selection events
            if (matrixDropdown) matrixDropdown.onValueChanged.AddListener(OnMatrixChanged);
            if (rightArmDropdown) rightArmDropdown.onValueChanged.AddListener(OnRightArmChanged);
            if (leftArmDropdown) leftArmDropdown.onValueChanged.AddListener(OnLeftArmChanged);
            if (lowerDropdown) lowerDropdown.onValueChanged.AddListener(OnLowerChanged);
            if (saveButton) saveButton.onClick.AddListener(SaveCurrentBuild);

            // Move list events
            if (move1Button) move1Button.onClick.AddListener(() => OnSelectMove(0));
            if (move2Button) move2Button.onClick.AddListener(() => OnSelectMove(1));
            if (move3Button) move3Button.onClick.AddListener(() => OnSelectMove(2));
        }

        // ---------------------------
        // Populate
        // ---------------------------
        private void PopulateMatrixDropdown()
        {
            _matrices.Clear();
            _matrices.AddRange(
                matrixCatalog.matrices
                    .Where(m => m && !string.IsNullOrEmpty(m.matrixCode))
                    .OrderBy(m => m.matrixCode)
            );

            if (!matrixDropdown) return;

            matrixDropdown.ClearOptions();
            matrixDropdown.AddOptions(_matrices.Select(m => $"{m.matrixCode} - {m.matrixName}").ToList());

            int idx = _matrices.FindIndex(m => m.matrixCode == _build.matrixId);
            if (idx < 0) idx = 0;
            matrixDropdown.value = idx;
            matrixDropdown.RefreshShownValue();

            _build.matrixId = _matrices[matrixDropdown.value].matrixCode;
        }

        private void PopulateModuleDropdowns()
        {
            string family = PartCodeUtil.FamilyOf(_build.matrixId) ?? "01";

            _rightArms = FilterByPrefixAndFamily("RA", family);
            _leftArms = FilterByPrefixAndFamily("LA", family);
            _lowers = FilterByPrefixAndFamily("IN", family);

            _build.rightArmId = PopulateDropdown(rightArmDropdown, _rightArms, _build.rightArmId);
            _build.leftArmId = PopulateDropdown(leftArmDropdown, _leftArms, _build.leftArmId);
            _build.lowerBodyId = PopulateDropdown(lowerDropdown, _lowers, _build.lowerBodyId);
        }

        private List<MechPart> FilterByPrefixAndFamily(string prefix, string family)
        {
            var all = partCatalog.parts.Where(p => p && !string.IsNullOrEmpty(p.partCode));
            var filtered = all.Where(p => p.partCode.StartsWith(prefix));
            if (lockByFamily) filtered = filtered.Where(p => PartCodeUtil.FamilyOf(p.partCode) == family);
            return filtered.OrderBy(p => p.partCode).ToList();
        }

        private string PopulateDropdown(Dropdown dd, List<MechPart> parts, string currentId)
        {
            if (!dd) return currentId;

            dd.ClearOptions();
            dd.AddOptions(parts.Select(p => $"{p.partCode} - {p.partName}").ToList());

            int idx = parts.FindIndex(p => p.partCode == currentId);
            if (idx < 0) idx = 0;
            dd.value = idx;
            dd.RefreshShownValue();

            return parts.Count > 0 ? parts[dd.value].partCode : currentId;
        }

        // ---------------------------
        // Apply to UI
        // ---------------------------
        private void ApplyBuildToUI()
        {
            var m = matrixCatalog.Get(_build.matrixId);
            var ra = partCatalog.Get(_build.rightArmId);
            var la = partCatalog.Get(_build.leftArmId);
            var lb = partCatalog.Get(_build.lowerBodyId);

            // Paper-doll (sprites optional)
            if (imgMatrix) imgMatrix.sprite = TryGetSprite(m);
            if (imgRightArm) imgRightArm.sprite = TryGetSprite(ra);
            if (imgLeftArm) imgLeftArm.sprite = TryGetSprite(la);
            if (imgLower) imgLower.sprite = TryGetSprite(lb);

            // Stats
            var totals = CalculateCurrentStats();
            UpdateStatBars(totals);

            // Move list + auto-select first available
            SetupMoveButtons(ra, la, lb);
            AutoSelectFirstAvailableMove(ra, la, lb);
        }

        private void SetupMoveButtons(MechPart ra, MechPart la, MechPart lb)
        {
            SetupMoveButton(move1Button, ra, "—"); // RA
            SetupMoveButton(move2Button, la, "—"); // LA
            SetupMoveButton(move3Button, lb, "—"); // IN
            UpdateMoveButtonsInteractable();
        }

        private void SetupMoveButton(Button btn, MechPart part, string emptyLabel)
        {
            if (!btn) return;
            var txt = btn.GetComponentInChildren<Text>();
            string label = emptyLabel;

            if (part?.moves != null && part.moves.Count > 0 && !string.IsNullOrEmpty(part.moves[0].moveName))
                label = part.moves[0].moveName;

            if (txt) txt.text = label;
        }

        private void AutoSelectFirstAvailableMove(MechPart ra, MechPart la, MechPart lb)
        {
            if (HasMove(ra)) { _selectedMoveIndex = 0; ShowMoveFrom(ra); return; }
            if (HasMove(la)) { _selectedMoveIndex = 1; ShowMoveFrom(la); return; }
            if (HasMove(lb)) { _selectedMoveIndex = 2; ShowMoveFrom(lb); return; }

            _selectedMoveIndex = -1;
            SetMovePanel("-", "-", "-", "-", "-", "-");
            UpdateMoveButtonsInteractable();
        }

        private bool HasMove(MechPart p) => p?.moves != null && p.moves.Count > 0;

        private void OnSelectMove(int index)
        {
            _selectedMoveIndex = index;

            var ra = partCatalog.Get(_build.rightArmId);
            var la = partCatalog.Get(_build.leftArmId);
            var lb = partCatalog.Get(_build.lowerBodyId);

            if (index == 0 && HasMove(ra)) ShowMoveFrom(ra);
            else if (index == 1 && HasMove(la)) ShowMoveFrom(la);
            else if (index == 2 && HasMove(lb)) ShowMoveFrom(lb);
            else SetMovePanel("-", "-", "-", "-", "-", "-");

            UpdateMoveButtonsInteractable();
        }

        private void UpdateMoveButtonsInteractable()
        {
            if (move1Button) move1Button.interactable = _selectedMoveIndex != 0;
            if (move2Button) move2Button.interactable = _selectedMoveIndex != 1;
            if (move3Button) move3Button.interactable = _selectedMoveIndex != 2;
        }

        private void ShowMoveFrom(MechPart sourcePart)
        {
            if (!HasMove(sourcePart))
            {
                SetMovePanel("-", "-", "-", "-", "-", "-");
                return;
            }

            var mv = sourcePart.moves[0];
            var partName = sourcePart.partName?.ToUpperInvariant() ?? "-";
            string typeDisplay = string.IsNullOrEmpty(mv.damageType) ? "-" : mv.damageType.ToUpperInvariant();
            string effectText = !string.IsNullOrEmpty(mv.effect) ? mv.effect
                              : (!string.IsNullOrEmpty(sourcePart.rawData) ? sourcePart.rawData : "-");

            SetMovePanel(
                mv.moveName ?? "-",
                partName,
                mv.baseDamage.ToString(),
                typeDisplay,
                mv.targetType.ToString().ToUpperInvariant(),
                effectText
            );
        }

        // Fallback: return null until you add sprite fields to your ScriptableObjects.
        private Sprite TryGetSprite(object _)
        {
            return null;
        }

        // ---------------------------
        // Stats
        // ---------------------------
        private StatBlock CalculateCurrentStats()
        {
            var total = new StatBlock();

            var m = matrixCatalog.Get(_build.matrixId);
            var ra = partCatalog.Get(_build.rightArmId);
            var la = partCatalog.Get(_build.leftArmId);
            var lb = partCatalog.Get(_build.lowerBodyId);

            if (m?.baseStats != null) Add(total, m.baseStats);
            if (ra?.statModifiers != null) Add(total, ra.statModifiers);
            if (la?.statModifiers != null) Add(total, la.statModifiers);
            if (lb?.statModifiers != null) Add(total, lb.statModifiers);

            return total;

            void Add(StatBlock dst, StatBlock src)
            {
                dst.HP += src.HP;
                dst.ATK += src.ATK;
                dst.DEF += src.DEF;
                dst.ENG += src.ENG;
                dst.SPD += src.SPD;
                dst.SYS += src.SYS;
            }
        }

        private void UpdateStatBars(StatBlock s)
        {
            if (hpBar) { hpBar.maxValue = Mathf.Max(1, s.HP); hpBar.value = s.HP; }
            if (atkBar) { atkBar.maxValue = Mathf.Max(1, s.ATK); atkBar.value = s.ATK; }
            if (defBar) { defBar.maxValue = Mathf.Max(1, s.DEF); defBar.value = s.DEF; }
            if (engBar) { engBar.maxValue = Mathf.Max(1, s.ENG); engBar.value = s.ENG; }
            if (spdBar) { spdBar.maxValue = Mathf.Max(1, s.SPD); spdBar.value = s.SPD; }
            if (sysBar) { sysBar.maxValue = Mathf.Max(1, s.SYS); sysBar.value = s.SYS; }
        }

        // ---------------------------
        // Events
        // ---------------------------
        private void OnMatrixChanged(int idx)
        {
            if (idx < 0 || idx >= _matrices.Count) return;
            _build.matrixId = _matrices[idx].matrixCode;

            PopulateModuleDropdowns();
            ApplyBuildToUI();
        }

        private void OnRightArmChanged(int idx)
        {
            if (idx < 0 || idx >= _rightArms.Count) return;
            _build.rightArmId = _rightArms[idx].partCode;
            ApplyBuildToUI();
        }

        private void OnLeftArmChanged(int idx)
        {
            if (idx < 0 || idx >= _leftArms.Count) return;
            _build.leftArmId = _leftArms[idx].partCode;
            ApplyBuildToUI();
        }

        private void OnLowerChanged(int idx)
        {
            if (idx < 0 || idx >= _lowers.Count) return;
            _build.lowerBodyId = _lowers[idx].partCode;
            ApplyBuildToUI();
        }

        private void SaveCurrentBuild()
        {
            BuildService.Save(_build);
            Debug.Log("[EditorController] Build saved.");
        }

        // ---------------------------
        // UI helpers
        // ---------------------------
        private void SetMovePanel(string name, string part, string dmg, string type, string tgt, string desc)
        {
            if (moveNameText) moveNameText.text = name;
            if (movePartText) movePartText.text = part;
            if (moveDamageText) moveDamageText.text = dmg;
            if (moveTypeText) moveTypeText.text = type;
            if (moveTargetText) moveTargetText.text = tgt;
            if (moveDescText) moveDescText.text = desc;
        }
    }
}
