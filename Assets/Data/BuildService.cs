using System.IO;
using UnityEngine;

namespace MechBattle
{
    /// <summary>
    /// Save/load the player's current build to persistent storage.
    /// </summary>
    public static class BuildService
    {
        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "solmechs_build.json");

        public static void Save(MechBuild build)
        {
            if (build == null)
            {
                Debug.LogWarning("[BuildService] Attempted to save a null build.");
                return;
            }
            var json = JsonUtility.ToJson(build);
            File.WriteAllText(FilePath, json);
#if UNITY_EDITOR
            Debug.Log($"[BuildService] Saved build to {FilePath}\n{json}");
#endif
        }

        public static MechBuild LoadOrNull()
        {
            if (!File.Exists(FilePath)) return null;
            var json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<MechBuild>(json);
        }

        public static MechBuild LoadOrSaveFallback(MechBuild fallback)
        {
            var current = LoadOrNull();
            if (current != null) return current;
            Save(fallback);
            return fallback;
        }
    }
}
