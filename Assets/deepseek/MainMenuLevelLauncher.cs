using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLevelLauncher : MonoBehaviour
{
    public string gameSceneName = "ARSceneDS";

    public void PlayLevel(int levelIndex)
    {
        PlayLevel(levelIndex, false);
    }

    public void PlayLevel(int levelIndex, bool bypassLockCheck)
    {
        if (!LevelProgress.CanPlayLevel(levelIndex, bypassLockCheck))
        {
            Debug.Log($"[LEVEL LAUNCHER] Level {levelIndex} masih locked. Selesaikan level {levelIndex - 1} dulu.");
            return;
        }

        LevelProgress.SelectLevelForPlay(levelIndex, bypassLockCheck);

        SceneManager.LoadScene(gameSceneName);
    }
}
