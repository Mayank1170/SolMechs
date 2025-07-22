using UnityEngine;
using System.Collections.Generic;
using MechBattle;

public class MechUnitLoader : MonoBehaviour
{
    [Header("Assign Mech Parts")]
    public Matrix matrix;
    public MechPart rightArm;
    public MechPart leftArm;
    public MechPart lower;

    public MechUnit GetUnitData()
    {
        if (matrix == null || rightArm == null || leftArm == null || lower == null)
        {
            Debug.LogError($"❌ Missing mech part on {gameObject.name}! Matrix: {matrix?.matrixName ?? "null"}, RightArm: {rightArm?.partName ?? "null"}, LeftArm: {leftArm?.partName ?? "null"}, Lower: {lower?.partName ?? "null"}");
            return null;
        }

        var unit = new MechUnit
        {
            Name = matrix.matrixName,
            chassis = matrix,
            matrixHP = matrix.baseStats?.HP ?? 100 // Consider making configurable
        };

        unit.modules[ModuleSlot.RightArm] = rightArm.ToModuleData(ModuleSlot.RightArm);
        unit.modules[ModuleSlot.LeftArm] = leftArm.ToModuleData(ModuleSlot.LeftArm);
        unit.modules[ModuleSlot.LowerBody] = lower.ToModuleData(ModuleSlot.LowerBody);

        unit.partStatuses[ModuleSlot.RightArm] = new PartStatus
        {
            partName = rightArm.partName,
            maxHP = rightArm.statModifiers?.HP ?? 50,
            currentHP = rightArm.statModifiers?.HP ?? 50
        };
        unit.partStatuses[ModuleSlot.LeftArm] = new PartStatus
        {
            partName = leftArm.partName,
            maxHP = leftArm.statModifiers?.HP ?? 50,
            currentHP = leftArm.statModifiers?.HP ?? 50
        };
        unit.partStatuses[ModuleSlot.LowerBody] = new PartStatus
        {
            partName = lower.partName,
            maxHP = lower.statModifiers?.HP ?? 50,
            currentHP = lower.statModifiers?.HP ?? 50
        };

        return unit;
    }
}