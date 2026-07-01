using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject pausePanel;
    public bool hidePanelOnStart = true;
    public bool pauseWithTimeScale = true;
    public bool allowEscapeKeyToggle = true;

    [Header("Buttons")]
    public Button pauseButton;
    public Button resumeButton;
    public Button restartButton;
    public Button exitButton;

    [Header("Pause Button Visibility")]
    public bool showPauseButtonFromScanStart = true;
    public bool movePauseButtonToCanvasRoot = true;
    public Transform pauseButtonVisibleParent;

    [Header("Volume")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;
    public bool applySfxVolumeToAllNonMusicSources = true;

    [Header("Settings Slider Style")]
    public bool useSettingsSliderBehavior = true;
    public bool keepFillAsStaticStatus = true;
    public bool autoRecoverCollapsedStaticFill = true;
    public bool autoRecoverHandleDragBinding = true;
    public bool enforceFullSlideRange = false;
    public float slideRangeLeftPadding = 0f;
    public float slideRangeRightPadding = 0f;

    [Header("Volume Icon States")]
    public GameObject musicBgGreen;
    public GameObject musicBgRed;
    public GameObject sfxBgGreen;
    public GameObject sfxBgRed;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";
    public bool restartFromSurfaceScan = true;

    [Header("Auto Resolve")]
    public bool autoResolveReferencesByName = true;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SfxVolume";

    private bool isPaused;
    private float timeScaleBeforePause = 1f;

    void Awake()
    {
        ResolveReferencesIfNeeded();
        EnsurePauseButtonVisibleFromScanStart();
        InitializeSliders();

        if (hidePanelOnStart)
            SetPanelActive(false);
    }

    void OnEnable()
    {
        BindButtons();
        BindSliders();
    }

    void OnDisable()
    {
        UnbindButtons();
        UnbindSliders();

        if (isPaused && pauseWithTimeScale)
            Time.timeScale = timeScaleBeforePause <= 0f ? 1f : timeScaleBeforePause;
    }

    void Update()
    {
        if (!allowEscapeKeyToggle)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void ForceShowPauseButtonFromScanStart()
    {
        ResolveReferencesIfNeeded();
        EnsurePauseButtonVisibleFromScanStart();
        BindButtons();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        timeScaleBeforePause = Time.timeScale <= 0f ? 1f : Time.timeScale;

        SetPanelActive(true);

        if (pauseWithTimeScale)
            Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            SetPanelActive(false);
            return;
        }

        isPaused = false;

        if (pauseWithTimeScale)
            Time.timeScale = timeScaleBeforePause <= 0f ? 1f : timeScaleBeforePause;

        SetPanelActive(false);
    }

    public void RestartLevel()
    {
        ResumeGame();

        if (restartFromSurfaceScan)
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else
                SceneManager.LoadScene(activeScene.name);

            return;
        }

        GameManager manager = GameManager.Instance;
        if (manager == null)
            manager = FindObjectOfType<GameManager>(true);

        if (manager != null)
            manager.ResetCurrentLevel();
    }

    public void ExitToMainMenu()
    {
        if (pauseWithTimeScale)
            Time.timeScale = 1f;

        GameManager manager = GameManager.Instance;
        if (manager == null)
            manager = FindObjectOfType<GameManager>(true);

        string targetScene = mainMenuSceneName;
        if (manager != null && !string.IsNullOrWhiteSpace(manager.mainMenuSceneName))
            targetScene = manager.mainMenuSceneName;

        SceneManager.LoadScene(targetScene);
    }

    void InitializeSliders()
    {
        ConfigureSlider(musicSlider);
        ConfigureSlider(sfxSlider);

        float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        BackgroundMusicPlayer musicPlayer = BackgroundMusicPlayer.Instance;
        if (musicPlayer == null)
            musicPlayer = FindObjectOfType<BackgroundMusicPlayer>(true);

        if (musicPlayer != null)
            musicVolume = musicPlayer.GetMusicVolume();

        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxVolume);

        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);
        UpdateMusicBackground(musicVolume);
        UpdateSfxBackground(sfxVolume);
        UpdateVolumeTexts();
    }

    void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = true;
        slider.direction = Slider.Direction.LeftToRight;

        if (!useSettingsSliderBehavior)
            return;

        EnsureSliderRuntimeConfig(slider);
        EnsureHandleDragBinding(slider);
        ApplyStaticFillBinding(slider);
        EnsureStaticFillVisual(slider);
    }

    void OnMusicSliderChanged(float value)
    {
        ApplyMusicVolume(value);
        UpdateMusicBackground(value);
        UpdateVolumeTexts();
    }

    void OnSfxSliderChanged(float value)
    {
        ApplySfxVolume(value);
        UpdateSfxBackground(value);
        UpdateVolumeTexts();
    }

    void UpdateMusicBackground(float value)
    {
        bool muted = value <= 0.001f;

        if (musicBgGreen != null)
            musicBgGreen.SetActive(!muted);

        if (musicBgRed != null)
            musicBgRed.SetActive(muted);
    }

    void UpdateSfxBackground(float value)
    {
        bool muted = value <= 0.001f;

        if (sfxBgGreen != null)
            sfxBgGreen.SetActive(!muted);

        if (sfxBgRed != null)
            sfxBgRed.SetActive(muted);
    }

    void ApplyMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        BackgroundMusicPlayer musicPlayer = BackgroundMusicPlayer.Instance;
        if (musicPlayer == null)
            musicPlayer = FindObjectOfType<BackgroundMusicPlayer>(true);

        if (musicPlayer != null)
            musicPlayer.SetMusicVolume(value);
        else
        {
            PlayerPrefs.SetFloat(MUSIC_KEY, value);
            PlayerPrefs.Save();
        }
    }

    void ApplySfxVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();

        if (SfxManager.Instance != null)
            SfxManager.Instance.SetSfxVolume(value);

        if (!applySfxVolumeToAllNonMusicSources)
            return;

        AudioSource musicSource = null;
        BackgroundMusicPlayer musicPlayer = BackgroundMusicPlayer.Instance;
        if (musicPlayer == null)
            musicPlayer = FindObjectOfType<BackgroundMusicPlayer>(true);

        if (musicPlayer != null)
            musicSource = musicPlayer.musicSource;

        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source == musicSource)
                continue;

            source.volume = value;
        }
    }

    void UpdateVolumeTexts()
    {
        if (musicValueText != null)
            musicValueText.text = ToPercentText(musicSlider != null ? musicSlider.value : PlayerPrefs.GetFloat(MUSIC_KEY, 1f));

        if (sfxValueText != null)
            sfxValueText.text = ToPercentText(sfxSlider != null ? sfxSlider.value : PlayerPrefs.GetFloat(SFX_KEY, 1f));
    }

    string ToPercentText(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    void BindButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseGame);
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartLevel);
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitToMainMenu);
            exitButton.onClick.AddListener(ExitToMainMenu);
        }
    }

    void UnbindButtons()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitToMainMenu);
    }

    void BindSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
    }

    void UnbindSliders()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    void SetPanelActive(bool active)
    {
        if (pausePanel != null)
            pausePanel.SetActive(active);
    }

    void ResolveReferencesIfNeeded()
    {
        if (!autoResolveReferencesByName)
            return;

        if (pausePanel == null)
            pausePanel = FindGameObjectByNameContains(transform, "pausepanel", "pause_panel", "pause menu", "pausemenu");

        if (pauseButton == null)
            pauseButton = FindButtonByNameContains(transform, "pausebutton", "pause_button", "buttonpause", "button_pause");

        if (pauseButton == null)
            pauseButton = FindButtonInSceneByNameContains("pausebutton", "pause_button", "buttonpause", "button_pause");

        if (resumeButton == null)
            resumeButton = FindButtonByNameContains(transform, "resumebutton", "resume_button", "buttonresume", "button_resume");

        if (restartButton == null)
            restartButton = FindButtonByNameContains(transform, "restartbutton", "restart_button", "buttonrestart", "button_restart");

        if (exitButton == null)
            exitButton = FindButtonByNameContains(transform, "exitbutton", "exit_button", "buttonexit", "button_exit");

        if (musicSlider == null)
            musicSlider = FindSliderByNameContains(transform, "musicslider", "music_slider");

        if (sfxSlider == null)
            sfxSlider = FindSliderByNameContains(transform, "sfxslider", "sfx_slider");

        if (musicBgGreen == null)
            musicBgGreen = FindGameObjectByNameContains(transform, "musicbggreen", "music_bg_green", "musicgreen");

        if (musicBgRed == null)
            musicBgRed = FindGameObjectByNameContains(transform, "musicbgred", "music_bg_red", "musicred");

        if (sfxBgGreen == null)
            sfxBgGreen = FindGameObjectByNameContains(transform, "sfxbggreen", "sfx_bg_green", "sfxgreen");

        if (sfxBgRed == null)
            sfxBgRed = FindGameObjectByNameContains(transform, "sfxbgred", "sfx_bg_red", "sfxred");
    }

    void EnsurePauseButtonVisibleFromScanStart()
    {
        if (!showPauseButtonFromScanStart)
            return;

        if (pauseButton == null)
            pauseButton = FindButtonInSceneByNameContains("pausebutton", "pause_button", "buttonpause", "button_pause");

        if (pauseButton == null)
        {
            Debug.LogWarning("[PAUSE] Pause Button belum ditemukan. Drag tombol Pause ke field Pause Button, atau beri nama object tombolnya 'PauseButton'.");
            return;
        }

        if (movePauseButtonToCanvasRoot)
        {
            Transform targetParent = pauseButtonVisibleParent;
            if (targetParent == null)
            {
                Canvas parentCanvas = pauseButton.GetComponentInParent<Canvas>(true);
                if (parentCanvas == null)
                    parentCanvas = GetComponentInParent<Canvas>(true);

                if (parentCanvas != null)
                    targetParent = parentCanvas.transform;
            }

            if (targetParent != null && pauseButton.transform.parent != targetParent)
                pauseButton.transform.SetParent(targetParent, true);
        }

        pauseButton.gameObject.SetActive(true);
        pauseButton.interactable = true;

        // Only wake the Canvas itself. Do not wake old ancestors like GameUIPanel,
        // because the pause button is intentionally allowed to appear before gameplay UI.
        Canvas canvas = pauseButton.GetComponentInParent<Canvas>(true);
        if (canvas != null && !canvas.gameObject.activeSelf)
            canvas.gameObject.SetActive(true);
    }

    Button FindButtonInSceneByNameContains(params string[] keywords)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && NameMatches(button.transform, keywords))
                return button;
        }

        return null;
    }

    void EnsureSliderRuntimeConfig(Slider slider)
    {
        if (slider == null)
            return;

        if (!enforceFullSlideRange)
            return;

        RectTransform backgroundRt = GetRect(slider.transform.Find("Background"));
        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));

        if (backgroundRt != null)
            StretchToSlider(backgroundRt, 0f, 0f);

        if (fillAreaRt != null)
            StretchToSlider(fillAreaRt, slideRangeLeftPadding, slideRangeRightPadding);

        if (handleAreaRt != null)
            StretchToSlider(handleAreaRt, slideRangeLeftPadding, slideRangeRightPadding);

        RectTransform fillRt = slider.fillRect;
        RectTransform handleRt = slider.handleRect;
        ApplyStaticFillBinding(slider);

        if (fillRt != null)
            fillRt.localScale = Vector3.one;

        if (handleRt != null)
        {
            handleRt.localScale = Vector3.one;
            slider.handleRect = handleRt;
        }

        if (slider.targetGraphic == null && handleRt != null)
        {
            Graphic handleGraphic = handleRt.GetComponent<Graphic>();
            if (handleGraphic != null)
                slider.targetGraphic = handleGraphic;
        }
    }

    void EnsureHandleDragBinding(Slider slider)
    {
        if (!autoRecoverHandleDragBinding || slider == null)
            return;

        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));
        if (handleAreaRt == null)
            handleAreaRt = GetRect(slider.transform.Find("Sliding Area"));

        RectTransform handleRt = slider.handleRect;
        if (handleRt == null && handleAreaRt != null)
            handleRt = GetRect(handleAreaRt.Find("Handle"));

        if (handleAreaRt != null)
        {
            float laneWidth = Mathf.Abs(handleAreaRt.rect.width);
            if (laneWidth < 1f)
                StretchToSlider(handleAreaRt, slideRangeLeftPadding, slideRangeRightPadding);
        }

        if (handleRt == null)
            return;

        slider.interactable = true;
        slider.handleRect = handleRt;

        if (slider.targetGraphic == null)
        {
            Graphic handleGraphic = handleRt.GetComponent<Graphic>();
            if (handleGraphic != null)
                slider.targetGraphic = handleGraphic;
        }
    }

    void ApplyStaticFillBinding(Slider slider)
    {
        if (slider == null || !keepFillAsStaticStatus)
            return;

        slider.fillRect = null;
    }

    void EnsureStaticFillVisual(Slider slider)
    {
        if (!keepFillAsStaticStatus || !autoRecoverCollapsedStaticFill || slider == null)
            return;

        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform fillRt = GetRect(slider.transform.Find("Fill Area/Fill"));

        if (fillRt == null)
            fillRt = GetRect(slider.transform.Find("Fill"));

        if (fillRt == null)
            return;

        if (fillAreaRt != null)
        {
            fillRt.SetParent(fillAreaRt, false);
            StretchToParent(fillRt, 0f, 0f, 0f, 0f);
        }
        else
        {
            StretchToSlider(fillRt, 0f, 0f);
        }

        fillRt.localScale = Vector3.one;
        slider.fillRect = null;
    }

    RectTransform GetRect(Transform target)
    {
        return target as RectTransform;
    }

    void StretchToSlider(RectTransform rt, float left, float right)
    {
        StretchToParent(rt, left, right, 0f, 0f);
    }

    void StretchToParent(RectTransform rt, float left, float right, float top, float bottom)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(-(left + right), -(top + bottom));
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

    GameObject FindGameObjectByNameContains(Transform root, params string[] keywords)
    {
        Transform found = FindTransformByNameContains(root, keywords);
        return found != null ? found.gameObject : null;
    }

    Button FindButtonByNameContains(Transform root, params string[] keywords)
    {
        if (root == null)
            return null;

        if (NameMatches(root, keywords))
        {
            Button button = root.GetComponent<Button>();
            if (button != null)
                return button;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Button found = FindButtonByNameContains(root.GetChild(i), keywords);
            if (found != null)
                return found;
        }

        return null;
    }

    Slider FindSliderByNameContains(Transform root, params string[] keywords)
    {
        if (root == null)
            return null;

        if (NameMatches(root, keywords))
        {
            Slider slider = root.GetComponent<Slider>();
            if (slider != null)
                return slider;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Slider found = FindSliderByNameContains(root.GetChild(i), keywords);
            if (found != null)
                return found;
        }

        return null;
    }

    Transform FindTransformByNameContains(Transform root, params string[] keywords)
    {
        if (root == null || keywords == null || keywords.Length == 0)
            return null;

        string nodeName = root.name.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (!string.IsNullOrEmpty(keyword) && nodeName.Contains(keyword.ToLowerInvariant()))
                return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformByNameContains(root.GetChild(i), keywords);
            if (found != null)
                return found;
        }

        return null;
    }

    bool NameMatches(Transform target, params string[] keywords)
    {
        if (target == null || keywords == null || keywords.Length == 0)
            return false;

        string nodeName = target.name.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (!string.IsNullOrEmpty(keyword) && nodeName.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }
}
