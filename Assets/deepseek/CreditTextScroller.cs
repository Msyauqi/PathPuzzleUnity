using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class CreditTextScroller : MonoBehaviour
{
    [Header("References")]
    public RectTransform scrollContent;
    public RectTransform viewport;
    public TMP_Text creditText;
    public bool ensureViewportMask = true;

    [Header("Scroll")]
    public bool playOnStart = true;
    public bool loop = true;
    public float scrollSpeed = 35f;
    public float resetDelay = 1.5f;
    public float extraEndPadding = 120f;
    public bool autoUseTextHeight = true;
    public bool useUnscaledTime = true;

    [Header("After One Full Scroll")]
    public GameObject showObjectAfterOneLoop;
    public bool hideObjectOnStart = true;
    public bool stopAfterFirstLoop;
    public UnityEvent onFirstLoopFinished;

    Vector2 startPosition;
    float resetTimer;
    bool isPlaying;
    bool firstLoopFinished;

    void Awake()
    {
        ResolveReferences();

        EnsureMask();

        if (scrollContent != null)
            startPosition = scrollContent.anchoredPosition;
    }

    void ResolveReferences()
    {
        RectTransform ownRect = transform as RectTransform;

        if (scrollContent == null)
            scrollContent = ownRect;

        // Common setup mistake: assigning the viewport/area as Scroll Content while
        // this script is on the text object. Treat that assigned parent as the viewport.
        if (ownRect != null && scrollContent != null && scrollContent != ownRect && ownRect.parent == scrollContent)
        {
            if (viewport == null)
                viewport = scrollContent;

            scrollContent = ownRect;
        }

        if (creditText == null && scrollContent != null)
            creditText = scrollContent.GetComponentInChildren<TMP_Text>(true);

        if (viewport == null && scrollContent != null && scrollContent.parent != null)
            viewport = scrollContent.parent as RectTransform;
    }

    void OnEnable()
    {
        resetTimer = resetDelay;
        isPlaying = playOnStart;
        firstLoopFinished = false;

        if (showObjectAfterOneLoop != null && hideObjectOnStart)
            showObjectAfterOneLoop.SetActive(false);
    }

    void Update()
    {
        if (!isPlaying || scrollContent == null)
            return;

        float maxOffset = GetScrollEndOffset();
        if (scrollContent.anchoredPosition.y < maxOffset)
        {
            scrollContent.anchoredPosition += Vector2.up * scrollSpeed * DeltaTime;
            resetTimer = resetDelay;
            return;
        }

        if (!loop)
        {
            FinishFirstLoop();
            isPlaying = false;
            return;
        }

        FinishFirstLoop();
        resetTimer -= DeltaTime;
        if (resetTimer <= 0f)
            ResetScroll();
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    [ContextMenu("Reset Scroll")]
    public void ResetScroll()
    {
        if (scrollContent != null)
            scrollContent.anchoredPosition = startPosition;

        resetTimer = resetDelay;
    }

    void FinishFirstLoop()
    {
        if (firstLoopFinished)
            return;

        firstLoopFinished = true;

        if (showObjectAfterOneLoop != null)
            showObjectAfterOneLoop.SetActive(true);

        onFirstLoopFinished?.Invoke();

        if (stopAfterFirstLoop)
            isPlaying = false;
    }

    [ContextMenu("Fit Content To Text")]
    public void FitContentToText()
    {
        ResolveReferences();

        if (scrollContent == null || creditText == null)
            return;

        creditText.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(creditText.preferredHeight, creditText.textBounds.size.y);
        Vector2 size = scrollContent.sizeDelta;
        size.y = Mathf.Max(size.y, preferredHeight + extraEndPadding);
        scrollContent.sizeDelta = size;
    }

    [ContextMenu("Ensure Viewport Mask")]
    public void EnsureMask()
    {
        if (!ensureViewportMask || viewport == null)
            return;

        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        Image image = viewport.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }

    float GetScrollEndOffset()
    {
        float contentHeight = scrollContent != null ? Mathf.Max(1f, scrollContent.rect.height) : 1f;
        float viewportHeight = viewport != null ? Mathf.Max(0f, viewport.rect.height) : 0f;

        if (autoUseTextHeight && creditText != null)
        {
            creditText.ForceMeshUpdate();
            float textHeight = Mathf.Max(creditText.preferredHeight, creditText.textBounds.size.y);
            contentHeight = Mathf.Max(contentHeight, textHeight);
        }

        return Mathf.Max(1f, contentHeight + viewportHeight + extraEndPadding);
    }

    float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
