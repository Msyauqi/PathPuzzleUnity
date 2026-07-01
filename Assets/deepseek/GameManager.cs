using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PathPuzzle;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event Action<int> LevelReady;

    [Header("UI")]
    public GameObject gameUIPanel;
    public bool showGameUIPanelDuringScanning = true;
    public Button startSimulationButton;
    public Button resetButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;

    [Header("Ball Status HUD")]
    public Image statusBallImage;
    public Sprite[] ballSkinPreviewSprites;
    public bool autoSyncStatusBallImage = true;
    public bool hideStatusBallImageWhenMissing = false;
    public bool showDirectionWhenReady = true;
    public string readyStatusText = "";
    public string movingStatusPrefix = "";
    public string moveUpStatusText = "Up";
    public string moveDownStatusText = "Down";
    public string moveLeftStatusText = "Left";
    public string moveRightStatusText = "Right";

    [Header("Complete Panel")]
    public GameObject completePanel;
    public TextMeshProUGUI completeTitleText;
    public TextMeshProUGUI completeScoreText;
    public TextMeshProUGUI completeMovesCountText;
    public TextMeshProUGUI completeCoinRewardText;
    public Image completeCoinRewardIcon;
    public Image[] completeStarImages;
    public Button completeRetryButton;
    public Button completeHomeButton;
    public Button completeNextButton;

    [Header("Lose Panel")]
    public GameObject losePanel;
    public TextMeshProUGUI loseTitleText;
    public TextMeshProUGUI failedScoreText;
    public TextMeshProUGUI loseMovesCountText;
    public Image[] loseStarImages;
    public Button loseRetryButton;
    public Button loseHomeButton;

    [Header("Star Sprites")]
    public Sprite emptyStarSprite;
    public Sprite filledStarSprite;

    [Header("Game")]
    public GridManager gridManager;
    public BallController ballController;
    public bool hideBallUntilGridReady = true;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Credits")]
    public bool goToCreditSceneAfterLastLevel = true;
    public bool autoLoadCreditSceneOnLastLevelComplete = false;
    public float autoLoadCreditSceneDelay = 1.5f;
    public string creditSceneName = "CreditScene";
    public string lastLevelNextButtonText = "Credits";

    [Header("Timer")]
    public float maxTimeSeconds = 120f;

    [Header("Coin Reward")]
    public bool useCoinPerStarFormula = true;
    public int coinPerStar = 5;
    public int coinRewardOneStar = 5;
    public int coinRewardTwoStar = 10;
    public int coinRewardThreeStar = 15;
    public Sprite completeCoinRewardSprite;
    public bool showCoinIconOnCompletePanel = true;
    public bool includeTotalCoinsOnCompletePanel = false;
    public string coinRewardPrefix = "+";

    [Header("Moves")]
    [SerializeField] private int currentMoveCount = 0;

    private GameState currentState = GameState.Setup;
    private float remainingTime;
    private float activeTimeLimitSeconds;
    private bool timerRunning;
    private bool gridReady;

    string FormatTime(float timeSeconds)
{
    int totalSeconds = Mathf.CeilToInt(timeSeconds);
    int minutes = totalSeconds / 60;
    int seconds = totalSeconds % 60;
    return $"{minutes}:{seconds:00}";
}


    public int CurrentMoveCount => currentMoveCount;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        AutoResolveCompleteCoinRewardText();
        AutoResolveCompleteCoinRewardIcon();
        AutoResolveStatusBallImage();

        if (startSimulationButton != null)
        {
            startSimulationButton.onClick.RemoveAllListeners();
            startSimulationButton.onClick.AddListener(StartSimulation);
        }

        if (resetButton != null)
        {
            // Replace the whole click event so old Inspector bindings cannot also reload the arena.
            resetButton.onClick = new Button.ButtonClickedEvent();
            resetButton.onClick.AddListener(ResetGameplayPathOnly);
        }

        if (completeRetryButton != null)
        {
            completeRetryButton.onClick.RemoveAllListeners();
            completeRetryButton.onClick.AddListener(ResetCurrentLevel);
        }

        if (completeHomeButton != null)
        {
            completeHomeButton.onClick.RemoveAllListeners();
            completeHomeButton.onClick.AddListener(GoToMainMenu);
        }

        if (completeNextButton != null)
        {
            completeNextButton.onClick.RemoveAllListeners();
            completeNextButton.onClick.AddListener(LoadNextLevel);
        }

        if (loseRetryButton != null)
        {
            loseRetryButton.onClick.RemoveAllListeners();
            loseRetryButton.onClick.AddListener(ResetCurrentLevel);
        }

        if (loseHomeButton != null)
        {
            loseHomeButton.onClick.RemoveAllListeners();
            loseHomeButton.onClick.AddListener(GoToMainMenu);
        }

        PauseMenuUI pauseMenuUI = FindObjectOfType<PauseMenuUI>(true);
        if (pauseMenuUI != null)
            pauseMenuUI.ForceShowPauseButtonFromScanStart();

        if (completePanel != null)
            completePanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);

        ResetMoveCount();
        ResetTimer();
        timerRunning = false;
        gridReady = false;
        UpdateTimerUI();
        UpdateLevelUI();
        RefreshStatusBallImage();
        SetBallReadyStatus();
        SetUIState(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(showGameUIPanelDuringScanning);

        if (hideBallUntilGridReady)
            SetBallVisible(false);
    }

    void Update()
{
    if (!timerRunning) return;
    if (currentState != GameState.Setup) return;

    remainingTime -= Time.deltaTime;
    remainingTime = Mathf.Max(remainingTime, 0f);
    UpdateTimerUI();

    if (remainingTime <= 0f)
    {
        timerRunning = false;
        OnGameLose();
    }
}


