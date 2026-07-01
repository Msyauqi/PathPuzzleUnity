using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuLevelButton : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public TextMeshProUGUI labelTMP;

    [Header("Data")]
    public int levelIndex;

    [Header("Reference")]
    public MainMenuLevelLauncher launcher;

    [Header("Fallback Launch")]
    public string fallbackGameSceneName = "ARSceneDS";

    [Header("Visual Sync")]
    public bool autoSyncStarVisual = true;
    public LevelSelectButton levelSelectVisual;
    public GameObject lockedVisual;
    public GameObject unlockedVisual;
    public bool disableButtonWhenLocked = true;
    public bool hideStarsWhenLocked = false;

    [Header("Inspector Test Lock")]
    public LevelLockInspectorMode lockTestMode = LevelLockInspectorMode.UseProgress;
    public bool syncLockTestModeToVisual = true;

    [Header("Input Fix")]
    public bool forceChildGraphicsIgnoreRaycast = true;
    public bool autoRecoverAbnormalLabelRect = true;
    public float abnormalLabelSizeFactor = 2f;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (levelSelectVisual == null)
            levelSelectVisual = GetComponent<LevelSelectButton>();
    }

    void Start()
    {
        AutoResolveLauncherIfNeeded();
        EnsureStableClickArea();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickLevel);
        }

        RefreshVisuals();
    }

    void OnEnable()
    {
        LevelProgress.StarsChanged += HandleStarsChanged;
        RefreshVisuals();
    }

    void OnDisable()
    {
        LevelProgress.StarsChanged -= HandleStarsChanged;
    }

    void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (levelSelectVisual == null)
            levelSelectVisual = GetComponent<LevelSelectButton>();

        if (syncLockTestModeToVisual && levelSelectVisual != null)
            levelSelectVisual.SetLockTestMode(lockTestMode, false);

        if (isActiveAndEnabled)
            RefreshVisuals();
    }

    public void Setup(int index, MainMenuLevelLauncher targetLauncher)
    {
        SetLevelIndex(index);
        launcher = targetLauncher;
    }

    public void SetLevelIndex(int index)
    {
        levelIndex = Mathf.Max(0, index);
        RefreshVisuals();
    }

    public void RefreshStars()
    {
        if (!autoSyncStarVisual)
            return;

        if (levelSelectVisual == null)
            levelSelectVisual = GetComponent<LevelSelectButton>();

        if (levelSelectVisual == null)
            return;

        if (syncLockTestModeToVisual)
            levelSelectVisual.SetLockTestMode(lockTestMode, false);

        levelSelectVisual.SetLevelIndex(levelIndex);
    }

    public void RefreshLockState()
    {
        bool unlocked = IsUnlocked();

        if (button != null && disableButtonWhenLocked)
            button.interactable = unlocked;

        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);

        if (hideStarsWhenLocked && levelSelectVisual != null && levelSelectVisual.starImages != null)
        {
            for (int i = 0; i < levelSelectVisual.starImages.Length; i++)
            {
                if (levelSelectVisual.starImages[i] != null)
                    levelSelectVisual.starImages[i].gameObject.SetActive(unlocked);
            }
        }
    }

    void RefreshLabel()
    {
        if (labelTMP != null)
            labelTMP.text = $"Level {levelIndex}";
    }

    public void RefreshVisuals()
    {
        RefreshLabel();
        RefreshStars();
        RefreshLockState();
    }

    void OnClickLevel()
    {
        if (!IsUnlocked())
        {
            Debug.Log($"[LEVEL BUTTON] Level {levelIndex} masih locked. Selesaikan level {levelIndex - 1} dulu.");
            return;
        }

        if (launcher != null)
        {
            launcher.PlayLevel(levelIndex, ShouldBypassLockForTesting());
            return;
        }

        // Fallback path so level buttons still work even when launcher reference is empty.
        if (!string.IsNullOrWhiteSpace(fallbackGameSceneName))
        {
            LevelProgress.SelectLevelForPlay(levelIndex, ShouldBypassLockForTesting());
            SceneManager.LoadScene(fallbackGameSceneName);
            return;
        }

        Debug.LogError($"[LEVEL BUTTON] Launcher belum di-assign dan fallback scene kosong untuk level {levelIndex}");
    }

    void AutoResolveLauncherIfNeeded()
    {
        if (launcher != null)
            return;

        launcher = FindObjectOfType<MainMenuLevelLauncher>(true);
    }

    void HandleStarsChanged(int changedLevelIndex, int stars)
    {
        if (changedLevelIndex != levelIndex && changedLevelIndex != levelIndex - 1)
            return;

        RefreshVisuals();
    }

    public bool IsUnlocked()
    {
        switch (lockTestMode)
        {
            case LevelLockInspectorMode.ForceLocked:
                return false;
            case LevelLockInspectorMode.ForceUnlocked:
                return true;
        }

        if (levelSelectVisual != null &&
            !syncLockTestModeToVisual &&
            levelSelectVisual.lockTestMode != LevelLockInspectorMode.UseProgress)
        {
            return levelSelectVisual.IsUnlocked();
        }

        return LevelProgress.IsUnlocked(levelIndex);
    }

    bool ShouldBypassLockForTesting()
    {
        if (lockTestMode == LevelLockInspectorMode.ForceUnlocked)
            return true;

        return levelSelectVisual != null &&
               !syncLockTestModeToVisual &&
               levelSelectVisual.lockTestMode == LevelLockInspectorMode.ForceUnlocked;
    }

    void EnsureStableClickArea()
    {
        if (button == null)
            return;

        Graphic target = button.targetGraphic;
        if (target == null)
        {
            target = GetComponent<Graphic>();
            if (target != null)
                button.targetGraphic = target;
        }

        if (forceChildGraphicsIgnoreRaycast)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic g = graphics[i];
                if (g == null)
                    continue;

                g.raycastTarget = (g == target);
            }
        }

        if (labelTMP == null || !autoRecoverAbnormalLabelRect)
            return;

        RectTransform labelRt = labelTMP.rectTransform;
        RectTransform rootRt = transform as RectTransform;
        if (labelRt == null || rootRt == null)
            return;

        float rootWidth = Mathf.Max(1f, Mathf.Abs(rootRt.rect.width));
        float rootHeight = Mathf.Max(1f, Mathf.Abs(rootRt.rect.height));
        float labelWidth = Mathf.Abs(labelRt.sizeDelta.x);
        float labelHeight = Mathf.Abs(labelRt.sizeDelta.y);

        bool abnormal = labelWidth > (rootWidth * abnormalLabelSizeFactor) ||
                        labelHeight > (rootHeight * abnormalLabelSizeFactor);

        if (!abnormal)
            return;

        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = Vector2.zero;
        labelRt.localScale = Vector3.one;
    }
}
