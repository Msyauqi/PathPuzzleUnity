using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public bool autoResolveSlidersFromSettingContent = true;

    [Header("Template Layout Fix")]
    public bool forceTemplateLayout = false;
    public bool disableNestedCanvasComponents = false;
    public Vector2 musicRowPosition = new Vector2(0f, 120f);
    public Vector2 sfxRowPosition = new Vector2(0f, -20f);
    public Vector2 rowSize = new Vector2(360f, 150f);
    public Vector2 labelPosition = new Vector2(0f, 42f);
    public Vector2 labelSize = new Vector2(220f, 90f);
    public Vector2 sliderPosition = new Vector2(62f, -6f);
    public Vector2 sliderSize = new Vector2(180f, 22f);
    public Vector2 linePosition = new Vector2(62f, -6f);
    public Vector2 lineSize = new Vector2(180f, 6f);
    public Vector2 iconPosition = new Vector2(-62f, -6f);
    public Vector2 iconSizeTemplate = new Vector2(48f, 48f);
    public float templateHandleSize = 42f;

    [Header("Slider Visual Auto Fix")]
    public bool autoFixSliderVisual = false;
    public float sliderSidePadding = 10f;
    public float handleSize = 24f;
    public bool autoRecoverHandleDragBinding = true;
    public bool keepFillAsStaticStatus = true;
    public bool autoRecoverCollapsedStaticFill = true;
    public bool enforceFullSlideRange = false;
    public float slideRangeLeftPadding = 0f;
    public float slideRangeRightPadding = 0f;

    [Header("Music Background States")]
    public GameObject musicBgGreen;
    public GameObject musicBgRed;

    [Header("SFX Background States")]
    public GameObject sfxBgGreen;
    public GameObject sfxBgRed;

    [Header("Status Icon Layout")]
    public bool autoAlignStatusIcons = false;
    public float iconOffsetFromSlider = 0f;
    public float iconGapFromSlider = 14f;
    public Vector2 statusIconSize = new Vector2(42f, 42f);

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SfxVolume";

    void Start()
    {
        DisableNestedCanvasIfNeeded();
        ResolveSliderReferencesIfNeeded();
        ApplyTemplateLayoutIfNeeded();
        EnsureSliderRuntimeConfig(musicSlider);
        EnsureSliderRuntimeConfig(sfxSlider);
        EnsureHandleDragBinding(musicSlider);
        EnsureHandleDragBinding(sfxSlider);
        ApplyStaticFillBinding();
        EnsureStaticFillVisual(musicSlider);
        EnsureStaticFillVisual(sfxSlider);

        if (autoFixSliderVisual)
        {
            NormalizeSliderVisual(musicSlider);
            NormalizeSliderVisual(sfxSlider);
        }

        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float savedSfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.value = savedMusic;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            sfxSlider.value = savedSfx;
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        ApplyMusicVolumeImmediately(savedMusic);

        UpdateMusicBackground(savedMusic);
        UpdateSfxBackground(savedSfx);
        AlignStatusIconsWithSliders();

        // Run one more pass after other Start() calls (like pager/layout scripts)
        // so our settings row layout is not overwritten by late layout rebuilds.
        StartCoroutine(ReapplyLayoutNextFrame());
    }

    void OnMusicChanged(float value)
    {
        ApplyMusicVolumeImmediately(value);

        UpdateMusicBackground(value);

        PlayerPrefs.SetFloat(MUSIC_KEY, value);
        PlayerPrefs.Save();
    }

    void OnSfxChanged(float value)
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.SetSfxVolume(value);

        UpdateSfxBackground(value);

        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
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

    void DisableNestedCanvasIfNeeded()
    {
        if (!disableNestedCanvasComponents)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
            canvas.overrideSorting = false;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.enabled = false;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;
    }

    void ApplyTemplateLayoutIfNeeded()
    {
        if (!forceTemplateLayout)
            return;

        RectTransform settingContentRt = GetScopedSettingContent();
        if (settingContentRt == null)
            return;

        SetRect(settingContentRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        settingContentRt.localScale = Vector3.one;

        ConfigureRowLayout(settingContentRt, "MusicContainer", "MusicText", "MusicGaris", musicSlider, musicRowPosition, musicBgGreen, musicBgRed, new[] { "MusicIcon", "MusicIcon2" });
        ConfigureRowLayout(settingContentRt, "SfxContainer", "SfxText", "SfxGaris", sfxSlider, sfxRowPosition, sfxBgGreen, sfxBgRed, new[] { "SfxIcon", "SfxIcon2" });
    }

    void ConfigureRowLayout(Transform root, string rowName, string labelName, string lineName, Slider slider, Vector2 rowPosition, GameObject greenIcon, GameObject redIcon, string[] legacyIconsToHide)
    {
        RectTransform rowRt = FindChildByName(root, rowName) as RectTransform;
        if (rowRt == null)
            return;

        SetRect(rowRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rowPosition, rowSize);
        rowRt.localScale = Vector3.one;

        RectTransform labelRt = FindChildByName(rowRt, labelName) as RectTransform;
        if (labelRt != null)
        {
            SetRect(labelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), labelPosition, labelSize);
            labelRt.localScale = Vector3.one;
        }

        RectTransform lineRt = FindChildByName(rowRt, lineName) as RectTransform;
        if (lineRt != null)
        {
            SetRect(lineRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), linePosition, lineSize);
            lineRt.localScale = Vector3.one;
        }

        if (slider != null)
            ConfigureSliderRect(slider, rowRt);

        RectTransform greenRt = greenIcon != null ? greenIcon.transform as RectTransform : null;
        RectTransform redRt = redIcon != null ? redIcon.transform as RectTransform : null;

        if (greenRt != null)
        {
            greenRt.SetParent(rowRt, false);
            SetRect(greenRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), iconPosition, iconSizeTemplate);
            greenRt.localScale = Vector3.one;
        }

        if (redRt != null)
        {
            redRt.SetParent(rowRt, false);
            SetRect(redRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), iconPosition, iconSizeTemplate);
            redRt.localScale = Vector3.one;
        }

        if (legacyIconsToHide != null)
        {
            for (int i = 0; i < legacyIconsToHide.Length; i++)
            {
                Transform legacy = FindChildByName(rowRt, legacyIconsToHide[i]);
                if (legacy != null)
                    legacy.gameObject.SetActive(false);
            }
        }
    }

    void ConfigureSliderRect(Slider slider, RectTransform rowRt)
    {
        RectTransform sliderRt = slider.transform as RectTransform;
        if (sliderRt == null || rowRt == null)
            return;

        sliderRt.SetParent(rowRt, false);
        SetRect(sliderRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sliderPosition, sliderSize);
        sliderRt.localScale = Vector3.one;

        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));
        RectTransform fillRt = fillAreaRt != null ? GetRect(fillAreaRt.Find("Fill")) : null;
        RectTransform handleRt = handleAreaRt != null ? GetRect(handleAreaRt.Find("Handle")) : null;

        if (fillAreaRt != null)
        {
            fillAreaRt.gameObject.SetActive(true);
            SetRect(fillAreaRt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, lineSize);
            fillAreaRt.localScale = Vector3.one;
        }

        if (fillRt != null)
        {
            SetRect(fillRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillRt.localScale = Vector3.one;
        }

        if (handleAreaRt != null)
        {
            handleAreaRt.gameObject.SetActive(true);
            SetRect(handleAreaRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleAreaRt.localScale = Vector3.one;
        }

        if (handleRt != null)
        {
            SetRect(handleRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(templateHandleSize, templateHandleSize));
            handleRt.localScale = Vector3.one;
            slider.handleRect = handleRt;
        }
    }

    System.Collections.IEnumerator ReapplyLayoutNextFrame()
    {
        yield return null;

        ResolveSliderReferencesIfNeeded();
        ApplyTemplateLayoutIfNeeded();
        EnsureSliderRuntimeConfig(musicSlider);
        EnsureSliderRuntimeConfig(sfxSlider);
        EnsureHandleDragBinding(musicSlider);
        EnsureHandleDragBinding(sfxSlider);
        ApplyStaticFillBinding();
        EnsureStaticFillVisual(musicSlider);
        EnsureStaticFillVisual(sfxSlider);
        AlignStatusIconsWithSliders();
    }

    void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        if (rt == null) return;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindChildByName(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    void NormalizeSliderVisual(Slider slider)
    {
        if (slider == null) return;

        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = false;

        if (!(slider.transform is RectTransform))
            return;

        RectTransform backgroundRt = GetRect(slider.transform.Find("Background"));
        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));

        if (backgroundRt != null)
            StretchToSlider(backgroundRt, 0f, 0f);

        if (fillAreaRt != null)
        {
            fillAreaRt.gameObject.SetActive(true);
            StretchToSlider(fillAreaRt, sliderSidePadding, sliderSidePadding);
        }

        if (handleAreaRt != null)
        {
            handleAreaRt.gameObject.SetActive(true);
            StretchToSlider(handleAreaRt, sliderSidePadding, sliderSidePadding);
        }

        RectTransform fillRt = null;
        RectTransform handleRt = null;

        if (fillAreaRt != null)
            fillRt = GetRect(fillAreaRt.Find("Fill"));
        if (handleAreaRt != null)
            handleRt = GetRect(handleAreaRt.Find("Handle"));

        if (fillRt == null)
            fillRt = slider.fillRect;
        if (handleRt == null)
            handleRt = slider.handleRect;

        if (fillRt != null)
        {
            // Static track: do not bind Fill to slider value.
            // Fill stays fixed, only Handle moves.
            fillRt.localScale = Vector3.one;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
        }

        slider.fillRect = null;

        if (handleRt != null)
        {
            handleRt.localScale = Vector3.one;
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(handleSize, handleSize);

            slider.handleRect = handleRt;

            if (slider.targetGraphic == null)
            {
                Graphic handleGraphic = handleRt.GetComponent<Graphic>();
                if (handleGraphic != null)
                    slider.targetGraphic = handleGraphic;
            }
        }
    }

    RectTransform GetRect(Transform t)
    {
        return t as RectTransform;
    }

    void StretchToSlider(RectTransform rt, float left, float right)
    {
        if (rt == null) return;

        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, 0f);
        rt.offsetMax = new Vector2(-right, 0f);
        rt.anchoredPosition = Vector2.zero;
    }

    void ApplyStaticFillBinding()
    {
        if (!keepFillAsStaticStatus)
            return;

        if (musicSlider != null)
            musicSlider.fillRect = null;

        if (sfxSlider != null)
            sfxSlider.fillRect = null;
    }

    void EnsureStaticFillVisual(Slider slider)
    {
        if (!keepFillAsStaticStatus || !autoRecoverCollapsedStaticFill || slider == null)
            return;

        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform fillRt = fillAreaRt != null ? GetRect(fillAreaRt.Find("Fill")) : null;
        if (fillRt == null)
            return;

        bool collapsedAnchor = Mathf.Abs(fillRt.anchorMin.x - fillRt.anchorMax.x) < 0.0001f &&
                               Mathf.Abs(fillRt.anchorMin.y - fillRt.anchorMax.y) < 0.0001f;
        bool collapsedSize = fillRt.sizeDelta.sqrMagnitude < 0.01f;
        bool shouldRecover = collapsedAnchor || collapsedSize;

        if (!shouldRecover)
            return;

        fillRt.localScale = Vector3.one;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.pivot = new Vector2(0.5f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;

        Image fillImage = fillRt.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.enabled = true;
            Color c = fillImage.color;
            if (c.a <= 0.01f)
            {
                c.a = 1f;
                fillImage.color = c;
            }
        }
    }

    void EnsureHandleDragBinding(Slider slider)
    {
        if (!autoRecoverHandleDragBinding || slider == null)
            return;

        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));
        if (handleAreaRt == null)
            handleAreaRt = GetRect(slider.transform.Find("Sliding Area"));

        RectTransform handleRt = handleAreaRt != null ? GetRect(handleAreaRt.Find("Handle")) : null;
        if (handleRt == null)
            handleRt = slider.handleRect;

        if (handleRt == null)
            return;

        if (handleAreaRt != null)
        {
            // Recover sliders whose drag lane was accidentally collapsed to the left edge.
            bool collapsedHorizontally =
                Mathf.Abs(handleAreaRt.anchorMax.x - handleAreaRt.anchorMin.x) < 0.0001f ||
                Mathf.Abs(handleAreaRt.rect.width) < 0.01f;

            if (collapsedHorizontally)
            {
                handleAreaRt.anchorMin = new Vector2(0f, 0f);
                handleAreaRt.anchorMax = new Vector2(1f, 1f);
                handleAreaRt.pivot = new Vector2(0.5f, 0.5f);
                handleAreaRt.anchoredPosition = Vector2.zero;
                handleAreaRt.sizeDelta = Vector2.zero;
            }

            handleAreaRt.gameObject.SetActive(true);
        }

        handleRt.gameObject.SetActive(true);
        slider.interactable = true;
        slider.handleRect = handleRt;

        Graphic handleGraphic = handleRt.GetComponent<Graphic>();
        if (handleGraphic != null)
        {
            handleGraphic.raycastTarget = true;
            slider.targetGraphic = handleGraphic;
        }
    }

    void EnsureSliderRuntimeConfig(Slider slider)
    {
        if (slider == null) return;

        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        if (!enforceFullSlideRange)
            return;

        RectTransform backgroundRt = GetRect(slider.transform.Find("Background"));
        RectTransform fillAreaRt = GetRect(slider.transform.Find("Fill Area"));
        RectTransform handleAreaRt = GetRect(slider.transform.Find("Handle Slide Area"));

        if (backgroundRt != null)
            StretchToSlider(backgroundRt, 0f, 0f);

        if (fillAreaRt != null)
        {
            fillAreaRt.gameObject.SetActive(true);
            StretchToSlider(fillAreaRt, slideRangeLeftPadding, slideRangeRightPadding);
        }

        if (handleAreaRt != null)
        {
            handleAreaRt.gameObject.SetActive(true);
            StretchToSlider(handleAreaRt, slideRangeLeftPadding, slideRangeRightPadding);
        }

        RectTransform fillRt = null;
        RectTransform handleRt = null;

        if (fillAreaRt != null)
            fillRt = GetRect(fillAreaRt.Find("Fill"));
        if (handleAreaRt != null)
            handleRt = GetRect(handleAreaRt.Find("Handle"));

        if (fillRt == null)
            fillRt = slider.fillRect;
        if (handleRt == null)
            handleRt = slider.handleRect;

        if (fillRt != null)
        {
            fillRt.localScale = Vector3.one;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
        }

        if (handleRt != null)
        {
            handleRt.localScale = Vector3.one;
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(handleSize, handleSize);

            slider.handleRect = handleRt;

            if (slider.targetGraphic == null)
            {
                Graphic handleGraphic = handleRt.GetComponent<Graphic>();
                if (handleGraphic != null)
                    slider.targetGraphic = handleGraphic;
            }
        }
    }

    void AlignStatusIconsWithSliders()
    {
        if (!autoAlignStatusIcons)
            return;

        AlignStatusIconPair(musicSlider, musicBgGreen, musicBgRed);
        AlignStatusIconPair(sfxSlider, sfxBgGreen, sfxBgRed);
    }

    void AlignStatusIconPair(Slider slider, GameObject greenIcon, GameObject redIcon)
    {
        if (slider == null) return;

        RectTransform sliderRt = slider.transform as RectTransform;
        if (sliderRt == null) return;

        RectTransform greenRt = greenIcon != null ? greenIcon.transform as RectTransform : null;
        RectTransform redRt = redIcon != null ? redIcon.transform as RectTransform : null;

        if (greenRt != null)
            ConfigureStatusIconRect(sliderRt, greenRt);

        if (redRt != null)
            ConfigureStatusIconRect(sliderRt, redRt);
    }

    void ConfigureStatusIconRect(RectTransform sliderRt, RectTransform iconRt)
    {
        if (sliderRt == null || iconRt == null) return;

        RectTransform iconParent = iconRt.parent as RectTransform;
        if (iconParent == null) return;

        iconRt.localScale = Vector3.one;
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        float halfSliderWidth = sliderRt.rect.width * 0.5f;
        float halfIconWidth = statusIconSize.x * 0.5f;
        float gap = Mathf.Max(0f, iconGapFromSlider);
        Vector3 worldAnchor = sliderRt.TransformPoint(new Vector3(-(halfSliderWidth + halfIconWidth + gap) + iconOffsetFromSlider, 0f, 0f));
        Vector3 localAnchor = iconParent.InverseTransformPoint(worldAnchor);
        iconRt.anchoredPosition = new Vector2(localAnchor.x, localAnchor.y);
        iconRt.sizeDelta = statusIconSize;
    }

    void ResolveSliderReferencesIfNeeded()
    {
        if (!autoResolveSlidersFromSettingContent)
            return;

        if (musicSlider != null && sfxSlider != null)
            return;

        RectTransform settingContent = GetScopedSettingContent();
        if (settingContent != null)
        {
            Slider resolvedMusic = FindSliderByName(settingContent, "MusicSlider");
            Slider resolvedSfx = FindSliderByName(settingContent, "SfxSlider");

            if (musicSlider == null && resolvedMusic != null)
                musicSlider = resolvedMusic;

            if (sfxSlider == null && resolvedSfx != null)
                sfxSlider = resolvedSfx;
        }

        if (musicSlider == null)
            musicSlider = FindSliderByName(transform, "MusicSlider");

        if (sfxSlider == null)
            sfxSlider = FindSliderByName(transform, "SfxSlider");
    }

    RectTransform GetScopedSettingContent()
    {
        Transform direct = transform.Find("SettingContent");
        if (direct != null)
            return direct as RectTransform;

        Transform nested = FindChildByName(transform, "SettingContent");
        if (nested != null)
            return nested as RectTransform;

        return null;
    }

    Slider FindSliderByName(Transform root, string sliderName)
    {
        if (root == null) return null;

        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider != null && slider.name == sliderName)
                return slider;
        }

        return null;
    }

    void ApplyMusicVolumeImmediately(float value)
    {
        BackgroundMusicPlayer player = BackgroundMusicPlayer.Instance;
        if (player == null)
            player = FindObjectOfType<BackgroundMusicPlayer>(true);

        if (player != null)
            player.SetMusicVolume(value);
        else
        {
            PlayerPrefs.SetFloat(MUSIC_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}