public void OnGridReady()
{
    currentState = GameState.Setup;
    gridReady = true;
    ResetMoveCount();
    ResetTimer();
    StartTimer();

    if (hideBallUntilGridReady)
        SetBallVisible(true);

    if (gameUIPanel != null)
        gameUIPanel.SetActive(true);

    if (ballController != null && gridManager != null)
    {
        // Keep local transform in grid space, avoid inherited scale drift from previous parent.
        ballController.transform.SetParent(gridManager.transform, false);
        ballController.transform.localPosition = Vector3.zero;
        ballController.transform.localRotation = Quaternion.identity;
        ballController.transform.localScale = Vector3.one;
        ballController.ResetToStart();
    }

    if (completePanel != null)
        completePanel.SetActive(false);

    if (losePanel != null)
        losePanel.SetActive(false);

    SetBallReadyStatus(ballController != null ? ballController.CurrentDirection : Direction.None);
    UpdateLevelUI();
    SetUIState(true);
    LevelReady?.Invoke(gridManager != null ? gridManager.requestedLevelIndex : 0);
}

    public void RefreshBallStartPositionAfterArenaSpawn()
    {
        if (currentState != GameState.Setup || ballController == null)
            return;

        ballController.RefreshPositionOnCurrentTile();
    }



   public void StartSimulation()
{
    if (currentState != GameState.Setup) return;

    Debug.Log("[GAME] Start Simulation");
    SfxManager.Instance?.PlayStartSimulation();

    currentState = GameState.Simulation;
    timerRunning = false;
    CancelInvoke();

    if (ballController != null)
    {
        ballController.ResetToStart();
        ballController.StartMovement();
    }

    if (startSimulationButton != null)
        startSimulationButton.interactable = false;

    SetBallDirectionStatus(ballController != null ? ballController.CurrentDirection : Direction.None);
}


    public void OnGameWin()
    {
        currentState = GameState.Setup;
        timerRunning = false;

        CancelInvoke();

        if (ballController != null)
            ballController.StopMovement();

        SetUIState(true);
        SfxManager.Instance?.PlayLevelComplete();

        int stars = CalculateStarsByTime();
        int progressLevelIndex = gridManager != null ? gridManager.requestedLevelIndex : 0;
        int previousBestStars = LevelProgress.GetStars(progressLevelIndex);

        LevelProgress.SaveStars(progressLevelIndex, stars);
        int earnedCoins = AwardCoinsForStarUpgrade(previousBestStars, stars);

        ShowCompletePanel(stars, earnedCoins);
    }

    public void OnGameLose()
    {
        currentState = GameState.Setup;
        timerRunning = false;

        CancelInvoke();

        if (ballController != null)
            ballController.StopMovement();

        SetUIState(true);
        SfxManager.Instance?.PlayLevelFailed();

        ShowLosePanel();
    }

    public void ResetCurrentLevel()
    {
        Debug.Log("[GAME] Reset Level");

        CancelInvoke();

        if (gridManager != null)
            gridManager.ResetLevel();

        if (ballController != null)
        {
            ballController.StopMovement();
            ballController.ResetToStart();
        }

        currentState = GameState.Setup;
        ResetMoveCount();
        ResetTimer();
        StartTimer();

        if (completePanel != null)
            completePanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);

        SetBallReadyStatus(ballController != null ? ballController.CurrentDirection : Direction.None);
        UpdateLevelUI();
        SetUIState(true);
    }

    public void ResetGameplayPathOnly()
    {
        Debug.Log("[GAME] Reset Path Only");

        CancelInvoke();

        if (ballController != null)
        {
            ballController.StopMovement();
        }

        if (gridManager != null)
            gridManager.ResetPathLayoutOnly();

        if (ballController != null)
            ballController.ResetToStart();

        currentState = GameState.Setup;

        if (completePanel != null)
            completePanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);

        SetBallReadyStatus(ballController != null ? ballController.CurrentDirection : Direction.None);
        UpdateLevelUI();
        SetUIState(true);

        if (gridReady && remainingTime > 0f)
            timerRunning = true;
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }

    public void IncrementMoveCount()
{
    currentMoveCount++;
    UpdateMovesUI();
    Debug.Log($"[MOVE COUNT] {currentMoveCount}");
}
void UpdateMovesUI()
{
    if (movesText != null)
        movesText.text = $"Moves: {currentMoveCount}";
}



    void ResetMoveCount()
{
    currentMoveCount = 0;
    UpdateMovesUI();
}

