using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolMechs.Audio
{
    [CreateAssetMenu(fileName = "SFXLibrary", menuName = "SolMechs/Audio/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SFXType type;
            public AudioClip clip;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        private Dictionary<SFXType, AudioClip> _map;

        public AudioClip GetClip(SFXType type)
        {
            if (_map == null)
            {
                _map = new Dictionary<SFXType, AudioClip>();
                foreach (var e in entries)
                {
                    if (e.clip != null)
                    {
                        _map[e.type] = e.clip;
                    }
                    else
                    {
                        Debug.LogWarning($"[SFXLibrary] Missing clip for SFXType: {e.type}");
                    }
                }
            }

            if (_map.TryGetValue(type, out var clip))
            {
                return clip;
            }
            else
            {
                Debug.LogWarning($"[SFXLibrary] No clip found for SFXType: {type}");
                return null;
            }
        }

        // Method to validate the library in the editor
        [ContextMenu("Validate Library")]
        private void ValidateLibrary()
        {
            var allTypes = System.Enum.GetValues(typeof(SFXType));
            foreach (SFXType sfxType in allTypes)
            {
                bool found = entries.Exists(e => e.type == sfxType);
                if (!found)
                {
                    Debug.LogWarning($"[SFXLibrary] Missing entry for SFXType: {sfxType}");
                }
            }
        }
    }
}