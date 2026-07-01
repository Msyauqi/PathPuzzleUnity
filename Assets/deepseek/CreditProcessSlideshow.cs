using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditProcessSlideshow : MonoBehaviour
{
    [Header("References")]
    public Image targetImage;
    public TMP_Text captionText;

    [Header("Slides")]
    public Sprite[] processImages;
    [TextArea(1, 3)] public string[] captions;
    public bool playOnStart = true;
    public bool loop = true;
    public bool preserveAspect = true;

    [Header("Timing")]
    [Min(0.25f)] public float imageDuration = 2.5f;
    [Range(0f, 1f)] public float fadeDuration = 0.35f;
    public bool useUnscaledTime = true;

    [Header("Motion")]
    public bool useSubtlePop = true;
    [Range(1f, 1.2f)] public float popScale = 1.04f;

    int currentIndex;
    float timer;
    bool isPlaying;
    Coroutine transitionRoutine;
    Vector3 initialScale = Vector3.one;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage != null)
        {
            targetImage.preserveAspect = preserveAspect;
            initialScale = targetImage.rectTransform.localScale;
        }
    }

    void Start()
    {
        ShowSlide(0, true);
        isPlaying = playOnStart;
    }

    void Update()
    {
        if (!isPlaying || processImages == null || processImages.Length <= 1)
            return;

        timer += DeltaTime;
        if (timer < imageDuration)
            return;

        timer = 0f;
        NextSlide();
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    [ContextMenu("Next Slide")]
    public void NextSlide()
    {
        if (processImages == null || processImages.Length == 0)
            return;

        int nextIndex = currentIndex + 1;
        if (nextIndex >= processImages.Length)
        {
            if (!loop)
            {
                isPlaying = false;
                return;
            }

            nextIndex = 0;
        }

        ShowSlide(nextIndex, false);
    }

    [ContextMenu("Previous Slide")]
    public void PreviousSlide()
    {
        if (processImages == null || processImages.Length == 0)
            return;

        int nextIndex = currentIndex - 1;
        if (nextIndex < 0)
            nextIndex = loop ? processImages.Length - 1 : 0;

        ShowSlide(nextIndex, false);
    }

    [ContextMenu("Refresh Slide")]
    public void RefreshSlide()
    {
        ShowSlide(currentIndex, true);
    }

    public void ShowSlide(int index, bool instant)
    {
        if (targetImage == null || processImages == null || processImages.Length == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, processImages.Length - 1);
        timer = 0f;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        if (instant || fadeDuration <= 0f)
        {
            ApplySlide(currentIndex);
            SetImageAlpha(1f);
            ResetImageScale();
            return;
        }

        transitionRoutine = StartCoroutine(TransitionToSlide(currentIndex));
    }

    System.Collections.IEnumerator TransitionToSlide(int index)
    {
        float halfFade = Mathf.Max(0.01f, fadeDuration * 0.5f);

        for (float t = 0f; t < halfFade; t += DeltaTime)
        {
            SetImageAlpha(1f - (t / halfFade));
            yield return null;
        }

        ApplySlide(index);

        for (float t = 0f; t < halfFade; t += DeltaTime)
        {
            float progress = t / halfFade;
            SetImageAlpha(progress);

            if (useSubtlePop && targetImage != null)
            {
                float scale = Mathf.Lerp(popScale, 1f, progress);
                targetImage.rectTransform.localScale = initialScale * scale;
            }

            yield return null;
        }

        SetImageAlpha(1f);
        ResetImageScale();
        transitionRoutine = null;
    }

    void ApplySlide(int index)
    {
        if (targetImage != null)
        {
            targetImage.sprite = processImages[index];
            targetImage.enabled = processImages[index] != null;
            targetImage.preserveAspect = preserveAspect;
        }

        if (captionText != null)
        {
            captionText.text = captions != null && index < captions.Length
                ? captions[index]
                : string.Empty;
        }
    }

    void SetImageAlpha(float alpha)
    {
        if (targetImage == null)
            return;

        Color color = targetImage.color;
        color.a = Mathf.Clamp01(alpha);
        targetImage.color = color;
    }

    void ResetImageScale()
    {
        if (targetImage == null)
            return;

        targetImage.rectTransform.localScale = initialScale;
    }

    float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
