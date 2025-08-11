// Assets/Scripts/Runtime/BuildAssembler.cs
using UnityEngine;

namespace MechBattle
{
    public static class BuildAssembler
    {
        // Assemble a runtime MechUnit from a saved MechBuild using the catalogs
        public static MechUnit Assemble(MechBuild build, MatrixCatalog matrices, PartCatalog parts)
        {
            if (build == null || matrices == null || parts == null)
            {
                Debug.LogError("[BuildAssembler] Missing inputs.");
                return null;
            }

            var matrix = matrices.Get(build.matrixId);
            var ra = parts.Get(build.rightArmId);
            var la = parts.Get(build.leftArmId);
            var lb = parts.Get(build.lowerBodyId);

            if (matrix == null || ra == null || la == null || lb == null)
            {
                Debug.LogError("[BuildAssembler] Invalid codes in build.");
                return null;
            }

            var unit = new MechUnit
            {
                Name = matrix.matrixName,
                chassis = matrix,
                matrixHP = matrix.baseStats?.HP ?? 100
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
}
