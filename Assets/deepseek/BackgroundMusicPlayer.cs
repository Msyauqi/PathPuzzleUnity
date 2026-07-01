using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BackgroundMusicPlayer : MonoBehaviour
{
    public static BackgroundMusicPlayer Instance { get; private set; }

    [Header("Lifecycle")]
    public bool persistAcrossScenes = false;
    public bool useSceneMusicTransitions = true;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "ARSceneDS";
    public string creditSceneName = "CreditScene";

    [Header("Audio Listener Safety")]
    public bool ensureAudioListener = true;
    public bool preferMainCameraForListener = true;

    [Header("Music Clips")]
    public AudioSource musicSource;
    [Tooltip("Dipakai sebagai music Main Menu jika Main Menu Clip kosong.")]
    public AudioClip backgroundClip;
    public AudioClip mainMenuClip;
    public AudioClip gameplayClip;
    public AudioClip creditClip;

    [Header("Transition")]
    public bool fadeBetweenTracks = true;
    [Min(0f)] public float transitionDuration = 0.75f;

    private const string MUSIC_KEY = "MusicVolume";
    private AudioListener fallbackListener;
    private Coroutine transitionCoroutine;
    private float targetVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes || useSceneMusicTransitions)
            DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        targetVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        EnsureAudioListenerExists();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        EnsureAudioListenerExists();

        ConfigureMusicSource();
        PlayMusicForScene(SceneManager.GetActiveScene().name, true);
    }

    public void SetMusicVolume(float value)
    {
        targetVolume = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = targetVolume;

        PlayerPrefs.SetFloat(MUSIC_KEY, targetVolume);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        return targetVolume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListenerExists();

        if (useSceneMusicTransitions)
            PlayMusicForScene(scene.name);
    }

    void ConfigureMusicSource()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = targetVolume;
    }

    public void SetCreditClip(AudioClip clip)
    {
        creditClip = clip;
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(GetMainMenuClip());
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayClip);
    }

    public void PlayCreditMusic()
    {
        PlayMusic(creditClip);
    }

    public void PlayMusicForScene(string sceneName, bool immediate = false)
    {
        AudioClip sceneClip = GetClipForScene(sceneName);
        PlayMusic(sceneClip, immediate);
    }

    AudioClip GetClipForScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(creditSceneName) && sceneName == creditSceneName)
            return creditClip;

        if (!string.IsNullOrEmpty(gameplaySceneName) && sceneName == gameplaySceneName)
            return gameplayClip;

        if (!string.IsNullOrEmpty(mainMenuSceneName) && sceneName == mainMenuSceneName)
            return GetMainMenuClip();

        return GetMainMenuClip();
    }

    AudioClip GetMainMenuClip()
    {
        return mainMenuClip != null ? mainMenuClip : backgroundClip;
    }

    public void PlayMusic(AudioClip clip, bool immediate = false)
    {
        ConfigureMusicSource();

        if (clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (!fadeBetweenTracks || immediate || transitionDuration <= 0f || !musicSource.isPlaying)
        {
            musicSource.clip = clip;
            musicSource.volume = targetVolume;
            musicSource.Play();
            return;
        }

        transitionCoroutine = StartCoroutine(FadeToClip(clip));
    }

    IEnumerator FadeToClip(AudioClip nextClip)
    {
        float halfDuration = Mathf.Max(0.01f, transitionDuration * 0.5f);
        float startVolume = musicSource.volume;

        for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.clip = nextClip;
        musicSource.Play();

        for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / halfDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        transitionCoroutine = null;
    }

    void EnsureAudioListenerExists()
    {
        if (!ensureAudioListener)
            return;

        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        int activeEnabledCount = 0;
        bool hasOtherActiveListener = false;

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null || !listener.enabled || !listener.gameObject.activeInHierarchy)
                continue;

            activeEnabledCount++;
            if (fallbackListener != null && listener != fallbackListener)
                hasOtherActiveListener = true;
        }

        if (activeEnabledCount == 0)
        {
            if (fallbackListener == null)
                fallbackListener = CreateOrResolveFallbackListener();

            if (fallbackListener != null)
                fallbackListener.enabled = true;

            return;
        }

        if (fallbackListener != null && hasOtherActiveListener)
            fallbackListener.enabled = false;
    }

    AudioListener CreateOrResolveFallbackListener()
    {
        if (preferMainCameraForListener && Camera.main != null)
        {
            AudioListener listenerOnMainCamera = Camera.main.GetComponent<AudioListener>();
            if (listenerOnMainCamera != null)
                return listenerOnMainCamera;

            return Camera.main.gameObject.AddComponent<AudioListener>();
        }

        AudioListener listenerOnSelf = GetComponent<AudioListener>();
        if (listenerOnSelf != null)
            return listenerOnSelf;

        return gameObject.AddComponent<AudioListener>();
    }
}
