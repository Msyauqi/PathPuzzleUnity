using UnityEngine;

public class CreditSceneMusic : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip creditMusic;
    public bool playOnStart = true;
    public bool loop = true;
    public bool useSavedMusicVolume = true;
    [Range(0f, 1f)] public float fallbackVolume = 1f;

    [Header("Scene Music")]
    [Tooltip("Jika ada BackgroundMusicPlayer global, music credit akan dimainkan lewat manager tersebut agar fade transition tetap rapi.")]
    public bool useGlobalMusicPlayer = true;

    const string MusicKey = "MusicVolume";

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;

        if (creditMusic != null)
            audioSource.clip = creditMusic;
    }

    void Start()
    {
        if (useGlobalMusicPlayer && BackgroundMusicPlayer.Instance != null)
        {
            if (!playOnStart)
                return;

            if (creditMusic != null)
                BackgroundMusicPlayer.Instance.SetCreditClip(creditMusic);

            BackgroundMusicPlayer.Instance.PlayCreditMusic();

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            return;
        }

        ApplySavedVolume();

        if (playOnStart && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    [ContextMenu("Apply Saved Volume")]
    public void ApplySavedVolume()
    {
        if (audioSource == null)
            return;

        audioSource.volume = useSavedMusicVolume
            ? PlayerPrefs.GetFloat(MusicKey, fallbackVolume)
            : fallbackVolume;
    }
}
