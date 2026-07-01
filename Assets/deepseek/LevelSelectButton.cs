using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelSelectButton : MonoBehaviour
{
    public TextMeshProUGUI labelTMP;
    public Button button;

    [Header("Stars")]
    public Image[] starImages;
    public Sprite emptyStarSprite;
    public Sprite filledStarSprite;

    [Header("Lock")]
    public GameObject lockedVisual;
    public GameObject unlockedVisual;
    public bool disableButtonWhenLocked = true;
    public bool hideStarsWhenLocked = false;

    [Header("Lock Icon")]
    public Image lockIconImage;
    public Sprite lockIconSprite;
    public bool autoCreateLockIconIfMissing = true;
    public Vector2 lockIconSize = new Vector2(44f, 44f);
    public Vector2 lockIconAnchoredPosition = Vector2.zero;
    public Color lockIconColor = new Color(1f, 0.92f, 0.15f, 1f);
    public bool bringLockIconToFront = true;
    public bool hideLabelWhenLocked = false;

    [Header("Inspector Test Lock")]
    public LevelLockInspectorMode lockTestMode = LevelLockInspectorMode.UseProgress;

    private int levelIndex;
    private Action<int> onClick;
    private static Sprite generatedLockSprite;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        EnsureLockIcon();
        EnsureStableClickArea();
    }

    void OnEnable()
    {
        RefreshVisuals();
    }

    void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (isActiveAndEnabled)
            RefreshVisuals();
    }

    public void Setup(int index, string label, Action<int> callback)
    {
        levelIndex = index;
        onClick = callback;

        if (labelTMP != null)
            labelTMP.text = label;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        EnsureStableClickArea();
        RefreshVisuals();
    }

    public void SetLevelIndex(int index)
    {
        levelIndex = index;
        RefreshVisuals();
    }

    public void SetLockTestMode(LevelLockInspectorMode mode, bool refresh = true)
    {
        lockTestMode = mode;

        if (refresh)
            RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        RefreshStars();
        RefreshLockState();
    }

    public void RefreshStars()
    {
        int stars = LevelProgress.GetStars(levelIndex);
        bool unlocked = IsUnlocked();

        if (starImages == null || starImages.Length == 0)
            return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            if (hideStarsWhenLocked)
                starImages[i].gameObject.SetActive(unlocked);

            starImages[i].sprite = i < stars ? filledStarSprite : emptyStarSprite;
        }
    }

    public void RefreshLockState()
    {
        bool unlocked = IsUnlocked();
        EnsureLockIcon();

        if (button != null && disableButtonWhenLocked)
            button.interactable = unlocked;

        if (lockedVisual != null)
            lockedVisual.SetActive(!unlocked);

        if (unlockedVisual != null)
            unlockedVisual.SetActive(unlocked);

        if (labelTMP != null && hideLabelWhenLocked)
            labelTMP.gameObject.SetActive(unlocked);

        if (lockIconImage != null)
        {
            lockIconImage.gameObject.SetActive(!unlocked);
            lockIconImage.sprite = lockIconSprite != null ? lockIconSprite : GetGeneratedLockSprite();
            lockIconImage.color = lockIconColor;
            lockIconImage.raycastTarget = false;

            RectTransform iconRt = lockIconImage.rectTransform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = lockIconAnchoredPosition;
            iconRt.sizeDelta = lockIconSize;
            iconRt.localScale = Vector3.one;

            if (bringLockIconToFront)
                lockIconImage.transform.SetAsLastSibling();
        }
    }

    void HandleClick()
    {
        if (!IsUnlocked())
        {
            Debug.Log($"[LEVEL SELECT] Level {levelIndex} masih locked. Selesaikan level {levelIndex - 1} dulu.");
            return;
        }

        onClick?.Invoke(levelIndex);
    }

    public bool IsUnlocked()
    {
        switch (lockTestMode)
        {
            case LevelLockInspectorMode.ForceLocked:
                return false;
            case LevelLockInspectorMode.ForceUnlocked:
                return true;
            default:
                return LevelProgress.IsUnlocked(levelIndex);
        }
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

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null)
                continue;

            g.raycastTarget = (g == target);
        }
    }

    void EnsureLockIcon()
    {
        if (lockIconImage != null || !autoCreateLockIconIfMissing)
            return;

        Transform existing = transform.Find("LockIcon_Auto");
        if (existing != null)
        {
            lockIconImage = existing.GetComponent<Image>();
            if (lockIconImage != null)
                return;
        }

        GameObject go = new GameObject("LockIcon_Auto", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        lockIconImage = go.GetComponent<Image>();
        lockIconImage.raycastTarget = false;
        lockIconImage.sprite = lockIconSprite != null ? lockIconSprite : GetGeneratedLockSprite();
        lockIconImage.color = lockIconColor;

        RectTransform iconRt = lockIconImage.rectTransform;
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = lockIconAnchoredPosition;
        iconRt.sizeDelta = lockIconSize;
        iconRt.localScale = Vector3.one;

        if (bringLockIconToFront)
            go.transform.SetAsLastSibling();
    }

    Sprite GetGeneratedLockSprite()
    {
        if (generatedLockSprite != null)
            return generatedLockSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated_LevelLockIcon";
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool pixel = false;

                bool body = x >= 14 && x <= 50 && y >= 14 && y <= 38;
                bool shackleOuter = IsInEllipseRing(x, y, 32, 39, 19, 19, 6);
                bool shackleCutBottom = y < 35 && x > 19 && x < 45;
                bool keyHole = (x >= 29 && x <= 35 && y >= 22 && y <= 31) ||
                               ((x - 32) * (x - 32) + (y - 31) * (y - 31) <= 18);

                if (body || (shackleOuter && !shackleCutBottom))
                    pixel = true;

                if (keyHole)
                    pixel = false;

                texture.SetPixel(x, y, pixel ? solid : transparent);
            }
        }

        texture.Apply();

        generatedLockSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
        generatedLockSprite.name = "Generated_LevelLockIcon";
        generatedLockSprite.hideFlags = HideFlags.HideAndDontSave;

        return generatedLockSprite;
    }

    bool IsInEllipseRing(int x, int y, int cx, int cy, int rx, int ry, int thickness)
    {
        float dx = (x - cx) / (float)rx;
        float dy = (y - cy) / (float)ry;
        float outer = dx * dx + dy * dy;

        float innerRx = Mathf.Max(1, rx - thickness);
        float innerRy = Mathf.Max(1, ry - thickness);
        float innerDx = (x - cx) / innerRx;
        float innerDy = (y - cy) / innerRy;
        float inner = innerDx * innerDx + innerDy * innerDy;

        return outer <= 1f && inner >= 1f;
    }
}
