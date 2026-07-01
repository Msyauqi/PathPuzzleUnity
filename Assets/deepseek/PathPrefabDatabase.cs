using UnityEngine;
using System.Collections.Generic;
using PathPuzzle;

[CreateAssetMenu(fileName = "PathPrefabDatabase", menuName = "Path Puzzle/Path Prefab Database")]
public class PathPrefabDatabase : ScriptableObject
{
    [System.Serializable]
    public class PathPrefabEntry
    {
        public PathType pathType;
        public GameObject[] skins;
    }

    public List<PathPrefabEntry> pathPrefabs = new List<PathPrefabEntry>();

    private Dictionary<PathType, PathPrefabEntry> lookup;

    // =========================
    void OnEnable()
    {
        BuildLookup();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        BuildLookup();
    }
#endif

    // =========================
    void BuildLookup()
    {
        lookup = new Dictionary<PathType, PathPrefabEntry>();

        foreach (var entry in pathPrefabs)
        {
            if (entry == null)
            {
                Debug.LogError("[DB] Null entry!");
                continue;
            }

            if (lookup.ContainsKey(entry.pathType))
            {
                Debug.LogWarning($"[DB] Duplicate PathType: {entry.pathType}");
                continue;
            }

            lookup.Add(entry.pathType, entry);
        }
    }

    // =========================
    // 🔥 GET PREFAB (SAFE)
    // =========================
    public GameObject GetPrefab(PathType type, int skinIndex)
    {
        if (lookup == null || lookup.Count == 0)
            BuildLookup();

        if (!lookup.TryGetValue(type, out var entry))
        {
            Debug.LogError($"[DB] Entry not found: {type}");
            return null;
        }

        if (entry.skins == null || entry.skins.Length == 0)
        {
            Debug.LogError($"[DB] No skins: {type}");
            return null;
        }

        // 🔥 handle negatif & overflow
        if (skinIndex < 0 || skinIndex >= entry.skins.Length)
        {
            Debug.LogWarning($"[DB] Skin index invalid → {type} ({skinIndex}) → fallback 0");
            skinIndex = 0;
        }

        var prefab = entry.skins[skinIndex];

        // 🔥 fallback aman
        if (prefab == null)
        {
            Debug.LogError($"[DB] Prefab NULL → {type} skin {skinIndex}");

            for (int i = 0; i < entry.skins.Length; i++)
            {
                if (entry.skins[i] != null)
                    return entry.skins[i];
            }

            Debug.LogError($"[DB] Semua prefab NULL → {type}");
            return null;
        }

        return prefab;
    }

    // =========================
    // 🔥 VALIDATION
    // =========================
    [ContextMenu("Validate Prefabs")]
    public void ValidatePrefabs()
    {
        Debug.Log("=== VALIDATING PREFABS ===");

        HashSet<PathType> seenTypes = new HashSet<PathType>();

        foreach (var entry in pathPrefabs)
        {
            if (entry == null)
            {
                Debug.LogError("[DB] Null entry!");
                continue;
            }

            // 🔥 duplicate check
            if (!seenTypes.Add(entry.pathType))
            {
                Debug.LogWarning($"[DB] Duplicate PathType → {entry.pathType}");
            }

            if (entry.skins == null || entry.skins.Length == 0)
            {
                Debug.LogError($"[DB] No skins → {entry.pathType}");
                continue;
            }

            for (int i = 0; i < entry.skins.Length; i++)
            {
                var prefab = entry.skins[i];

                if (prefab == null)
                {
                    Debug.LogError($"[DB] Missing prefab → {entry.pathType} skin {i}");
                    continue;
                }

                var tile = prefab.GetComponent<PathTile>();

                if (tile == null)
                {
                    Debug.LogError($"[DB] No PathTile → {prefab.name}");
                    continue;
                }

                Debug.Log($"✓ {entry.pathType} [{i}] → {prefab.name}");
            }
        }

        Debug.Log("=== VALIDATION DONE ===");
    }
}