void UpdateLevelUI()
{
    if (levelText == null)
        return;

    levelText.text = $"Level {GetDisplayLevelIndex()}";
}

int GetDisplayLevelIndex()
{
    if (gridReady && gridManager != null && gridManager.currentLevel != null)
        return Mathf.Max(0, gridManager.currentLevelIndex);

    return Mathf.Max(0, PlayerPrefs.GetInt(LevelProgress.SelectedLevelKey, 0));
}


    void ResetTimer()
    {
        activeTimeLimitSeconds = GetConfiguredLevelTimeLimit();
        remainingTime = activeTimeLimitSeconds;
        UpdateTimerUI();
    }

    void StartTimer()
    {
        timerRunning = true;
    }

    public void SetRemainingTimeForTesting(float seconds, bool startTimerIfSetup = true)
    {
        remainingTime = Mathf.Max(0f, seconds);

        if (startTimerIfSetup && currentState == GameState.Setup)
            timerRunning = true;

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    int CalculateStarsByTime()
    {
        float safeLimit = Mathf.Max(1f, activeTimeLimitSeconds);
        float remainingRatio = Mathf.Clamp01(remainingTime / safeLimit);

        if (remainingRatio >= 0.6667f)
            return 3;

        if (remainingRatio >= 0.5f)
            return 2;

        return 1;
    }

    float GetConfiguredLevelTimeLimit()
    {
        LevelData levelData = null;

        if (gridManager != null)
        {
            levelData = gridManager.currentLevel;

            if (levelData == null && gridManager.allLevels != null && gridManager.allLevels.Count > 0)
            {
                int selectedIndex = PlayerPrefs.GetInt(LevelProgress.SelectedLevelKey, 0);
                selectedIndex = Mathf.Clamp(selectedIndex, 0, gridManager.allLevels.Count - 1);
                levelData = gridManager.allLevels[selectedIndex];
            }
        }

        if (levelData != null && levelData.HasTimeLimit())
            return Mathf.Max(1f, levelData.timeLimit);

        return Mathf.Max(1f, maxTimeSeconds);
    }

    void ShowCompletePanel(int stars, int earnedCoins)
{
    if (completePanel != null)
        completePanel.SetActive(true);

    if (losePanel != null)
        losePanel.SetActive(false);

    if (completeTitleText != null)
        completeTitleText.text = $"Level {GetDisplayLevelIndex()}\nCOMPLETE";

    if (completeScoreText != null)
        completeScoreText.text = $"Time: {FormatTime(remainingTime)}";

    if (completeMovesCountText != null)
        completeMovesCountText.text = $"Moves: {currentMoveCount}";

    if (completeCoinRewardText != null)
    {
        string rewardText = $"{coinRewardPrefix}{earnedCoins}";
        if (includeTotalCoinsOnCompletePanel)
            rewardText += $"  (Total: {CoinWallet.GetCoins()})";

        completeCoinRewardText.text = rewardText;
    }
    else if (completeScoreText != null)
        completeScoreText.text += $"\n{coinRewardPrefix}{earnedCoins}";

    if (completeCoinRewardIcon != null)
    {
        completeCoinRewardIcon.gameObject.SetActive(showCoinIconOnCompletePanel);
        if (showCoinIconOnCompletePanel && completeCoinRewardSprite != null)
            completeCoinRewardIcon.sprite = completeCoinRewardSprite;
    }

    SetStars(completeStarImages, stars);
    ConfigureCompleteNextButton();

    if (goToCreditSceneAfterLastLevel && autoLoadCreditSceneOnLastLevelComplete && IsLastLevel())
        Invoke(nameof(GoToCreditScene), Mathf.Max(0f, autoLoadCreditSceneDelay));
}


    void ShowLosePanel()
{
    if (losePanel != null)
        losePanel.SetActive(true);

    if (completePanel != null)
        completePanel.SetActive(false);

    if (loseTitleText != null)
        loseTitleText.text = $"Level {GetDisplayLevelIndex()}\nFailed";

    if (failedScoreText != null)
        failedScoreText.text = $"Time: {FormatTime(remainingTime)}";

    if (loseMovesCountText != null)
        loseMovesCountText.text = $"Moves: {currentMoveCount}";

    SetStars(loseStarImages, 0);
}


    void SetStars(Image[] starImages, int filledCount)
{
    if (starImages == null) return;

    for (int i = 0; i < starImages.Length; i++)
    {
        if (starImages[i] == null) continue;

        starImages[i].sprite = i < filledCount
            ? filledStarSprite
            : emptyStarSprite;
    }
}


    void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void GoToCreditScene()
    {
        if (string.IsNullOrEmpty(creditSceneName))
        {
            Debug.LogWarning("[GAME] Credit scene name kosong. Balik ke main menu.");
            GoToMainMenu();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(creditSceneName))
        {
            Debug.LogWarning($"[GAME] Scene credit '{creditSceneName}' belum ada di Build Settings. Balik ke main menu.");
            GoToMainMenu();
            return;
        }

        SceneManager.LoadScene(creditSceneName);
    }

    void LoadNextLevel()
    {
        if (goToCreditSceneAfterLastLevel && IsLastLevel())
        {
            GoToCreditScene();
            return;
        }

        if (completePanel != null)
            completePanel.SetActive(false);

        if (gridManager != null)
            gridManager.LoadNextLevel();
    }

    void ConfigureCompleteNextButton()
    {
        if (completeNextButton == null)
            return;

        bool isLastLevel = IsLastLevel();
        completeNextButton.gameObject.SetActive(true);

        if (goToCreditSceneAfterLastLevel && isLastLevel && !string.IsNullOrEmpty(lastLevelNextButtonText))
            SetButtonLabel(completeNextButton, lastLevelNextButtonText);
    }

    void SetButtonLabel(Button button, string text)
    {
        if (button == null || string.IsNullOrEmpty(text))
            return;

        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = text;
    }

    bool IsLastLevel()
    {
        if (gridManager == null || gridManager.allLevels == null || gridManager.allLevels.Count == 0)
            return false;

        return gridManager.currentLevelIndex >= gridManager.allLevels.Count - 1;
    }

    void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    public void SetBallReadyStatus(Direction previewDirection = Direction.None)
    {
        if (showDirectionWhenReady && previewDirection != Direction.None)
            SetStatus(GetDirectionStatusText(previewDirection));
        else
            SetStatus(string.Empty);

        RefreshStatusBallImage();
    }

    public void SetBallDirectionStatus(Direction direction)
    {
        if (direction == Direction.None)
        {
            SetBallReadyStatus();
            return;
        }

        SetStatus($"{movingStatusPrefix}{GetDirectionStatusText(direction)}");
        RefreshStatusBallImage();
    }

    public void RefreshStatusBallImage()
    {
        if (!autoSyncStatusBallImage || statusBallImage == null)
            return;

        int selectedSkin = BallSkinStore.GetSelectedSkinIndex();
        Sprite preview = null;

        if (ballSkinPreviewSprites != null &&
            selectedSkin >= 0 &&
            selectedSkin < ballSkinPreviewSprites.Length)
        {
            preview = ballSkinPreviewSprites[selectedSkin];
        }

        if (preview != null)
        {
            statusBallImage.sprite = preview;
            statusBallImage.enabled = true;
            statusBallImage.gameObject.SetActive(true);
        }
        else if (hideStatusBallImageWhenMissing)
        {
            statusBallImage.enabled = false;
        }
    }

    string GetDirectionStatusText(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return moveUpStatusText;
            case Direction.Down:
                return moveDownStatusText;
            case Direction.Left:
                return moveLeftStatusText;
            case Direction.Right:
                return moveRightStatusText;
            default:
                return string.Empty;
        }
    }

    void SetUIState(bool interactable)
    {
        if (startSimulationButton != null)
            startSimulationButton.interactable = interactable;

        if (resetButton != null)
            resetButton.interactable = interactable;
    }

    int AwardCoinsForStarUpgrade(int previousBestStars, int newStars)
    {
        int oldRewardValue = GetCoinRewardValueForStars(previousBestStars);
        int newRewardValue = GetCoinRewardValueForStars(newStars);
        int reward = Mathf.Max(0, newRewardValue - oldRewardValue);

        if (reward > 0)
            CoinWallet.AddCoins(reward);

        return reward;
    }

    int GetCoinRewardValueForStars(int stars)
    {
        int clampedStars = Mathf.Clamp(stars, 0, 3);

        if (useCoinPerStarFormula)
            return clampedStars * Mathf.Max(0, coinPerStar);

        if (clampedStars >= 3)
            return coinRewardThreeStar;
        if (clampedStars == 2)
            return coinRewardTwoStar;
        if (clampedStars == 1)
            return coinRewardOneStar;

        return 0;
    }

    void AutoResolveCompleteCoinRewardText()
    {
        if (completeCoinRewardText != null)
            return;

        if (completePanel == null)
            return;

        Transform found = FindChildByNameContains(completePanel.transform, "coin");
        if (found == null)
            return;

        completeCoinRewardText = found.GetComponent<TextMeshProUGUI>();
    }

    void AutoResolveCompleteCoinRewardIcon()
    {
        if (completeCoinRewardIcon != null)
            return;

        if (completePanel == null)
            return;

        Transform found = FindChildByAnyKeyword(
            completePanel.transform,
            "coinicon",
            "coin_icon",
            "coinimage",
            "coin_image");

        if (found == null)
            return;

        completeCoinRewardIcon = found.GetComponent<Image>();
    }

    void AutoResolveStatusBallImage()
    {
        if (statusBallImage != null)
            return;

        if (gameUIPanel == null)
            return;

        Transform found = FindChildByAnyKeyword(
            gameUIPanel.transform,
            "ballimage",
            "ball_image",
            "statusballimage",
            "status_ball_image");

        if (found == null)
            return;

        statusBallImage = found.GetComponent<Image>();
    }

    Transform FindChildByNameContains(Transform root, string keywordLowercase)
    {
        if (root == null || string.IsNullOrEmpty(keywordLowercase))
            return null;

        if (root.name.ToLower().Contains(keywordLowercase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildByNameContains(child, keywordLowercase);
            if (found != null)
                return found;
        }

        return null;
    }

    Transform FindChildByAnyKeyword(Transform root, params string[] keywordsLowercase)
    {
        if (root == null || keywordsLowercase == null || keywordsLowercase.Length == 0)
            return null;

        string nodeName = root.name.ToLower();
        for (int i = 0; i < keywordsLowercase.Length; i++)
        {
            string keyword = keywordsLowercase[i];
            if (string.IsNullOrEmpty(keyword))
                continue;

            if (nodeName.Contains(keyword))
                return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildByAnyKeyword(child, keywordsLowercase);
            if (found != null)
                return found;
        }

        return null;
    }

    void SetBallVisible(bool visible)
    {
        if (ballController == null)
            return;

        GameObject ballRoot = ballController.gameObject;
        if (ballRoot != null && ballRoot.activeSelf != visible)
            ballRoot.SetActive(visible);
    }
}
