using System.Collections.Generic;
using UnityEngine;

namespace MechBattle
{
    /// <summary>
    /// Lookup table for MechPart assets by their unique partCode (e.g., RA01, LA01, IN01).
    /// </summary>
    [CreateAssetMenu(menuName = "SolMechs/Catalog/PartCatalog")]
    public class PartCatalog : ScriptableObject
    {
        public List<MechPart> parts = new();

        [System.NonSerialized] private Dictionary<string, MechPart> _byCode;

        public void Init()
        {
            if (_byCode != null) return;
            _byCode = new Dictionary<string, MechPart>();
            foreach (var p in parts)
            {
                if (p == null || string.IsNullOrEmpty(p.partCode)) continue;
                _byCode[p.partCode] = p;
            }
        }

        public bool TryGet(string code, out MechPart part)
        {
            Init();

            if (_byCode == null || string.IsNullOrEmpty(code))
            {
                part = null;
                return false;
            }

            if (_byCode.TryGetValue(code, out var found))
            {
                part = found;
                return true;
            }

            part = null;
            return false;
        }

        public MechPart Get(string code) => TryGet(code, out var p) ? p : null;

#if UNITY_EDITOR
        private void OnValidate() { _byCode = null; }
#endif
    }
}
