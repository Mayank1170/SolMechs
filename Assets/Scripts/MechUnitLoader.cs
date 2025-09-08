using UnityEngine;
using System.Collections.Generic;
using MechBattle;

public class MechUnitLoader : MonoBehaviour
{
    [Header("Load mode")]
    [Tooltip("If true, loads the last saved build from BuildService using catalogs. If false, uses the manual ScriptableObject fields below.")]
    public bool useSavedBuild = true;

    [Header("Catalogs (required when useSavedBuild = true)")]
    public MatrixCatalog matrixCatalog;
    public PartCatalog partCatalog;

    [Header("Manual assignment (fallback if no saved build)")]
    public Matrix matrix;
    public MechPart rightArm;
    public MechPart leftArm;
    public MechPart lower;

    // Expose the final assets the loader actually used (for UI sprites).
    [HideInInspector] public Matrix ResolvedMatrix;
    [HideInInspector] public MechPart ResolvedRightArm;
    [HideInInspector] public MechPart ResolvedLeftArm;
    [HideInInspector] public MechPart ResolvedLower;

    public MechUnit GetUnitData()
    {
        Matrix m = matrix;
        MechPart ra = rightArm;
        MechPart la = leftArm;
        MechPart lb = lower;

        if (useSavedBuild)
        {
            if (matrixCatalog != null) matrixCatalog.Init();
            if (partCatalog != null) partCatalog.Init();

            var saved = BuildService.LoadOrNull();
            if (saved != null && matrixCatalog != null && partCatalog != null)
            {
                var mTry = matrixCatalog.Get(saved.matrixId);
                var raTry = partCatalog.Get(saved.rightArmId);
                var laTry = partCatalog.Get(saved.leftArmId);
                var lbTry = partCatalog.Get(saved.lowerBodyId);

                if (mTry != null) m = mTry;
                if (raTry != null) ra = raTry;
                if (laTry != null) la = laTry;
                if (lbTry != null) lb = lbTry;
            }
            else
            {
                Debug.LogWarning("[MechUnitLoader] Saved build not found or catalogs missing. Falling back to manual assignment.");
            }
        }

        // Cache the actually used assets for other systems (UI).
        ResolvedMatrix = m;
        ResolvedRightArm = ra;
        ResolvedLeftArm = la;
        ResolvedLower = lb;

        if (m == null || ra == null || la == null || lb == null)
        {
            Debug.LogError($"❌ Missing mech part on {gameObject.name}! Matrix: {m?.matrixName ?? "null"}, RightArm: {ra?.partName ?? "null"}, LeftArm: {la?.partName ?? "null"}, Lower: {lb?.partName ?? "null"}");
            return null;
        }

        var unit = new MechUnit
        {
            Name = m.matrixName,
            chassis = m,
            matrixHP = m.baseStats?.HP ?? 100
        };

        // Modules
        unit.modules[ModuleSlot.RightArm] = ra.ToModuleData(ModuleSlot.RightArm);
        unit.modules[ModuleSlot.LeftArm] = la.ToModuleData(ModuleSlot.LeftArm);
        unit.modules[ModuleSlot.LowerBody] = lb.ToModuleData(ModuleSlot.LowerBody);

        // Part HP/buffs
        unit.partStatuses[ModuleSlot.RightArm] = new PartStatus
        {
            partName = ra.partName,
            maxHP = ra.statModifiers?.HP ?? 50,
            currentHP = ra.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };
        unit.partStatuses[ModuleSlot.LeftArm] = new PartStatus
        {
            partName = la.partName,
            maxHP = la.statModifiers?.HP ?? 50,
            currentHP = la.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };
        unit.partStatuses[ModuleSlot.LowerBody] = new PartStatus
        {
            partName = lb.partName,
            maxHP = lb.statModifiers?.HP ?? 50,
            currentHP = lb.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };

        return unit;
    }
}