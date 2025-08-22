using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AttackFxLibrary", menuName = "SolMechs/FX/AttackFxLibrary")]
public class AttackFxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string attackName;       // must match AttackData.attackName
        public UIFxPlayer prefab;       // UI prefab with Animator
    }

    [Tooltip("Maps attackName to a UIFxPlayer prefab")]
    public List<Entry> items = new List<Entry>();

    // fast lookup cache (built at runtime)
    private Dictionary<string, UIFxPlayer> _map;

    private void OnEnable()
    {
        _map = new Dictionary<string, UIFxPlayer>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in items)
        {
            if (e != null && !string.IsNullOrEmpty(e.attackName) && e.prefab != null)
                _map[e.attackName] = e.prefab;
        }
    }

    public UIFxPlayer Get(string attackName)
    {
        if (string.IsNullOrEmpty(attackName)) return null;
        _map ??= new Dictionary<string, UIFxPlayer>(StringComparer.OrdinalIgnoreCase);
        UIFxPlayer p;
        return _map.TryGetValue(attackName, out p) ? p : null;
    }
}
