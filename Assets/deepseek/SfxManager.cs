using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Lifecycle")]
    public bool persistAcrossScenes = false;

    [Header("Audio Source")]
    public AudioSource oneShotSource;
    public AudioSource rollingSource;

    [Header("Clips")]
    public AudioClip buttonClickClip;
    public AudioClip swipePageClip;
    public AudioClip placeArenaClip;
    public AudioClip tileDragStartClip;
    public AudioClip tileDropClip;
    public AudioClip invalidMoveClip;
    public AudioClip startSimulationClip;
    public AudioClip ballRollingClip;
    public AudioClip levelFailedClip;
    public AudioClip levelCompleteClip;
    public AudioClip buySuccessClip;
    public AudioClip buyFailedClip;
    public AudioClip equipSkinClip;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterSfxVolume = 1f;
    public bool useSavedSfxVolume = true;
    public bool autoBindButtonClicks = true;
    public bool ignoreButtonClickOnShopBuyButtons = false;

    private const string SFX_KEY = "SfxVolume";
    private readonly HashSet<Button> boundButtons = new HashSet<Button>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        ApplySavedVolume();
    }

    void Start()
    {
        BindButtonClicksInScene();
        StartCoroutine(BindButtonClicksNextFrame());
    }

    void OnEnable()
    {
        BindButtonClicksInScene();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshButtonBindings()
    {
        BindButtonClicksInScene();
    }

    IEnumerator BindButtonClicksNextFrame()
    {
        yield return null;
        BindButtonClicksInScene();
    }

    public void SetSfxVolume(float value)
    {
        masterSfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SFX_KEY, masterSfxVolume);
        PlayerPrefs.Save();

        if (rollingSource != null)
            rollingSource.volume = masterSfxVolume;
    }

    public float GetSfxVolume()
    {
        if (useSavedSfxVolume)
            return PlayerPrefs.GetFloat(SFX_KEY, masterSfxVolume);

        return masterSfxVolume;
    }

    public void PlayButtonClick() => PlayOneShot(buttonClickClip);
    public void PlaySwipePage() => PlayOneShot(swipePageClip);
    public void PlayPlaceArena() => PlayOneShot(placeArenaClip);
    public void PlayTileDragStart() => PlayOneShot(tileDragStartClip);
    public void PlayTileDrop() => PlayOneShot(tileDropClip);
    public void PlayInvalidMove() => PlayOneShot(invalidMoveClip);
    public void PlayStartSimulation() => PlayOneShot(startSimulationClip);
    public void PlayLevelFailed() => PlayOneShot(levelFailedClip);
    public void PlayLevelComplete() => PlayOneShot(levelCompleteClip);
    public void PlayBuySuccess() => PlayOneShot(buySuccessClip);
    public void PlayBuyFailed() => PlayOneShot(buyFailedClip != null ? buyFailedClip : invalidMoveClip);
    public void PlayEquipSkin() => PlayOneShot(equipSkinClip != null ? equipSkinClip : buttonClickClip);

    public void StartBallRolling()
    {
        EnsureAudioSources();

        if (rollingSource == null || ballRollingClip == null)
            return;

        if (rollingSource.clip != ballRollingClip)
            rollingSource.clip = ballRollingClip;

        rollingSource.loop = true;
        rollingSource.playOnAwake = false;
        rollingSource.spatialBlend = 0f;
        rollingSource.volume = GetSfxVolume();

        if (!rollingSource.isPlaying)
            rollingSource.Play();
    }

    public void StopBallRolling()
    {
        if (rollingSource != null && rollingSource.isPlaying)
            rollingSource.Stop();
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureAudioSources();

        if (oneShotSource == null)
            return;

        oneShotSource.PlayOneShot(clip, GetSfxVolume());
    }

    void EnsureAudioSources()
    {
        if (oneShotSource == null)
            oneShotSource = GetComponent<AudioSource>();

        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;

        if (rollingSource == null)
        {
            GameObject rollingObject = new GameObject("SfxRollingSource");
            rollingObject.transform.SetParent(transform, false);
            rollingSource = rollingObject.AddComponent<AudioSource>();
        }

        rollingSource.playOnAwake = false;
        rollingSource.loop = true;
        rollingSource.spatialBlend = 0f;
    }

    void ApplySavedVolume()
    {
        if (useSavedSfxVolume)
            masterSfxVolume = PlayerPrefs.GetFloat(SFX_KEY, masterSfxVolume);

        if (rollingSource != null)
            rollingSource.volume = masterSfxVolume;
    }

    void BindButtonClicksInScene()
    {
        if (!autoBindButtonClicks)
            return;

        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || boundButtons.Contains(button))
                continue;

            if (ignoreButtonClickOnShopBuyButtons && IsShopBuyButton(button))
                continue;

            button.onClick.AddListener(PlayButtonClick);
            boundButtons.Add(button);
        }
    }

    bool IsShopBuyButton(Button button)
    {
        if (button == null)
            return false;

        string lower = button.name.ToLowerInvariant();
        return lower.Contains("buybutton") || lower.Contains("buy_btn") || lower.Contains("buybtn") || lower == "buy";
    }
}
