using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuCanvasManager : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject homeCanvas;
    public GameObject shopCanvas;
    public GameObject settingsCanvas;
    public bool autoResolveShopCanvas = true;
    public bool keepHomeCanvasActiveWhenShopIsNested = true;

    [Header("About Panel")]
    public GameObject aboutPanel;
    public bool autoResolveAboutPanel = true;
    public bool hideBottomBarWhenAboutOpen = true;

    [Header("Single Bottom Bar Mode")]
    public bool usePagerAsSingleSource = true;
    public HomeMenuPager pager;
    
    [Header("Separate Settings Mode")]
    public bool separateSettingsFromHome = true;
    public bool detachSettingsFromPagerContentOnStart = true;
    public bool disableHomeSwipeWhenSettingsVisible = true;
    public bool syncSettingsCanvasScalerWithHome = true;

    [Header("Global Bottom Bar")]
    public bool keepBottomBarVisibleAcrossPages = true;
    public RectTransform bottomBarRoot;
    public Canvas globalBottomBarCanvas;
    public int globalBottomBarSortingOrder = 50;

    [Header("Bottom Bar Layout")]
    public bool autoApplyBottomBarLayout = true;
    public float bottomBarYOffset = 0f;
    public float bottomBarHeight = 190f;
    public float bottomBarLeftInset = 0f;
    public float bottomBarRightInset = 0f;

    [Header("Coin HUD")]
    public bool enableCoinHud = true;
    public bool autoResolveCoinHud = true;
    public GameObject coinContainer;
    public TextMeshProUGUI coinText;
    public bool autoResolveShopCoinHud = true;
    public GameObject shopCoinContainer;
    public TextMeshProUGUI shopCoinText;
    public string coinPrefix = "";
    public bool showCoinPrefix = false;
    public bool setStartingCoinsIfNoSave = false;
    public int startingCoins = 0;
    public bool forceSetStartingCoinsOnStart = false;

    [Header("Current Skin Display")]
    public bool refreshSkinDisplaysOnPageChange = true;
    public bool autoResolveCurrentSkinDisplays = true;
    public CurrentSkinDisplayUI[] currentSkinDisplays;

    private RectTransform cachedHomeSafeArea;
    private bool isAboutOpen;

    void Awake()
    {
        AutoResolvePager();
        AutoResolveBottomBar();
        AutoResolveCoinHud();
        AutoResolveShopCoinHud();
        AutoResolveShopCanvas();
        AutoResolveAboutPanel();
        AutoResolveCurrentSkinDisplays();
        InitializeSeparateSettingsMode();
        InitializeGlobalBottomBarMode();
    }

    void Start()
    {
        EnsureCoinsInitializedIfNeeded();
        ShowHome();
        CloseAboutWithoutNavigation();
        RefreshCoinText();
        RefreshCurrentSkinDisplays();
        SyncGlobalBottomBarCanvasToSafeArea();
        ApplyBottomBarLayout();
    }

    void OnEnable()
    {
        CoinWallet.CoinsChanged += HandleCoinsChanged;
    }

    void OnDisable()
    {
        CoinWallet.CoinsChanged -= HandleCoinsChanged;
    }

    void LateUpdate()
    {
        SyncGlobalBottomBarCanvasToSafeArea();

        if (!isAboutOpen)
            ApplyBottomBarLayout();
    }

    public void ShowHome()
    {
        CloseAboutWithoutNavigation();
        RefreshCurrentSkinDisplays();

        if (separateSettingsFromHome)
        {
            SetCanvasState(showHomeCanvas: true, showShopCanvas: false, showSettingsCanvas: false);
            SetHomeSwipeEnabled(true);
            SetBottomTab(HomeMenuPager.BottomTab.Home);

            if (pager != null)
                pager.GoToHome();
            return;
        }

        if (UsePagerMode())
        {
            bool keepSettingsPageActive = IsSettingsCanvasUsedByPager();
            SetCanvasState(
                showHomeCanvas: true,
                showShopCanvas: false,
                showSettingsCanvas: keepSettingsPageActive);
            pager.GoToHome();
            return;
        }

        SetCanvasState(showHomeCanvas: true, showShopCanvas: false, showSettingsCanvas: false);
    }

    public void ShowShop()
    {
        CloseAboutWithoutNavigation();
        RefreshCurrentSkinDisplays();

        if (separateSettingsFromHome)
        {
            bool hasShopCanvas = shopCanvas != null;
            bool nestedUnderHome = hasShopCanvas && homeCanvas != null && shopCanvas.transform.IsChildOf(homeCanvas.transform);
            bool keepHomeVisible = keepHomeCanvasActiveWhenShopIsNested && nestedUnderHome;

            SetCanvasState(
                showHomeCanvas: !hasShopCanvas || keepHomeVisible,
                showShopCanvas: hasShopCanvas,
                showSettingsCanvas: false);
            SetHomeSwipeEnabled(!hasShopCanvas);
            SetBottomTab(hasShopCanvas ? HomeMenuPager.BottomTab.Shop : HomeMenuPager.BottomTab.Home);

            if (!hasShopCanvas && pager != null)
                pager.GoToShop();

            if (!hasShopCanvas)
                Debug.LogWarning("[MAIN MENU] ShowShop dipanggil tapi shopCanvas belum ter-assign.");

            return;
        }

        if (UsePagerMode())
        {
            bool keepSettingsPageActive = IsSettingsCanvasUsedByPager();
            SetCanvasState(
                showHomeCanvas: true,
                showShopCanvas: false,
                showSettingsCanvas: keepSettingsPageActive);
            pager.GoToShop();
            return;
        }

        SetCanvasState(showHomeCanvas: false, showShopCanvas: true, showSettingsCanvas: false);
    }

    public void ShowSettings()
    {
        CloseAboutWithoutNavigation();
        RefreshCurrentSkinDisplays();

        if (separateSettingsFromHome)
        {
            SetCanvasState(showHomeCanvas: false, showShopCanvas: false, showSettingsCanvas: true);

            if (disableHomeSwipeWhenSettingsVisible)
                SetHomeSwipeEnabled(false);

            SetBottomTab(HomeMenuPager.BottomTab.Settings);

            return;
        }

        if (UsePagerMode())
        {
            bool keepSettingsPageActive = IsSettingsCanvasUsedByPager();
            SetCanvasState(
                showHomeCanvas: true,
                showShopCanvas: false,
                showSettingsCanvas: keepSettingsPageActive);
            pager.GoToSettings();
            return;
        }

        SetCanvasState(showHomeCanvas: false, showShopCanvas: false, showSettingsCanvas: true);
    }

    public void ShowAbout()
    {
        AutoResolveAboutPanel();

        if (aboutPanel == null)
        {
            Debug.LogWarning("[MAIN MENU] ShowAbout dipanggil, tapi aboutPanel belum di-assign. Buat object PanelAbout/AboutPanel lalu drag ke MainMenuCanvasManager.");
            return;
        }

        isAboutOpen = true;
        SetCanvasState(showHomeCanvas: false, showShopCanvas: false, showSettingsCanvas: true);
        SetHomeSwipeEnabled(false);
        SetBottomTab(HomeMenuPager.BottomTab.Settings);
        SetAboutPanelActive(true);
        SetBottomBarVisible(!hideBottomBarWhenAboutOpen);
    }

    public void CloseAbout()
    {
        CloseAboutWithoutNavigation();
        ShowSettings();
    }

    void CloseAboutWithoutNavigation()
    {
        isAboutOpen = false;
        SetAboutPanelActive(false);
        SetBottomBarVisible(true);
    }

    void SetAboutPanelActive(bool active)
    {
        if (aboutPanel == null)
            return;

        if (aboutPanel.activeSelf != active)
            aboutPanel.SetActive(active);

        if (active)
            aboutPanel.transform.SetAsLastSibling();
    }

    void SetBottomBarVisible(bool visible)
    {
        if (bottomBarRoot == null)
            return;

        if (bottomBarRoot.gameObject.activeSelf != visible)
            bottomBarRoot.gameObject.SetActive(visible);
    }

    bool UsePagerMode()
    {
        return usePagerAsSingleSource && pager != null && !separateSettingsFromHome;
    }

    void AutoResolvePager()
    {
        if (pager != null) return;

        if (homeCanvas != null)
            pager = homeCanvas.GetComponent<HomeMenuPager>();

        if (pager != null) return;

        pager = FindObjectOfType<HomeMenuPager>(true);
    }

    void AutoResolveBottomBar()
    {
        if (bottomBarRoot != null) return;
        if (homeCanvas == null) return;

        Transform direct = homeCanvas.transform.Find("SafeArea/BottomBar");
        if (direct != null)
        {
            bottomBarRoot = direct as RectTransform;
            return;
        }

        Transform nested = FindChildByName(homeCanvas.transform, "BottomBar");
        if (nested != null)
            bottomBarRoot = nested as RectTransform;
    }

    void SetCanvasState(bool showHomeCanvas, bool showShopCanvas, bool showSettingsCanvas)
    {
        if (homeCanvas != null) homeCanvas.SetActive(showHomeCanvas);
        if (shopCanvas != null) shopCanvas.SetActive(showShopCanvas);
        if (settingsCanvas != null) settingsCanvas.SetActive(showSettingsCanvas);

        UpdateCoinVisibility(showHomeCanvas, showShopCanvas, showSettingsCanvas);
        RefreshCoinText();
    }

    void SetHomeSwipeEnabled(bool enabled)
    {
        if (pager == null) return;
        pager.SetSwipeEnabled(enabled);
    }

    void SetBottomTab(HomeMenuPager.BottomTab tab)
    {
        if (pager == null) return;
        pager.SetBottomNavTab(tab);
    }

    void InitializeSeparateSettingsMode()
    {
        if (!separateSettingsFromHome)
            return;

        if (settingsCanvas == null || pager == null)
            return;

        if (detachSettingsFromPagerContentOnStart)
            DetachSettingsFromPagerContent();

        if (syncSettingsCanvasScalerWithHome)
            SyncSettingsCanvasScalerToHome();

        RemoveSettingsFromPagerPages();
    }

    void InitializeGlobalBottomBarMode()
    {
        if (!keepBottomBarVisibleAcrossPages)
            return;

        if (bottomBarRoot == null)
            return;

        RectTransform globalCanvasRt = EnsureGlobalBottomBarCanvas();
        if (globalCanvasRt == null)
            return;

        if (bottomBarRoot.parent != globalCanvasRt)
            bottomBarRoot.SetParent(globalCanvasRt, false);

        SyncGlobalBottomBarCanvasToSafeArea();
        ApplyBottomBarLayout();
    }

    void DetachSettingsFromPagerContent()
    {
        if (settingsCanvas == null || pager == null || pager.content == null)
            return;

        Transform settingsTransform = settingsCanvas.transform;
        if (!settingsTransform.IsChildOf(pager.content))
            return;

        Transform newParent = null;
        if (pager != null && pager.scrollRect != null)
            newParent = pager.scrollRect.transform.parent; // usually SafeArea

        if (newParent == null && homeCanvas != null)
            newParent = homeCanvas.transform;

        if (newParent == null)
            newParent = transform.parent;

        if (newParent == null)
            return;

        settingsTransform.SetParent(newParent, false);

        RectTransform settingsRect = settingsTransform as RectTransform;
        RectTransform scrollRectRect = pager.scrollRect != null ? pager.scrollRect.transform as RectTransform : null;
        if (settingsRect != null && scrollRectRect != null)
        {
            settingsRect.anchorMin = scrollRectRect.anchorMin;
            settingsRect.anchorMax = scrollRectRect.anchorMax;
            settingsRect.pivot = scrollRectRect.pivot;
            settingsRect.anchoredPosition = scrollRectRect.anchoredPosition;
            settingsRect.sizeDelta = scrollRectRect.sizeDelta;
            settingsRect.localScale = Vector3.one;
            settingsRect.localRotation = Quaternion.identity;
        }

        Canvas settingsCv = settingsCanvas.GetComponent<Canvas>();
        if (settingsCv != null)
            settingsCv.overrideSorting = false;
    }

    RectTransform EnsureGlobalBottomBarCanvas()
    {
        if (globalBottomBarCanvas == null)
        {
            Transform existing = transform.parent != null ? transform.parent.Find("GlobalBottomBarCanvas") : null;
            if (existing != null)
                globalBottomBarCanvas = existing.GetComponent<Canvas>();
        }

        if (globalBottomBarCanvas == null)
        {
            GameObject go = new GameObject("GlobalBottomBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Transform parent = transform.parent != null ? transform.parent : transform;
            go.transform.SetParent(parent, false);
            globalBottomBarCanvas = go.GetComponent<Canvas>();
        }

        RectTransform canvasRt = globalBottomBarCanvas.transform as RectTransform;
        if (canvasRt == null)
            return null;

        Canvas homeCv = homeCanvas != null ? homeCanvas.GetComponent<Canvas>() : null;
        globalBottomBarCanvas.renderMode = homeCv != null ? homeCv.renderMode : RenderMode.ScreenSpaceOverlay;
        globalBottomBarCanvas.overrideSorting = true;
        globalBottomBarCanvas.sortingOrder = globalBottomBarSortingOrder;

        CanvasScaler globalScaler = globalBottomBarCanvas.GetComponent<CanvasScaler>();
        CanvasScaler homeScaler = homeCanvas != null ? homeCanvas.GetComponent<CanvasScaler>() : null;
        if (globalScaler != null && homeScaler != null)
            CopyCanvasScaler(homeScaler, globalScaler);

        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.pivot = new Vector2(0.5f, 0.5f);
        canvasRt.anchoredPosition = Vector2.zero;
        canvasRt.sizeDelta = Vector2.zero;
        canvasRt.localScale = Vector3.one;
        canvasRt.localRotation = Quaternion.identity;

        return canvasRt;
    }

    void SyncGlobalBottomBarCanvasToSafeArea()
    {
        if (!keepBottomBarVisibleAcrossPages || globalBottomBarCanvas == null)
            return;

        RectTransform safeAreaRt = GetHomeSafeArea();
        RectTransform globalCanvasRt = globalBottomBarCanvas.transform as RectTransform;
        if (safeAreaRt == null || globalCanvasRt == null)
            return;

        if (globalCanvasRt.anchorMin != safeAreaRt.anchorMin)
            globalCanvasRt.anchorMin = safeAreaRt.anchorMin;

        if (globalCanvasRt.anchorMax != safeAreaRt.anchorMax)
            globalCanvasRt.anchorMax = safeAreaRt.anchorMax;

        if (!Approximately(globalCanvasRt.pivot, safeAreaRt.pivot))
            globalCanvasRt.pivot = safeAreaRt.pivot;

        if (!Approximately(globalCanvasRt.anchoredPosition, safeAreaRt.anchoredPosition))
            globalCanvasRt.anchoredPosition = safeAreaRt.anchoredPosition;

        if (!Approximately(globalCanvasRt.sizeDelta, safeAreaRt.sizeDelta))
            globalCanvasRt.sizeDelta = safeAreaRt.sizeDelta;

        if (!Approximately(globalCanvasRt.offsetMin, safeAreaRt.offsetMin))
            globalCanvasRt.offsetMin = safeAreaRt.offsetMin;

        if (!Approximately(globalCanvasRt.offsetMax, safeAreaRt.offsetMax))
            globalCanvasRt.offsetMax = safeAreaRt.offsetMax;

        if (!Approximately(globalCanvasRt.localScale, Vector3.one))
            globalCanvasRt.localScale = Vector3.one;

        if (globalCanvasRt.localRotation != Quaternion.identity)
            globalCanvasRt.localRotation = Quaternion.identity;
    }

    void ApplyBottomBarLayout()
    {
        if (!keepBottomBarVisibleAcrossPages || !autoApplyBottomBarLayout || bottomBarRoot == null)
            return;

        // Keep a stable full-width bottom anchor, but let Inspector tune Y/height/insets.
        Vector2 targetAnchorMin = new Vector2(0f, 0f);
        Vector2 targetAnchorMax = new Vector2(1f, 0f);
        Vector2 targetPivot = new Vector2(0.5f, 0f);

        if (!Approximately(bottomBarRoot.anchorMin, targetAnchorMin))
            bottomBarRoot.anchorMin = targetAnchorMin;

        if (!Approximately(bottomBarRoot.anchorMax, targetAnchorMax))
            bottomBarRoot.anchorMax = targetAnchorMax;

        if (!Approximately(bottomBarRoot.pivot, targetPivot))
            bottomBarRoot.pivot = targetPivot;

        Vector2 anchored = bottomBarRoot.anchoredPosition;
        anchored.x = 0f;
        anchored.y = bottomBarYOffset;
        if (!Approximately(bottomBarRoot.anchoredPosition, anchored))
            bottomBarRoot.anchoredPosition = anchored;

        Vector2 size = bottomBarRoot.sizeDelta;
        size.y = Mathf.Max(0f, bottomBarHeight);
        if (!Approximately(bottomBarRoot.sizeDelta, size))
            bottomBarRoot.sizeDelta = size;

        Vector2 offsetMin = bottomBarRoot.offsetMin;
        Vector2 offsetMax = bottomBarRoot.offsetMax;
        offsetMin.x = bottomBarLeftInset;
        offsetMax.x = -bottomBarRightInset;
        if (!Approximately(bottomBarRoot.offsetMin, offsetMin))
            bottomBarRoot.offsetMin = offsetMin;

        if (!Approximately(bottomBarRoot.offsetMax, offsetMax))
            bottomBarRoot.offsetMax = offsetMax;

        if (!Approximately(bottomBarRoot.localScale, Vector3.one))
            bottomBarRoot.localScale = Vector3.one;

        if (bottomBarRoot.localRotation != Quaternion.identity)
            bottomBarRoot.localRotation = Quaternion.identity;

        if (bottomBarRoot.parent != null && bottomBarRoot.GetSiblingIndex() != bottomBarRoot.parent.childCount - 1)
            bottomBarRoot.SetAsLastSibling();
    }

    RectTransform GetHomeSafeArea()
    {
        if (cachedHomeSafeArea != null)
            return cachedHomeSafeArea;

        if (homeCanvas == null)
            return null;

        Transform direct = homeCanvas.transform.Find("SafeArea");
        if (direct != null)
        {
            cachedHomeSafeArea = direct as RectTransform;
            return cachedHomeSafeArea;
        }

        Transform nested = FindChildByName(homeCanvas.transform, "SafeArea");
        cachedHomeSafeArea = nested as RectTransform;
        return cachedHomeSafeArea;
    }

    void SyncSettingsCanvasScalerToHome()
    {
        if (homeCanvas == null || settingsCanvas == null)
            return;

        CanvasScaler homeScaler = homeCanvas.GetComponent<CanvasScaler>();
        CanvasScaler settingsScaler = settingsCanvas.GetComponent<CanvasScaler>();
        if (homeScaler == null || settingsScaler == null)
            return;

        CopyCanvasScaler(homeScaler, settingsScaler);
    }

    void CopyCanvasScaler(CanvasScaler source, CanvasScaler target)
    {
        if (source == null || target == null)
            return;

        target.uiScaleMode = source.uiScaleMode;
        target.referencePixelsPerUnit = source.referencePixelsPerUnit;
        target.scaleFactor = source.scaleFactor;
        target.referenceResolution = source.referenceResolution;
        target.screenMatchMode = source.screenMatchMode;
        target.matchWidthOrHeight = source.matchWidthOrHeight;
        target.physicalUnit = source.physicalUnit;
        target.fallbackScreenDPI = source.fallbackScreenDPI;
        target.defaultSpriteDPI = source.defaultSpriteDPI;
        target.dynamicPixelsPerUnit = source.dynamicPixelsPerUnit;
    }

    void RemoveSettingsFromPagerPages()
    {
        if (settingsCanvas == null || pager == null || pager.pages == null)
            return;

        RectTransform settingsRect = settingsCanvas.transform as RectTransform;
        if (settingsRect == null)
            return;

        pager.pages.RemoveAll(page => page == settingsRect);

        // Settings is no longer part of swipe paging in this mode.
        pager.hasSettingsPage = false;
        pager.settingsPageIndex = -1;
    }

    bool IsSettingsCanvasUsedByPager()
    {
        if (settingsCanvas == null || pager == null)
            return false;

        RectTransform settingsRect = settingsCanvas.transform as RectTransform;
        if (settingsRect == null)
            return false;

        if (pager.content != null && settingsRect.IsChildOf(pager.content))
            return true;

        for (int i = 0; i < pager.pages.Count; i++)
        {
            if (pager.pages[i] == settingsRect)
                return true;
        }

        return false;
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

    void AutoResolveShopCanvas()
    {
        if (!autoResolveShopCanvas || shopCanvas != null)
            return;

        Transform found = null;
        if (transform.parent != null)
            found = FindChildByName(transform.parent, "ShopCanvas");

        if (found == null && homeCanvas != null)
            found = FindChildByName(homeCanvas.transform, "ShopCanvas");

        if (found != null)
            shopCanvas = found.gameObject;
    }

    void AutoResolveAboutPanel()
    {
        if (!autoResolveAboutPanel || aboutPanel != null)
            return;

        Transform found = null;

        if (settingsCanvas != null)
        {
            found = FindChildByName(settingsCanvas.transform, "PanelAbout");
            if (found == null)
                found = FindChildByName(settingsCanvas.transform, "AboutPanel");
        }

        if (found == null && transform.parent != null)
        {
            found = FindChildByName(transform.parent, "PanelAbout");
            if (found == null)
                found = FindChildByName(transform.parent, "AboutPanel");
        }

        if (found != null)
            aboutPanel = found.gameObject;
    }

    void AutoResolveCurrentSkinDisplays()
    {
        if (!autoResolveCurrentSkinDisplays)
            return;

        if (currentSkinDisplays != null && currentSkinDisplays.Length > 0)
            return;

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        currentSkinDisplays = searchRoot.GetComponentsInChildren<CurrentSkinDisplayUI>(true);
    }

    public void RefreshCurrentSkinDisplays()
    {
        if (!refreshSkinDisplaysOnPageChange)
            return;

        if (currentSkinDisplays == null || currentSkinDisplays.Length == 0)
            AutoResolveCurrentSkinDisplays();

        if (currentSkinDisplays == null)
            return;

        for (int i = 0; i < currentSkinDisplays.Length; i++)
        {
            if (currentSkinDisplays[i] != null)
                currentSkinDisplays[i].Refresh();
        }
    }

    void HandleCoinsChanged(int totalCoins)
    {
        RefreshCoinText();
    }

    static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y)
            && Mathf.Approximately(a.z, b.z);
    }

    void AutoResolveCoinHud()
    {
        if (!enableCoinHud || !autoResolveCoinHud)
            return;

        if (coinContainer == null && homeCanvas != null)
        {
            Transform direct = homeCanvas.transform.Find("SafeArea/CoinContainer");
            if (direct != null)
                coinContainer = direct.gameObject;
            else
            {
                Transform nested = FindChildByName(homeCanvas.transform, "CoinContainer");
                if (nested != null)
                    coinContainer = nested.gameObject;
            }
        }

        if (coinText == null && coinContainer != null)
        {
            Transform directText = coinContainer.transform.Find("CoinBackground/CoinText");
            if (directText != null)
                coinText = directText.GetComponent<TextMeshProUGUI>();
            else
            {
                Transform nestedText = FindChildByName(coinContainer.transform, "CoinText");
                if (nestedText != null)
                    coinText = nestedText.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void AutoResolveShopCoinHud()
    {
        if (!enableCoinHud || !autoResolveShopCoinHud)
            return;

        if (shopCoinContainer == null && shopCanvas != null)
        {
            Transform direct = shopCanvas.transform.Find("CoinContainer");
            if (direct != null)
                shopCoinContainer = direct.gameObject;
            else
            {
                Transform nested = FindChildByName(shopCanvas.transform, "CoinContainer");
                if (nested != null)
                    shopCoinContainer = nested.gameObject;
            }
        }

        if (shopCoinText == null && shopCoinContainer != null)
        {
            Transform directText = shopCoinContainer.transform.Find("CoinBackground/CoinText");
            if (directText != null)
                shopCoinText = directText.GetComponent<TextMeshProUGUI>();
            else
            {
                Transform nestedText = FindChildByName(shopCoinContainer.transform, "CoinText");
                if (nestedText != null)
                    shopCoinText = nestedText.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void RefreshCoinText()
    {
        if (!enableCoinHud)
            return;

        if (coinText == null || coinContainer == null)
            AutoResolveCoinHud();

        if (shopCoinText == null || shopCoinContainer == null)
            AutoResolveShopCoinHud();

        int coins = CoinWallet.GetCoins();
        string value = showCoinPrefix ? $"{coinPrefix}{coins}" : coins.ToString();

        if (coinText != null)
            coinText.text = value;

        if (shopCoinText != null)
            shopCoinText.text = value;
    }

    void UpdateCoinVisibility(bool showHomeCanvas, bool showShopCanvas, bool showSettingsCanvas)
    {
        if (!enableCoinHud)
            return;

        if (coinContainer == null || coinText == null)
            AutoResolveCoinHud();

        if (shopCoinContainer == null || shopCoinText == null)
            AutoResolveShopCoinHud();

        bool showHomeCoin = showHomeCanvas;
        bool showShopCoin = showShopCanvas;

        if (showSettingsCanvas && !showHomeCanvas && !showShopCanvas)
        {
            showHomeCoin = false;
            showShopCoin = false;
        }

        if (coinContainer != null && coinContainer.activeSelf != showHomeCoin)
            coinContainer.SetActive(showHomeCoin);

        if (shopCoinContainer != null && shopCoinContainer.activeSelf != showShopCoin)
            shopCoinContainer.SetActive(showShopCoin);
    }

    void EnsureCoinsInitializedIfNeeded()
    {
        if (forceSetStartingCoinsOnStart)
        {
            CoinWallet.SetCoins(startingCoins);
            return;
        }

        if (!setStartingCoinsIfNoSave)
            return;

        CoinWallet.EnsureInitialized(startingCoins);
    }
}
