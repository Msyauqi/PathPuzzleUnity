using UnityEngine;

public class LevelLockTester : MonoBehaviour
{
    [Header("Target")]
    public int targetLevelIndex = 0;
    [Range(0, 3)] public int targetStars = 3;

    [Header("Bulk")]
    public int maxLevelIndex = 20;
    public int unlockUntilLevelIndex = 0;
    [Range(1, 3)] public int unlockStars = 1;

    [Header("Refresh")]
    public bool autoRefreshButtons = true;
    public bool logStatusAfterAction = true;

    [ContextMenu("Test/Set Stars On Target Level")]
    public void SetStarsOnTargetLevel()
    {
        LevelProgress.SetStarsForTesting(targetLevelIndex, targetStars);
        AfterProgressChanged($"Set level {targetLevelIndex} stars = {targetStars}");
    }

    [ContextMenu("Test/Clear Target Level Stars")]
    public void ClearTargetLevelStars()
    {
        LevelProgress.ClearStarsForTesting(targetLevelIndex);
        AfterProgressChanged($"Clear stars level {targetLevelIndex}");
    }

    [ContextMenu("Test/Unlock Until Level")]
    public void UnlockUntilLevel()
    {
        int safeUntil = Mathf.Clamp(unlockUntilLevelIndex, 0, Mathf.Max(0, maxLevelIndex));

        for (int i = 0; i < safeUntil; i++)
            LevelProgress.SetStarsForTesting(i, unlockStars);

        AfterProgressChanged($"Unlock sampai level {safeUntil}");
    }

    [ContextMenu("Test/Reset All Level Progress")]
    public void ResetAllLevelProgress()
    {
        LevelProgress.ResetAllStarsForTesting(maxLevelIndex);
        PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelKey);
        PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelBypassLockKey);
        PlayerPrefs.Save();
        AfterProgressChanged($"Reset level progress 0..{maxLevelIndex}");
    }

    [ContextMenu("Test/Log Lock Status")]
    public void LogLockStatus()
    {
        int safeMax = Mathf.Max(0, maxLevelIndex);

        for (int i = 0; i <= safeMax; i++)
        {
            Debug.Log($"[LEVEL LOCK TEST] Level {i} | unlocked={LevelProgress.IsUnlocked(i)} | stars={LevelProgress.GetStars(i)}");
        }
    }

    [ContextMenu("Test/Refresh Level Buttons")]
    public void RefreshLevelButtons()
    {
        MainMenuLevelButton[] mainButtons = FindObjectsOfType<MainMenuLevelButton>(true);
        for (int i = 0; i < mainButtons.Length; i++)
        {
            if (mainButtons[i] != null)
                mainButtons[i].RefreshVisuals();
        }

        LevelSelectButton[] visualButtons = FindObjectsOfType<LevelSelectButton>(true);
        for (int i = 0; i < visualButtons.Length; i++)
        {
            if (visualButtons[i] != null)
                visualButtons[i].RefreshVisuals();
        }

        Debug.Log($"[LEVEL LOCK TEST] Refreshed {mainButtons.Length} main buttons, {visualButtons.Length} visual buttons.");
    }

    void AfterProgressChanged(string action)
    {
        if (autoRefreshButtons)
            RefreshLevelButtons();

        if (logStatusAfterAction)
        {
            Debug.Log($"[LEVEL LOCK TEST] {action}");
            LogLockStatus();
        }
    }
}
