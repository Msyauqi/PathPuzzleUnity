using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "GameScene";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject levelSelectPanel;
    public GameObject settingsPanel;
    public GameObject aboutPanel;

    void Start()
    {
        ShowMain();
    }

    public void OpenLevelSelect()
    {
        Debug.Log("[MENU] Open Level Select");

        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    public void ShowMain()
    {
        Debug.Log("[MENU] Show Main");

        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    public void StartGameFromLevel(int levelIndex)
    {
        Debug.Log($"[MENU] StartGameFromLevel levelIndex={levelIndex}, scene={gameSceneName}");

        if (!LevelProgress.IsUnlocked(levelIndex))
        {
            Debug.Log($"[MENU] Level {levelIndex} masih locked. Selesaikan level {levelIndex - 1} dulu.");
            return;
        }

        LevelProgress.SelectLevelForPlay(levelIndex);

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[MENU] gameSceneName kosong!");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    public void OpenAbout()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(true);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
