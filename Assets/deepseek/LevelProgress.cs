using UnityEngine;
using System;

public enum LevelLockInspectorMode
{
    UseProgress,
    ForceLocked,
    ForceUnlocked
}

public static class LevelProgress
{
    public static event Action<int, int> StarsChanged;

    public const string SelectedLevelKey = "SelectedLevelIndex";
    public const string SelectedLevelBypassLockKey = "SelectedLevelBypassLock";

    public static bool IsUnlocked(int levelIndex)
    {
        if (levelIndex <= 0)
            return true;

        return GetStars(levelIndex - 1) > 0;
    }

    public static bool CanPlayLevel(int levelIndex, bool bypassLock)
    {
        return bypassLock || IsUnlocked(levelIndex);
    }

    public static void SelectLevelForPlay(int levelIndex, bool bypassLock = false)
    {
        int safeLevelIndex = Mathf.Max(0, levelIndex);

        PlayerPrefs.SetInt(SelectedLevelKey, safeLevelIndex);
        PlayerPrefs.SetInt(SelectedLevelBypassLockKey, bypassLock ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsSelectedLevelLockBypassed()
    {
        return PlayerPrefs.GetInt(SelectedLevelBypassLockKey, 0) == 1;
    }

    public static void ClearSelectedLevelLockBypass()
    {
        PlayerPrefs.SetInt(SelectedLevelBypassLockKey, 0);
        PlayerPrefs.Save();
    }

    public static int GetStars(int levelIndex)
    {
        return PlayerPrefs.GetInt(GetStarKey(levelIndex), 0);
    }

    public static void SaveStars(int levelIndex, int stars)
    {
        stars = Mathf.Clamp(stars, 0, 3);

        int oldStars = GetStars(levelIndex);
        if (stars > oldStars)
        {
            PlayerPrefs.SetInt(GetStarKey(levelIndex), stars);
            PlayerPrefs.Save();
            StarsChanged?.Invoke(levelIndex, stars);
        }
    }

    public static void SetStarsForTesting(int levelIndex, int stars)
    {
        int safeLevelIndex = Mathf.Max(0, levelIndex);
        int safeStars = Mathf.Clamp(stars, 0, 3);

        PlayerPrefs.SetInt(GetStarKey(safeLevelIndex), safeStars);
        PlayerPrefs.Save();
        StarsChanged?.Invoke(safeLevelIndex, safeStars);
    }

    public static void ClearStarsForTesting(int levelIndex)
    {
        int safeLevelIndex = Mathf.Max(0, levelIndex);

        PlayerPrefs.DeleteKey(GetStarKey(safeLevelIndex));
        PlayerPrefs.Save();
        StarsChanged?.Invoke(safeLevelIndex, 0);
    }

    public static void ResetAllStarsForTesting(int maxLevelIndex)
    {
        int safeMax = Mathf.Max(0, maxLevelIndex);

        for (int i = 0; i <= safeMax; i++)
            PlayerPrefs.DeleteKey(GetStarKey(i));

        PlayerPrefs.Save();

        for (int i = 0; i <= safeMax; i++)
            StarsChanged?.Invoke(i, 0);
    }

    private static string GetStarKey(int levelIndex)
    {
        return $"LevelStars_{levelIndex}";
    }
}
