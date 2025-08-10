using System.Collections.Generic;
using UnityEngine;

namespace MechBattle
{
    /// <summary>
    /// Lookup table for Matrix assets by their unique matrixCode (e.g., M01, M02).
    /// </summary>
    [CreateAssetMenu(menuName = "SolMechs/Catalog/MatrixCatalog")]
    public class MatrixCatalog : ScriptableObject
    {
        public List<Matrix> matrices = new();

        [System.NonSerialized] private Dictionary<string, Matrix> _byCode;

        public void Init()
        {
            if (_byCode != null) return;
            _byCode = new Dictionary<string, Matrix>();
            foreach (var m in matrices)
            {
                if (m == null || string.IsNullOrEmpty(m.matrixCode)) continue;
                _byCode[m.matrixCode] = m;
            }
        }

        public bool TryGet(string code, out Matrix matrix)
        {
            Init();

            if (_byCode == null || string.IsNullOrEmpty(code))
            {
                matrix = null;
                return false;
            }

            if (_byCode.TryGetValue(code, out var found))
            {
                matrix = found;
                return true;
            }

            matrix = null;
            return false;
        }

        public Matrix Get(string code) => TryGet(code, out var m) ? m : null;

#if UNITY_EDITOR
        private void OnValidate() { _byCode = null; }
#endif
    }
}
