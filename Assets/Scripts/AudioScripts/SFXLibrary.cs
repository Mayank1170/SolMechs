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
                    if (e.clip != null) _map[e.type] = e.clip;
            }
            return _map != null && _map.TryGetValue(type, out var clip) ? clip : null;
        }
    }
}