using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeMenuPager : MonoBehaviour
{
    public enum BottomTab
    {
        Shop,
        Home,
        Settings
    }

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public List<RectTransform> pages = new List<RectTransform>();
    public float snapSpeed = 10f;
    public float settleVelocityThreshold = 150f;
    public float autoSnapTolerance = 0.0025f;
    public bool normalizePageTransforms = true;

    [Header("Swipe Feel")]
    [Range(0.01f, 0.40f)] public float swipeNormalizedThreshold = 0.08f;
    public float swipeVelocityThreshold = 350f;

    [Header("Shop Button")]
    public RectTransform shopButtonRoot;
    public GameObject shopBlueVisual;
    public GameObject shopYellowVisual;

    [Header("Home Button")]
    public RectTransform homeButtonRoot;
    public GameObject homeBlueVisual;
    public GameObject homeYellowVisual;

    [Header("Settings Button")]
    public RectTransform settingButtonRoot;
    public GameObject settingBlueVisual;
    public GameObject settingYellowVisual;

    [Header("Button State")]
    public float buttonBaseYOffset = 12f;
    public float activeYOffset = 28f;
    public float inactiveYOffset = 0f;

    [Header("Button Draw Order")]
    public bool keepActiveButtonOnTop = true;

    [Header("Page Index")]
    public bool hasShopPage = false;
    public bool hasSettingsPage = true;
    public int shopPageIndex = 0;
    public int firstLevelPageIndex = 1;
    public int lastLevelPageIndex = 3;
    public int settingsPageIndex = 4;

    [Header("Settings Layout")]
    public bool autoNudgeSettingsContent = true;
    public float settingsContentYOffset = 80f;

    [Header("Level Layout")]
    public bool autoNudgeLevelContent = true;
    public float levelContentYOffset = -280f;

    [Header("Nested Canvas")]
    public bool disableNestedCanvasScaler = false;
    public bool disableNestedGraphicRaycaster = false;

    [Header("Level Index Auto Assign")]
    public bool autoAssignLevelButtonIndices = true;
    public int autoAssignStartLevelIndex = 0;
    public bool autoAssignByGridPosition = true;

    private int currentPageIndex = 1;
    private bool isDragging = false;
    private bool isSnapping = false;
    private float dragStartNormalizedPos = 0f;
    private int dragStartPageIndex = 0;
    private bool swipeEnabled = true;

    void Start()
    {
        PreparePagesForPaging();
        AutoAssignLevelButtonIndicesIfNeeded();
        SnapToPage(currentPageIndex, true);
        UpdateBottomNav();
    }

    public void GoToShop()
    {
        ResolvePageLayout(out bool hasValidShop, out int shopIndex, out int homeStartIndex, out _, out _);
        SnapToPage(hasValidShop ? shopIndex : homeStartIndex);
    }

    public void GoToHome()
    {
        ResolvePageLayout(out _, out _, out int homeStartIndex, out _, out _);
        SnapToPage(homeStartIndex);
    }

    public void GoToSettings()
    {
        ResolvePageLayout(out _, out _, out _, out _, out int settingsIndex);
        SnapToPage(settingsIndex);
    }

    public void OnEndDrag()
    {
        if (!CanSwipe()) return;
        if (pages.Count <= 1) return;

        isDragging = false;

        float endPos = scrollRect.horizontalNormalizedPosition;
        float deltaPos = endPos - dragStartNormalizedPos;
        float velocityX = scrollRect.velocity.x;

        bool passedDistanceThreshold = Mathf.Abs(deltaPos) >= swipeNormalizedThreshold;
        bool passedVelocityThreshold = Mathf.Abs(velocityX) >= swipeVelocityThreshold;

        if (passedDistanceThreshold || passedVelocityThreshold)
        {
            float directionSignal = passedDistanceThreshold ? deltaPos : velocityX;
            int direction = directionSignal >= 0f ? 1 : -1;
            int targetIndex = Mathf.Clamp(dragStartPageIndex + direction, 0, pages.Count - 1);
            SnapToPage(targetIndex);
            return;
        }

        SnapToPage(GetNearestPageIndex());
    }

    public void OnBeginDrag()
    {
        if (!CanSwipe()) return;
        if (pages.Count <= 1) return;

        isDragging = true;
        isSnapping = false;
        dragStartNormalizedPos = scrollRect.horizontalNormalizedPosition;
        dragStartPageIndex = GetNearestPageIndex();
        StopAllCoroutines();
    }

    void Update()
    {
        if (!CanSwipe()) return;
        if (scrollRect == null || pages.Count <= 1) return;
        if (isDragging || isSnapping) return;

        if (Mathf.Abs(scrollRect.velocity.x) > settleVelocityThreshold)
            return;

        int nearestIndex = GetNearestPageIndex();
        float target = GetNormalizedPositionForPage(nearestIndex);

        if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - target) > autoSnapTolerance)
            SnapToPage(nearestIndex);
        else
            SetCurrentPageFromPosition();
    }

    public void SnapToPage(int pageIndex, bool immediate = false)
    {
        if (scrollRect == null || pages.Count == 0)
            return;

        pageIndex = Mathf.Clamp(pageIndex, 0, pages.Count - 1);
        bool pageChanged = pageIndex != currentPageIndex;

        StopAllCoroutines();
        scrollRect.velocity = Vector2.zero;

        if (immediate)
        {
            isSnapping = false;
            scrollRect.horizontalNormalizedPosition = GetNormalizedPositionForPage(pageIndex);
            SetCurrentPageFromPosition();
            UpdateBottomNav();
            return;
        }

        if (pageChanged)
            SfxManager.Instance?.PlaySwipePage();

        isSnapping = true;
        StartCoroutine(SmoothSnap(pageIndex));
    }

    public void SetSwipeEnabled(bool enabled)
    {
        swipeEnabled = enabled;

        if (scrollRect == null)
            return;

        scrollRect.horizontal = enabled;

        if (!enabled)
        {
            isDragging = false;
            isSnapping = false;
            scrollRect.StopMovement();
            StopAllCoroutines();
        }
    }

    IEnumerator SmoothSnap(int pageIndex)
    {
        float target = GetNormalizedPositionForPage(pageIndex);

        while (Mathf.Abs(scrollRect.horizontalNormalizedPosition - target) > 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition,
                target,
                Time.deltaTime * snapSpeed
            );

            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = target;
        scrollRect.velocity = Vector2.zero;
        isSnapping = false;
        SetCurrentPageFromPosition();
        UpdateBottomNav();
    }

    int GetNearestPageIndex()
    {
        float nearestDistance = float.MaxValue;
        int nearestIndex = currentPageIndex;

        for (int i = 0; i < pages.Count; i++)
        {
            float target = GetNormalizedPositionForPage(i);
            float distance = Mathf.Abs(scrollRect.horizontalNormalizedPosition - target);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    float GetNormalizedPositionForPage(int pageIndex)
    {
        if (pages.Count <= 1) return 0f;
        return (float)pageIndex / (pages.Count - 1);
    }

    void UpdateBottomNav()
    {
        ResolvePageLayout(
            out bool hasValidShop,
            out int clampedShopPage,
            out int clampedFirstLevelPage,
            out int clampedLastLevelPage,
            out int clampedSettingsPage);

        // Make nav states exclusive so only one tab can be active at a time.
        bool isShop = false;
        bool isHome = false;
        bool isSettings = false;

        if (clampedSettingsPage >= 0 && currentPageIndex == clampedSettingsPage)
            isSettings = true;
        else if (hasValidShop && currentPageIndex == clampedShopPage)
            isShop = true;
        else if (currentPageIndex >= clampedFirstLevelPage && currentPageIndex <= clampedLastLevelPage)
            isHome = true;
        else
            isHome = true; // fallback

        ApplyButtonState(shopButtonRoot, shopBlueVisual, shopYellowVisual, isShop);
        ApplyButtonState(homeButtonRoot, homeBlueVisual, homeYellowVisual, isHome);
        ApplyButtonState(settingButtonRoot, settingBlueVisual, settingYellowVisual, isSettings);
    }

    void ApplyButtonState(RectTransform buttonRoot, GameObject blueVisual, GameObject yellowVisual, bool active)
    {
        if (buttonRoot != null)
        {
            // Keep bottom nav buttons clickable:
            // reversed (Y=180) UI transforms can be ignored by GraphicRaycaster.
            buttonRoot.localRotation = Quaternion.identity;

            Vector2 pos = buttonRoot.anchoredPosition;
            pos.y = buttonBaseYOffset + (active ? activeYOffset : inactiveYOffset);
            buttonRoot.anchoredPosition = pos;

            // Keep selected tab visually in front of other tabs.
            if (keepActiveButtonOnTop && active)
                buttonRoot.SetAsLastSibling();
        }

        if (blueVisual != null)
            blueVisual.SetActive(!active);

        if (yellowVisual != null)
            yellowVisual.SetActive(active);
    }

    void ResolvePageLayout(
        out bool hasValidShop,
        out int clampedShopPage,
        out int clampedFirstLevelPage,
        out int clampedLastLevelPage,
        out int clampedSettingsPage)
    {
        if (pages == null || pages.Count == 0)
        {
            hasValidShop = false;
            clampedShopPage = 0;
            clampedFirstLevelPage = 0;
            clampedLastLevelPage = 0;
            clampedSettingsPage = -1;
            return;
        }

        int maxIndex = pages.Count - 1;

        bool hasValidSettings = hasSettingsPage && settingsPageIndex >= 0;
        clampedSettingsPage = hasValidSettings ? Mathf.Clamp(settingsPageIndex, 0, maxIndex) : -1;
        clampedShopPage = Mathf.Clamp(shopPageIndex, 0, maxIndex);

        hasValidShop = hasShopPage && (clampedSettingsPage < 0 || clampedShopPage != clampedSettingsPage);

        if (hasValidShop)
        {
            clampedFirstLevelPage = Mathf.Clamp(firstLevelPageIndex, 0, maxIndex);
            clampedLastLevelPage = Mathf.Clamp(lastLevelPageIndex, clampedFirstLevelPage, maxIndex);
            return;
        }

        // No dedicated shop page:
        // treat all non-settings pages as Home/Level pages.
        clampedFirstLevelPage = 0;
        clampedLastLevelPage = maxIndex;

        if (clampedSettingsPage == 0)
            clampedFirstLevelPage = Mathf.Min(1, maxIndex);
        else if (clampedSettingsPage > 0)
            clampedLastLevelPage = clampedSettingsPage - 1;
        else
            clampedLastLevelPage = maxIndex;

        clampedLastLevelPage = Mathf.Clamp(clampedLastLevelPage, clampedFirstLevelPage, maxIndex);
    }

    void SetCurrentPageFromPosition()
    {
        currentPageIndex = GetNearestPageIndex();
    }

    void PreparePagesForPaging()
    {
        if (!normalizePageTransforms) return;
        if (scrollRect == null || scrollRect.viewport == null || content == null) return;
        if (pages == null || pages.Count == 0) return;

        HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            // We only want horizontal paging. Let each page keep its full viewport height.
            hlg.childControlHeight = false;
            hlg.childForceExpandHeight = false;
        }

        Canvas.ForceUpdateCanvases();

        float viewportWidth = scrollRect.viewport.rect.width;
        float viewportHeight = scrollRect.viewport.rect.height;
        if (viewportWidth <= 0f || viewportHeight <= 0f) return;

        for (int i = 0; i < pages.Count; i++)
        {
            RectTransform page = pages[i];
            if (page == null) continue;

            if (page.parent != content)
                page.SetParent(content, false);

            if (!page.gameObject.activeSelf)
                page.gameObject.SetActive(true);

            DisableNestedCanvasComponents(page);
            NudgeSettingsContent(page);
            NudgeLevelContent(page);

            page.localScale = Vector3.one;
            page.localRotation = Quaternion.identity;

            // Keep horizontal paging deterministic even when page came from another canvas.
            page.anchorMin = new Vector2(0f, 0f);
            page.anchorMax = new Vector2(0f, 1f);
            page.pivot = new Vector2(0.5f, 0.5f);
            page.anchoredPosition = Vector2.zero;

            Vector2 size = page.sizeDelta;
            size.x = viewportWidth;
            size.y = 0f;
            page.sizeDelta = size;

            LayoutElement element = page.GetComponent<LayoutElement>();
            if (element == null)
                element = page.gameObject.AddComponent<LayoutElement>();

            element.minWidth = viewportWidth;
            element.preferredWidth = viewportWidth;
            element.flexibleWidth = 0f;
            element.minHeight = viewportHeight;
            element.preferredHeight = viewportHeight;
            element.flexibleHeight = 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    void DisableNestedCanvasComponents(RectTransform page)
    {
        if (page == null) return;

        Canvas nestedCanvas = page.GetComponent<Canvas>();
        if (nestedCanvas != null)
            nestedCanvas.overrideSorting = false;

        CanvasScaler nestedScaler = page.GetComponent<CanvasScaler>();
        if (nestedScaler != null)
            nestedScaler.enabled = !ShouldDisableNestedCanvasScaler(page);

        GraphicRaycaster nestedRaycaster = page.GetComponent<GraphicRaycaster>();
        if (nestedRaycaster != null)
            nestedRaycaster.enabled = !disableNestedGraphicRaycaster;
    }

    bool ShouldDisableNestedCanvasScaler(RectTransform page)
    {
        if (!disableNestedCanvasScaler)
            return false;

        // Keep Settings page scaler alive so slider/text sizing stays stable.
        if (page.name == "SettingCanvas")
            return false;

        return true;
    }

    void NudgeSettingsContent(RectTransform page)
    {
        if (!autoNudgeSettingsContent || page == null) return;

        Transform contentNode = page.Find("SettingContent");
        if (contentNode == null) return;

        RectTransform rt = contentNode as RectTransform;
        if (rt == null) return;

        Vector2 anchored = rt.anchoredPosition;
        anchored.y = settingsContentYOffset;
        rt.anchoredPosition = anchored;
    }

    void NudgeLevelContent(RectTransform page)
    {
        if (!autoNudgeLevelContent || page == null) return;

        Transform levelNode = FindLevelContentNode(page);
        if (levelNode == null) return;

        RectTransform rt = levelNode as RectTransform;
        if (rt == null) return;

        Vector2 anchored = rt.anchoredPosition;
        anchored.y = levelContentYOffset;
        rt.anchoredPosition = anchored;
    }

    Transform FindLevelContentNode(RectTransform page)
    {
        for (int i = 0; i < page.childCount; i++)
        {
            Transform child = page.GetChild(i);
            if (child == null) continue;

            string nodeName = child.name;
            if (nodeName.StartsWith("LevelGrid_"))
                return child;

            if (nodeName.ToLower().Contains("levelindex"))
                return child;
        }

        return null;
    }

    bool CanSwipe()
    {
        return swipeEnabled && scrollRect != null && scrollRect.horizontal;
    }

    void AutoAssignLevelButtonIndicesIfNeeded()
    {
        if (!autoAssignLevelButtonIndices || pages == null || pages.Count == 0)
            return;

        ResolvePageLayout(
            out _,
            out _,
            out int levelPageStart,
            out int levelPageEnd,
            out _);

        if (levelPageEnd < levelPageStart)
            return;

        int nextLevelIndex = Mathf.Max(0, autoAssignStartLevelIndex);

        for (int pageIndex = levelPageStart; pageIndex <= levelPageEnd; pageIndex++)
        {
            if (pageIndex < 0 || pageIndex >= pages.Count)
                continue;

            RectTransform page = pages[pageIndex];
            if (page == null)
                continue;

            MainMenuLevelButton[] rawButtons = page.GetComponentsInChildren<MainMenuLevelButton>(true);
            if (rawButtons == null || rawButtons.Length == 0)
                continue;

            List<MainMenuLevelButton> pageButtons = new List<MainMenuLevelButton>(rawButtons);
            if (autoAssignByGridPosition)
            {
                pageButtons.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;

                    Vector3 localA = page.InverseTransformPoint(a.transform.position);
                    Vector3 localB = page.InverseTransformPoint(b.transform.position);

                    if (!Mathf.Approximately(localA.y, localB.y))
                        return localB.y.CompareTo(localA.y); // top -> bottom

                    return localA.x.CompareTo(localB.x); // left -> right
                });
            }

            for (int i = 0; i < pageButtons.Count; i++)
            {
                MainMenuLevelButton levelButton = pageButtons[i];
                if (levelButton == null)
                    continue;

                levelButton.SetLevelIndex(nextLevelIndex);
                nextLevelIndex++;
            }
        }
    }

    public void SetBottomNavTab(BottomTab tab)
    {
        ApplyButtonState(shopButtonRoot, shopBlueVisual, shopYellowVisual, tab == BottomTab.Shop);
        ApplyButtonState(homeButtonRoot, homeBlueVisual, homeYellowVisual, tab == BottomTab.Home);
        ApplyButtonState(settingButtonRoot, settingBlueVisual, settingYellowVisual, tab == BottomTab.Settings);
    }
}
