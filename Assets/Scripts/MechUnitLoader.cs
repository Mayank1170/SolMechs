using UnityEngine;
using MechBattle;

public class MechUnitLoader : MonoBehaviour
{
    [Header("Load order")]
    [Tooltip("If true, try to assemble from the last saved MechBuild first, using the catalogs below.")]
    public bool useSavedBuildFirst = true;

    [Header("Catalogs (required if useSavedBuildFirst = true)")]
    public MatrixCatalog matrixCatalog;
    public PartCatalog partCatalog;

    [Header("Fallback (used if no valid saved build)")]
    public Matrix matrix;
    public MechPart rightArm;
    public MechPart leftArm;
    public MechPart lower;

    /// <summary>
    /// Returns a MechUnit assembled from the last saved build (if available & valid).
    /// Falls back to the manually assigned SOs otherwise.
    /// </summary>
    public MechUnit GetUnitData()
    {
        // 1) Try the saved build from the editor
        if (useSavedBuildFirst)
        {
            var unitFromSaved = TryAssembleFromSavedBuild();
            if (unitFromSaved != null)
            {
                Debug.Log($"[MechUnitLoader:{gameObject.name}] Loaded unit from saved build.");
                return unitFromSaved;
            }
        }

        // 2) Fallback to manually assigned parts
        if (matrix == null || rightArm == null || leftArm == null || lower == null)
        {
            Debug.LogError(
                $"❌ Missing fallback mech parts on {gameObject.name}!\n" +
                $"Matrix: {matrix?.matrixName ?? "null"}, RightArm: {rightArm?.partName ?? "null"}, " +
                $"LeftArm: {leftArm?.partName ?? "null"}, Lower: {lower?.partName ?? "null"}"
            );
            return null;
        }

        var fallbackUnit = new MechUnit
        {
            Name = matrix.matrixName,
            chassis = matrix,
            matrixHP = matrix.baseStats?.HP ?? 100
        };

        fallbackUnit.modules[ModuleSlot.RightArm] = rightArm.ToModuleData(ModuleSlot.RightArm);
        fallbackUnit.modules[ModuleSlot.LeftArm] = leftArm.ToModuleData(ModuleSlot.LeftArm);
        fallbackUnit.modules[ModuleSlot.LowerBody] = lower.ToModuleData(ModuleSlot.LowerBody);

        fallbackUnit.partStatuses[ModuleSlot.RightArm] = new PartStatus
        {
            partName = rightArm.partName,
            maxHP = rightArm.statModifiers?.HP ?? 50,
            currentHP = rightArm.statModifiers?.HP ?? 50
        };
        fallbackUnit.partStatuses[ModuleSlot.LeftArm] = new PartStatus
        {
            partName = leftArm.partName,
            maxHP = leftArm.statModifiers?.HP ?? 50,
            currentHP = leftArm.statModifiers?.HP ?? 50
        };
        fallbackUnit.partStatuses[ModuleSlot.LowerBody] = new PartStatus
        {
            partName = lower.partName,
            maxHP = lower.statModifiers?.HP ?? 50,
            currentHP = lower.statModifiers?.HP ?? 50
        };

        Debug.Log($"[MechUnitLoader:{gameObject.name}] Loaded unit from fallback SOs.");
        return fallbackUnit;
    }

    // --- helpers ---

    private MechUnit TryAssembleFromSavedBuild()
    {
        var saved = BuildService.LoadOrNull();
        if (saved == null)
        {
            Debug.Log($"[MechUnitLoader:{gameObject.name}] No saved build found.");
            return null;
        }

        if (matrixCatalog == null || partCatalog == null)
        {
            Debug.LogWarning($"[MechUnitLoader:{gameObject.name}] Catalogs not set; cannot assemble from saved build.");
            return null;
        }

        var m = matrixCatalog.Get(saved.matrixId);
        var ra = partCatalog.Get(saved.rightArmId);
        var la = partCatalog.Get(saved.leftArmId);
        var lb = partCatalog.Get(saved.lowerBodyId);

        if (m == null || ra == null || la == null || lb == null)
        {
            Debug.LogWarning(
                $"[MechUnitLoader:{gameObject.name}] Saved build contains invalid codes. " +
                $"matrixId={saved.matrixId}, RA={saved.rightArmId}, LA={saved.leftArmId}, IN={saved.lowerBodyId}"
            );
            return null;
        }

        var unit = new MechUnit
        {
            Name = m.matrixName,
            chassis = m,
            matrixHP = m.baseStats?.HP ?? 100
        };

        unit.modules[ModuleSlot.RightArm] = ra.ToModuleData(ModuleSlot.RightArm);
        unit.modules[ModuleSlot.LeftArm] = la.ToModuleData(ModuleSlot.LeftArm);
        unit.modules[ModuleSlot.LowerBody] = lb.ToModuleData(ModuleSlot.LowerBody);

        unit.partStatuses[ModuleSlot.RightArm] = new PartStatus
        {
            partName = ra.partName,
            maxHP = ra.statModifiers?.HP ?? 50,
            currentHP = ra.statModifiers?.HP ?? 50
        };
        unit.partStatuses[ModuleSlot.LeftArm] = new PartStatus
        {
            partName = la.partName,
            maxHP = la.statModifiers?.HP ?? 50,
            currentHP = la.statModifiers?.HP ?? 50
        };
        unit.partStatuses[ModuleSlot.LowerBody] = new PartStatus
        {
            partName = lb.partName,
            maxHP = lb.statModifiers?.HP ?? 50,
            currentHP = lb.statModifiers?.HP ?? 50
        };

        return unit;
    }
}
