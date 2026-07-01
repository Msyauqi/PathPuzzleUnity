using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditSceneNavigation : MonoBehaviour
{
    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Buttons")]
    public Button mainMenuButton;
    public Button quitButton;

    void Awake()
    {
        BindButtons();
    }

    void OnEnable()
    {
        BindButtons();
    }

    [ContextMenu("Bind Buttons")]
    public void BindButtons()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            return;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
