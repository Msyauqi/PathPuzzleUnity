using PathPuzzle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelZeroTutorial : MonoBehaviour
{
    enum TutorialStep
    {
        Hidden,
        TapToSpawn,
        Drag,
        Rotate
    }

    public const string TutorialDoneKey = "LevelZeroTutorialDone";

    [Header("References")]
    public GameManager gameManager;
    public GridManager gridManager;
    public ARPlacementManager placementManager;

    [Header("UI")]
    public GameObject tutorialRoot;
    public TextMeshProUGUI tutorialText;
    public Image tutorialImage;
    public GameObject dragHintVisual;
    public GameObject rotateHintVisual;
    public Button nextButton;
    public Button backButton;
    public Button skipButton;
    public TextMeshProUGUI nextButtonLabel;
    public bool forceNonButtonGraphicsIgnoreRaycast = true;
    public bool allowRotatedButtonsToReceiveClicks = true;

    [Header("Level")]
    public int tutorialLevelIndex = 0;
    public bool showOnlyOnce = true;
    public bool restartTutorialOnLevelReset = false;

    [Header("Image Page Tutorial")]
    public bool useImagePageTutorial = true;
    public Sprite[] tutorialPageImages;
    [TextArea] public string[] tutorialPageTexts =
    {
        "Letakkan arena permainan pada permukaan yang sudah terdeteksi.",
        "Drag tile jalur ke kotak grid yang kosong.",
        "Rotate tile sampai arah jalurnya sesuai."
    };
    public string nextButtonText = "Next";
    public string finishButtonText = "Mulai";

    [Header("Text")]
    [TextArea] public string tapToSpawnInstruction = "Tap layar untuk spawn arena.";
    [TextArea] public string dragInstruction = "Drag tile jalur ke kotak grid yang kosong.";
    [TextArea] public string rotateInstruction = "Gunakan dua jari untuk memutar tile sampai arah jalurnya benar.";

    [Header("Behavior")]
    public bool advanceToRotateAfterDragStarted = true;
    public bool advanceToRotateAfterSuccessfulDrop = true;
    public bool completeTutorialAfterRotate = true;
    public bool hideAfterRotate = false;
    public float rotateAutoHideDelay = 2.5f;

    TutorialStep currentStep = TutorialStep.Hidden;
    bool hasDragged;
    bool hasRotated;
    int currentPageIndex;
    int lastPageButtonFrame = -1;
    bool tutorialOpenedThisSession;

    void Awake()
    {
        AutoResolveReferences();
        WireButtons();
        HideTutorial();
    }

    void OnEnable()
    {
        AutoResolveReferences();
        WireButtons();

        ARPlacementManager.ArenaPlaced += HandleArenaPlaced;
        GameManager.LevelReady += HandleLevelReady;
        DragDropHandler.TileDragStarted += HandleTileDragStarted;
        DragDropHandler.TileDropped += HandleTileDropped;
        DragDropHandler.TileRotated += HandleTileRotated;

        ShowTapToSpawnIfSelectedLevelZero();
    }

    void OnDisable()
    {
        ARPlacementManager.ArenaPlaced -= HandleArenaPlaced;
        GameManager.LevelReady -= HandleLevelReady;
        DragDropHandler.TileDragStarted -= HandleTileDragStarted;
        DragDropHandler.TileDropped -= HandleTileDropped;
        DragDropHandler.TileRotated -= HandleTileRotated;
    }

    void AutoResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>(true);

        if (placementManager == null)
            placementManager = FindObjectOfType<ARPlacementManager>(true);

        if (tutorialRoot == null)
            tutorialRoot = FindObjectByName("TutorialPanel");

        if (tutorialImage == null)
            tutorialImage = FindChildComponent<Image>("TutorialImage");

        if (tutorialText == null)
            tutorialText = FindChildComponent<TextMeshProUGUI>("TutorialText");

        if (nextButton == null)
            nextButton = FindChildComponent<Button>("NextButton");

        if (backButton == null)
            backButton = FindChildComponent<Button>("BackButton");
    }

    void WireButtons()
    {
        if (nextButtonLabel == null && nextButton != null)
            nextButtonLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
            nextButton.onClick.AddListener(ShowNextPage);
            nextButton.interactable = true;
            nextButton.gameObject.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowPreviousPage);
            backButton.onClick.AddListener(ShowPreviousPage);
            backButton.interactable = true;
            backButton.gameObject.SetActive(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(FinishTutorial);
            skipButton.onClick.AddListener(FinishTutorial);
            skipButton.gameObject.SetActive(false);
        }

        ApplyRaycastSafety();
        ApplyGraphicRaycasterSafety();
    }

    void ApplyRaycastSafety()
    {
        if (!forceNonButtonGraphicsIgnoreRaycast || tutorialRoot == null)
            return;

        Graphic[] graphics = tutorialRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            Button ownerButton = graphic.GetComponentInParent<Button>(true);
            graphic.raycastTarget = ownerButton != null;
        }

        if (nextButton != null && nextButton.targetGraphic != null)
            nextButton.targetGraphic.raycastTarget = true;

        if (backButton != null && backButton.targetGraphic != null)
            backButton.targetGraphic.raycastTarget = true;

        if (skipButton != null && skipButton.targetGraphic != null)
            skipButton.targetGraphic.raycastTarget = true;
    }

    void ApplyGraphicRaycasterSafety()
    {
        if (!allowRotatedButtonsToReceiveClicks || tutorialRoot == null)
            return;

        GraphicRaycaster raycaster = tutorialRoot.GetComponentInParent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.ignoreReversedGraphics = false;
    }

    GameObject FindObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate != null && candidate.name == objectName && candidate.scene.IsValid())
                return candidate;
        }

        return null;
    }

    T FindChildComponent<T>(string objectName) where T : Component
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        if (tutorialRoot != null)
        {
            Transform[] children = tutorialRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == objectName)
                    return children[i].GetComponent<T>();
            }
        }

        GameObject foundObject = FindObjectByName(objectName);
        return foundObject != null ? foundObject.GetComponent<T>() : null;
    }

    void ShowTapToSpawnIfSelectedLevelZero()
    {
        int selectedLevel = PlayerPrefs.GetInt(LevelProgress.SelectedLevelKey, 0);
        if (selectedLevel != tutorialLevelIndex)
            return;

        if (showOnlyOnce && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1 && !restartTutorialOnLevelReset)
            return;

        StartTutorial();
    }

    void HandleLevelReady(int levelIndex)
    {
        if (levelIndex != tutorialLevelIndex)
        {
            HideTutorial();
            return;
        }

        if (showOnlyOnce && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1 && !restartTutorialOnLevelReset)
        {
            HideTutorial();
            return;
        }

        if (tutorialOpenedThisSession)
            return;

        StartTutorial();
    }

    void HandleArenaPlaced()
    {
        if (useImagePageTutorial)
            return;

        if (currentStep == TutorialStep.TapToSpawn)
            ShowStep(TutorialStep.Drag);
    }

    [ContextMenu("Tutorial/Start Tutorial")]
    public void StartTutorial()
    {
        CancelInvoke(nameof(HideTutorial));
        hasDragged = false;
        hasRotated = false;
        tutorialOpenedThisSession = true;

        if (useImagePageTutorial)
        {
            ShowPage(0);
            return;
        }

        ShowStep(TutorialStep.TapToSpawn);
    }

    [ContextMenu("Tutorial/Reset Tutorial Save")]
    public void ResetTutorialSave()
    {
        ResetTutorialProgress();
    }

    public static void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(TutorialDoneKey);
        PlayerPrefs.Save();
    }

    void HandleTileDragStarted(PathTile tile)
    {
        if (useImagePageTutorial)
            return;

        if (currentStep != TutorialStep.Drag)
            return;

        hasDragged = true;

        if (advanceToRotateAfterDragStarted)
            ShowStep(TutorialStep.Rotate);
    }

    void HandleTileDropped(PathTile tile, Vector2Int gridPosition)
    {
        if (useImagePageTutorial)
            return;

        if (currentStep != TutorialStep.Drag)
            return;

        hasDragged = true;

        if (advanceToRotateAfterSuccessfulDrop)
            ShowStep(TutorialStep.Rotate);
    }

    void HandleTileRotated(PathTile tile)
    {
        if (useImagePageTutorial)
            return;

        if (currentStep != TutorialStep.Rotate && currentStep != TutorialStep.Drag)
            return;

        hasRotated = true;

        if (currentStep == TutorialStep.Drag)
            ShowStep(TutorialStep.Rotate);

        MarkTutorialDone();

        if (completeTutorialAfterRotate)
        {
            HideTutorial();
            return;
        }

        if (hideAfterRotate)
            Invoke(nameof(HideTutorial), Mathf.Max(0.1f, rotateAutoHideDelay));
    }

    public void ShowNextPage()
    {
        if (!useImagePageTutorial)
            return;

        if (lastPageButtonFrame == Time.frameCount)
            return;

        lastPageButtonFrame = Time.frameCount;

        int totalPages = GetPageCount();
        if (currentPageIndex >= totalPages - 1)
        {
            FinishTutorial();
            return;
        }

        ShowPage(currentPageIndex + 1);
    }

    public void ShowPreviousPage()
    {
        if (!useImagePageTutorial)
            return;

        if (lastPageButtonFrame == Time.frameCount)
            return;

        lastPageButtonFrame = Time.frameCount;

        if (currentPageIndex <= 0)
            return;

        ShowPage(currentPageIndex - 1);
    }

    void ShowPage(int pageIndex)
    {
        currentStep = TutorialStep.TapToSpawn;
        currentPageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, GetPageCount() - 1));

        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);

        if (dragHintVisual != null)
            dragHintVisual.SetActive(false);

        if (rotateHintVisual != null)
            rotateHintVisual.SetActive(false);

        if (tutorialImage != null)
        {
            Sprite pageSprite = GetPageSprite(currentPageIndex);
            tutorialImage.sprite = pageSprite;
            tutorialImage.enabled = pageSprite != null;
        }

        if (tutorialText != null)
            tutorialText.text = GetPageText(currentPageIndex);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (backButton != null)
            backButton.gameObject.SetActive(currentPageIndex > 0);

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        if (nextButtonLabel != null)
            nextButtonLabel.text = currentPageIndex >= GetPageCount() - 1 ? finishButtonText : nextButtonText;
    }

    int GetPageCount()
    {
        int imageCount = tutorialPageImages != null ? tutorialPageImages.Length : 0;
        int textCount = tutorialPageTexts != null ? tutorialPageTexts.Length : 0;
        return Mathf.Max(1, Mathf.Max(imageCount, textCount));
    }

    Sprite GetPageSprite(int index)
    {
        if (tutorialPageImages == null || index < 0 || index >= tutorialPageImages.Length)
            return null;

        return tutorialPageImages[index];
    }

    string GetPageText(int index)
    {
        if (tutorialPageTexts == null || index < 0 || index >= tutorialPageTexts.Length)
            return string.Empty;

        return tutorialPageTexts[index];
    }

    public void FinishTutorial()
    {
        MarkTutorialDone();
        HideTutorial();
    }

    void ShowStep(TutorialStep step)
    {
        currentStep = step;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(step != TutorialStep.Hidden);

        if (dragHintVisual != null)
            dragHintVisual.SetActive(step == TutorialStep.Drag);

        if (rotateHintVisual != null)
            rotateHintVisual.SetActive(step == TutorialStep.Rotate);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.enabled = false;

        if (tutorialText != null)
        {
            switch (step)
            {
                case TutorialStep.TapToSpawn:
                    tutorialText.text = tapToSpawnInstruction;
                    break;
                case TutorialStep.Drag:
                    tutorialText.text = dragInstruction;
                    break;
                case TutorialStep.Rotate:
                    tutorialText.text = rotateInstruction;
                    break;
                default:
                    tutorialText.text = string.Empty;
                    break;
            }
        }
    }

    void MarkTutorialDone()
    {
        if (!showOnlyOnce)
            return;

        PlayerPrefs.SetInt(TutorialDoneKey, 1);
        PlayerPrefs.Save();
    }

    void HideTutorial()
    {
        currentStep = TutorialStep.Hidden;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);

        if (dragHintVisual != null)
            dragHintVisual.SetActive(false);

        if (rotateHintVisual != null)
            rotateHintVisual.SetActive(false);

        if (tutorialImage != null)
            tutorialImage.enabled = false;

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
    }
}
