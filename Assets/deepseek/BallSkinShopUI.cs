using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BallSkinShopUI : MonoBehaviour
{
    [System.Serializable]
    public class PriceTextBinding
    {
        public int skinIndex = 1;
        public TMP_Text priceText;
    }

    [System.Serializable]
    public class SkinCardBinding
    {
        public int skinIndex = 1;
        public Button buyButton;
        public Button equipButton;
        public Button equippedButton; // dark state / sedang dipakai
        public GameObject equipVisual;
        public GameObject equippedVisual;
        public Image skinImage;
        public TMP_Text priceText;
    }

    [Header("Optional UI")]
    public TextMeshProUGUI feedbackText;

    [Header("Fail Buy Panel")]
    public bool autoResolveFailBuyPanel = true;
    public bool hideFailBuyPanelOnEnable = true;
    public GameObject failBuyPanel;
    public Button failBuyOkButton;

    [Header("Fail Buy Pop Animation")]
    public bool animateFailBuyPanel = true;
    public RectTransform failBuyPanelRoot;
    [Min(0.01f)] public float failBuyPopDuration = 0.22f;
    [Range(0.1f, 1f)] public float failBuyStartScale = 0.65f;
    [Range(1f, 1.5f)] public float failBuyOvershootScale = 1.08f;
    public bool fadeFailBuyPanel = true;

    Coroutine failBuyPopRoutine;
    Vector3 failBuyOriginalScale = Vector3.one;
    bool hasFailBuyOriginalScale;

    [Header("Current Skin Info")]
    public bool showCurrentSkinInfo = true;
    public bool autoResolveCurrentSkinInfo = true;
    public TMP_Text usingText;
    public Image usingSkinImage;
    [Tooltip("Index array = skin index. Isi sprite preview skin supaya panel Using Skin bisa menampilkan gambar skin yang sedang dipakai.")]
    public Sprite[] skinPreviewSprites;
    public string usingTextFormat = "Skin {0}";

    [Header("Custom Price List (Opsional)")]
    public bool useCustomPriceList = true;
    public bool forceSkinZeroFree = true;
    [Tooltip("Index array = skin index. Contoh: elemen[3] adalah harga skin index 3.")]
    public int[] customSkinPrices = new int[] { 0, 15, 15, 15, 15, 15 };

    [Header("Auto Sync Price Text")]
    public bool autoSyncPriceTexts = true;
    public bool autoBuildBindingsFromBuyButtons = true;
    public bool rebuildBindingsOnEnable = true;
    public RectTransform bindingSearchRoot;
    public int autoBuildStartSkinIndex = 0;
    public string priceTextPrefix = "";
    public string priceTextSuffix = "";
    public bool forceSingleLinePriceText = true;
    public PriceTextBinding[] priceTextBindings;

    [Header("Card Button State")]
    public bool autoBuildSkinCardsFromBuyButtons = true;
    public bool rebuildSkinCardsOnEnable = true;
    public bool overrideCardButtonListeners = true;
    public bool disableEquippedButtonInteractable = true;
    public bool strictThreeStateButtons = true;
    public bool keepEquippedSkinWhenBuyFails = true;
    public SkinCardBinding[] skinCards;

    [Header("Fallback (Jika Equip/Equipped Object Tidak Ada)")]
    public bool keepBuyButtonVisibleIfStateButtonsMissing = true;
    public string buyButtonLabel = "BUY";
    public string equipButtonLabel = "Equip";
    public string equippedButtonLabel = "Equipped";

    [Header("Debug")]
    public int resetMaxSkinIndex = 200;

    int blockedEquipFrame = -1;
    int equippedSkinBeforeBlockedBuy = 0;
    string blockedBuyFailMessage = "";

    void Awake()
    {
        InitializeShopUI();
    }

    void OnEnable()
    {
        InitializeShopUI();
    }

    void InitializeShopUI()
    {
        ResolveFailBuyPanelRefs();
        WireFailBuyPanelButton();
        if (hideFailBuyPanelOnEnable)
            HideFailBuyPanel();

        ResolveCurrentSkinInfoRefs();
        ApplyPriceConfig();
        Transform searchRoot = bindingSearchRoot != null ? bindingSearchRoot : transform;
        bool needSkinCardRebuild = NeedsSkinCardRebuild(searchRoot);
        bool needPriceBindingRebuild = NeedsPriceBindingRebuild();

        if (autoBuildSkinCardsFromBuyButtons && (skinCards == null || skinCards.Length == 0 || rebuildSkinCardsOnEnable || needSkinCardRebuild))
            AutoBuildSkinCards();

        if (autoBuildBindingsFromBuyButtons && (priceTextBindings == null || priceTextBindings.Length == 0 || rebuildBindingsOnEnable || needPriceBindingRebuild || needSkinCardRebuild))
            AutoBuildPriceTextBindings();

        if (ShouldWireCardButtonListeners())
            WireCardButtonListeners();

        RefreshAllVisuals();
    }

    [ContextMenu("Apply Price Config")]
    public void ApplyPriceConfig()
    {
        if (useCustomPriceList)
            BallSkinStore.SetCustomPriceList(customSkinPrices, forceSkinZeroFree);
        else
            BallSkinStore.ClearCustomPriceList();
    }

    [ContextMenu("Auto Build Skin Cards")]
    public void AutoBuildSkinCards()
    {
        Transform searchRoot = bindingSearchRoot != null ? bindingSearchRoot : transform;
        List<Button> buyButtons = CollectBuyButtons(searchRoot);

        SortButtonsByGridPosition(searchRoot, buyButtons);

        int skinIndex = Mathf.Max(0, autoBuildStartSkinIndex);
        List<SkinCardBinding> generated = new List<SkinCardBinding>();

        for (int i = 0; i < buyButtons.Count; i++)
        {
            Button buyButton = buyButtons[i];
            if (buyButton == null)
                continue;

            Transform cardRoot = buyButton.transform.parent;
            if (cardRoot == null)
                continue;

            SkinCardBinding card = new SkinCardBinding();
            card.skinIndex = skinIndex;
            card.buyButton = buyButton;
            card.equipButton = FindButtonInCard(cardRoot, "EquipButton", "equipbutton");
            card.equippedButton = FindButtonInCard(cardRoot, "UnequipButton", "unequipbutton");
            card.equipVisual = card.equipButton != null ? card.equipButton.gameObject : FindVisualInCard(cardRoot, "EquipButton", "equipbutton");
            card.equippedVisual = card.equippedButton != null ? card.equippedButton.gameObject : FindVisualInCard(cardRoot, "UnequipButton", "unequipbutton");
            card.skinImage = FindImageInCard(cardRoot, "SkinImage", "skinimage");
            card.priceText = FindPriceTextForCard(cardRoot);

            generated.Add(card);
            skinIndex++;
        }

        skinCards = generated.ToArray();
    }

    [ContextMenu("Auto Build Price Text Bindings")]
    public void AutoBuildPriceTextBindings()
    {
        if (skinCards != null && skinCards.Length > 0)
        {
            List<PriceTextBinding> fromCards = new List<PriceTextBinding>();
            for (int i = 0; i < skinCards.Length; i++)
            {
                SkinCardBinding card = skinCards[i];
                if (card == null || card.priceText == null)
                    continue;

                PriceTextBinding binding = new PriceTextBinding();
                binding.skinIndex = card.skinIndex;
                binding.priceText = card.priceText;
                fromCards.Add(binding);
            }

            priceTextBindings = fromCards.ToArray();
            return;
        }

        Transform searchRoot = bindingSearchRoot != null ? bindingSearchRoot : transform;
        List<Button> buyButtons = CollectBuyButtons(searchRoot);

        SortButtonsByGridPosition(searchRoot, buyButtons);

        List<PriceTextBinding> bindings = new List<PriceTextBinding>();
        int skinIndex = Mathf.Max(0, autoBuildStartSkinIndex);

        for (int i = 0; i < buyButtons.Count; i++)
        {
            Button buyButton = buyButtons[i];
            Transform cardRoot = buyButton != null ? buyButton.transform.parent : null;
            TMP_Text priceText = FindPriceTextForCard(cardRoot);

            if (priceText == null)
            {
                skinIndex++;
                continue;
            }

            PriceTextBinding binding = new PriceTextBinding();
            binding.skinIndex = skinIndex;
            binding.priceText = priceText;
            bindings.Add(binding);
            skinIndex++;
        }

        priceTextBindings = bindings.ToArray();
    }

    void SortButtonsByGridPosition(Transform referenceRoot, List<Button> buyButtons)
    {
        buyButtons.Sort((a, b) =>
        {
            Vector3 la = referenceRoot.InverseTransformPoint(a.transform.position);
            Vector3 lb = referenceRoot.InverseTransformPoint(b.transform.position);

            if (!Mathf.Approximately(la.y, lb.y))
                return lb.y.CompareTo(la.y); // top -> bottom

            return la.x.CompareTo(lb.x); // left -> right
        });
    }

    List<Button> CollectBuyButtons(Transform searchRoot)
    {
        List<Button> buyButtons = new List<Button>();
        if (searchRoot == null)
            return buyButtons;

        Button[] allButtons = searchRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null)
                continue;

            if (!IsBuyButtonName(button.name))
                continue;

            buyButtons.Add(button);
        }

        return buyButtons;
    }

    bool IsBuyButtonName(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
            return false;

        string lower = buttonName.ToLower();
        if (lower.Contains("buybutton") || lower.Contains("buy_btn") || lower.Contains("buybtn"))
            return true;

        if (lower == "buy" || lower.StartsWith("buy_") || lower.StartsWith("buy"))
            return true;

        return false;
    }

    bool NeedsSkinCardRebuild(Transform searchRoot)
    {
        List<Button> detectedBuyButtons = CollectBuyButtons(searchRoot);
        if (detectedBuyButtons.Count == 0)
            return false;

        if (skinCards == null || skinCards.Length == 0)
            return true;

        HashSet<Button> mappedButtons = new HashSet<Button>();
        int mappedCount = 0;

        for (int i = 0; i < skinCards.Length; i++)
        {
            SkinCardBinding card = skinCards[i];
            if (card == null || card.buyButton == null)
                continue;

            mappedButtons.Add(card.buyButton);
            mappedCount++;
        }

        if (mappedCount != detectedBuyButtons.Count)
            return true;

        for (int i = 0; i < detectedBuyButtons.Count; i++)
        {
            if (!mappedButtons.Contains(detectedBuyButtons[i]))
                return true;
        }

        return false;
    }

    bool NeedsPriceBindingRebuild()
    {
        if (skinCards == null || skinCards.Length == 0)
            return false;

        int expected = 0;
        for (int i = 0; i < skinCards.Length; i++)
        {
            SkinCardBinding card = skinCards[i];
            if (card != null && card.priceText != null)
                expected++;
        }

        if (expected == 0)
            return false;

        int actual = priceTextBindings != null ? priceTextBindings.Length : 0;
        return actual != expected;
    }

    [ContextMenu("Wire Card Button Listeners")]
    public void WireCardButtonListeners()
    {
        if (skinCards == null)
            return;

        for (int i = 0; i < skinCards.Length; i++)
        {
            SkinCardBinding card = skinCards[i];
            if (card == null)
                continue;

            int index = card.skinIndex;

            if (card.buyButton != null)
            {
                card.buyButton.onClick.RemoveAllListeners();
                card.buyButton.onClick.AddListener(() => OnBuyPressed(index));
            }

            if (card.equipButton != null)
            {
                card.equipButton.onClick.RemoveAllListeners();
                card.equipButton.onClick.AddListener(() => OnEquipPressed(index));
            }

            if (card.equippedButton != null)
            {
                card.equippedButton.onClick.RemoveAllListeners();
                card.equippedButton.onClick.AddListener(() => OnEquippedPressed(index));
            }
        }
    }

    bool ShouldWireCardButtonListeners()
    {
        if (overrideCardButtonListeners)
            return true;

        // Fallback: kalau inspector listener belum diisi, auto-wire listener dari script.
        return AnyCardButtonMissingPersistentListener();
    }

    bool AnyCardButtonMissingPersistentListener()
    {
        if (skinCards == null || skinCards.Length == 0)
            return false;

        for (int i = 0; i < skinCards.Length; i++)
        {
            SkinCardBinding card = skinCards[i];
            if (card == null)
                continue;

            if (card.buyButton != null && card.buyButton.onClick.GetPersistentEventCount() == 0)
                return true;

            if (card.equipButton != null && card.equipButton.onClick.GetPersistentEventCount() == 0)
                return true;

            if (card.equippedButton != null && card.equippedButton.onClick.GetPersistentEventCount() == 0)
                return true;
        }

        return false;
    }

    [ContextMenu("Refresh Shop Visuals")]
    public void RefreshAllVisuals()
    {
        RefreshPriceTexts();
        RefreshCardButtons();
        RefreshCurrentSkinInfo();
        CurrentSkinDisplayUI.RefreshAllDisplays();
    }

    [ContextMenu("Rebuild + Rewire + Refresh")]
    public void RebuildRewireRefresh()
    {
        AutoBuildSkinCards();
        AutoBuildPriceTextBindings();
        WireCardButtonListeners();
        RefreshAllVisuals();
    }

    [ContextMenu("Refresh Price Texts")]
    public void RefreshPriceTexts()
    {
        if (!autoSyncPriceTexts || priceTextBindings == null)
            return;

        for (int i = 0; i < priceTextBindings.Length; i++)
        {
            PriceTextBinding binding = priceTextBindings[i];
            if (binding == null || binding.priceText == null)
                continue;

            int price = BallSkinStore.GetPrice(binding.skinIndex);
            ApplyPriceTextStyle(binding.priceText);
            binding.priceText.text = $"{priceTextPrefix}{price}{priceTextSuffix}";
        }
    }

    [ContextMenu("Refresh Card Buttons")]
    public void RefreshCardButtons()
    {
        if (skinCards == null)
            return;

        int selectedSkin = BallSkinStore.GetSelectedSkinIndex();

        for (int i = 0; i < skinCards.Length; i++)
        {
            SkinCardBinding card = skinCards[i];
            if (card == null)
                continue;

            bool unlocked = BallSkinStore.IsUnlocked(card.skinIndex);
            bool equipped = unlocked && selectedSkin == card.skinIndex;
            GameObject equipVisual = GetEquipVisual(card);
            GameObject equippedVisual = GetEquippedVisual(card);
            bool hasStateVisual = equipVisual != null || equippedVisual != null;

            if (!strictThreeStateButtons && !hasStateVisual && keepBuyButtonVisibleIfStateButtonsMissing)
            {
                if (card.buyButton != null)
                {
                    card.buyButton.gameObject.SetActive(true);
                    if (!unlocked)
                    {
                        card.buyButton.interactable = true;
                        SetButtonLabel(card.buyButton, buyButtonLabel);
                    }
                    else if (!equipped)
                    {
                        card.buyButton.interactable = true;
                        SetButtonLabel(card.buyButton, equipButtonLabel);
                    }
                    else
                    {
                        card.buyButton.interactable = false;
                        SetButtonLabel(card.buyButton, equippedButtonLabel);
                    }
                }

                continue;
            }

            if (card.buyButton != null)
            {
                card.buyButton.gameObject.SetActive(!unlocked);
                card.buyButton.interactable = !unlocked;
                if (!unlocked)
                    SetButtonLabel(card.buyButton, buyButtonLabel);
            }

            if (equipVisual != null)
                equipVisual.SetActive(unlocked && !equipped);

            if (card.equipButton != null)
            {
                card.equipButton.interactable = unlocked && !equipped;
                if (card.equipButton.gameObject.activeSelf)
                    SetButtonLabel(card.equipButton, equipButtonLabel);
            }

            if (equippedVisual != null)
                equippedVisual.SetActive(equipped);

            if (card.equippedButton != null)
            {
                card.equippedButton.interactable = !disableEquippedButtonInteractable ? equipped : false;
                if (card.equippedButton.gameObject.activeSelf)
                    SetButtonLabel(card.equippedButton, equippedButtonLabel);
            }

            // Strict mode: always enforce exclusive visibility per state.
            if (strictThreeStateButtons)
            {
                if (card.buyButton != null)
                    card.buyButton.gameObject.SetActive(!unlocked);

                if (equipVisual != null)
                    equipVisual.SetActive(unlocked && !equipped);

                if (equippedVisual != null)
                    equippedVisual.SetActive(equipped);
            }
        }
    }

    public void BuyOrSelectSkin(int skinIndex)
    {
        // Backward-compatible untuk tombol lama: BUY tidak boleh otomatis equip.
        OnBuyPressed(skinIndex);
    }

    public void OnPrimaryPressed(int skinIndex)
    {
        if (!BallSkinStore.IsUnlocked(skinIndex))
            OnBuyPressed(skinIndex);
        else
            OnEquipPressed(skinIndex);
    }

    public void OnBuyPressed(int skinIndex)
    {
        int equippedBeforeBuy = BallSkinStore.GetSelectedSkinIndex();
        bool wasUnlockedBeforeBuy = BallSkinStore.IsUnlocked(skinIndex);
        bool success = BallSkinStore.TryBuySkin(skinIndex, out string message);
        if (success && !wasUnlockedBeforeBuy)
            SfxManager.Instance?.PlayBuySuccess();

        if (!success)
        {
            if (keepEquippedSkinWhenBuyFails)
            {
                blockedEquipFrame = Time.frameCount;
                equippedSkinBeforeBlockedBuy = equippedBeforeBuy;
                blockedBuyFailMessage = message;
                BallSkinStore.RestoreSelectedSkin(equippedBeforeBuy);
            }

            SfxManager.Instance?.PlayBuyFailed();
            ShowFailBuyPanel();
        }

        if (success && !wasUnlockedBeforeBuy && BallSkinStore.IsUnlocked(skinIndex))
            message = $"Skin {skinIndex} sudah dibeli. Tekan EQUIP untuk pakai.";
        else if (success && wasUnlockedBeforeBuy)
            message = $"Skin {skinIndex} sudah dibeli. Tekan EQUIP untuk pakai.";

        SetFeedback(message);
        RefreshAllVisuals();
    }

    public void OnEquipPressed(int skinIndex)
    {
        if (keepEquippedSkinWhenBuyFails && blockedEquipFrame == Time.frameCount)
        {
            BallSkinStore.RestoreSelectedSkin(equippedSkinBeforeBlockedBuy);
            SetFeedback(string.IsNullOrEmpty(blockedBuyFailMessage) ? "Coin kurang." : blockedBuyFailMessage);
            RefreshAllVisuals();
            return;
        }

        bool success = BallSkinStore.TrySelectUnlocked(skinIndex);
        if (!success)
        {
            SetFeedback("Skin belum dibeli.");
            RefreshAllVisuals();
            return;
        }

        SfxManager.Instance?.PlayEquipSkin();

        BallController runtimeBall = FindObjectOfType<BallController>(true);
        if (runtimeBall != null)
            runtimeBall.ApplySelectedSkin();

        SetFeedback($"Skin {skinIndex} sedang dipakai.");
        RefreshAllVisuals();
    }

    public void OnEquippedPressed(int skinIndex)
    {
        SetFeedback($"Skin {skinIndex} sedang dipakai.");
        RefreshAllVisuals();
    }

    [ContextMenu("Reset Purchased Skins")]
    public void ResetPurchasedSkins()
    {
        BallSkinStore.ResetUnlockedSkins(resetMaxSkinIndex, true);
        SetFeedback("Semua skin purchase di-reset.");
        RefreshAllVisuals();
    }

    public int GetSkinPrice(int skinIndex)
    {
        return BallSkinStore.GetPrice(skinIndex);
    }

    public bool IsSkinUnlocked(int skinIndex)
    {
        return BallSkinStore.IsUnlocked(skinIndex);
    }

    [ContextMenu("Refresh Current Skin Info")]
    public void RefreshCurrentSkinInfo()
    {
        if (!showCurrentSkinInfo)
            return;

        ResolveCurrentSkinInfoRefs();

        int selectedSkin = BallSkinStore.GetSelectedSkinIndex();

        if (usingText != null)
            usingText.text = string.Format(string.IsNullOrEmpty(usingTextFormat) ? "Skin {0}" : usingTextFormat, selectedSkin);

        if (usingSkinImage == null)
            return;

        Sprite preview = GetSkinPreviewSprite(selectedSkin);
        usingSkinImage.sprite = preview;
        usingSkinImage.enabled = preview != null;
        usingSkinImage.preserveAspect = true;
    }

    void SetFeedback(string text)
    {
        if (!string.IsNullOrEmpty(text))
            Debug.Log($"[SHOP] {text}");

        if (feedbackText != null)
            feedbackText.text = text;
    }

    public void ShowFailBuyPanel()
    {
        ResolveFailBuyPanelRefs();

        if (failBuyPanel == null)
            return;

        failBuyPanel.SetActive(true);
        PlayFailBuyPanelPop();
    }

    public void HideFailBuyPanel()
    {
        StopFailBuyPanelPop(true);

        if (failBuyPanel != null)
            failBuyPanel.SetActive(false);
    }

    void PlayFailBuyPanelPop()
    {
        if (!animateFailBuyPanel || failBuyPanel == null)
            return;

        RectTransform target = GetFailBuyPanelRoot();
        if (target == null)
            return;

        CaptureFailBuyOriginalScale(target);

        if (failBuyPopRoutine != null)
            StopCoroutine(failBuyPopRoutine);

        failBuyPopRoutine = StartCoroutine(AnimateFailBuyPanelPop(target));
    }

    IEnumerator AnimateFailBuyPanelPop(RectTransform target)
    {
        CanvasGroup canvasGroup = failBuyPanel != null ? failBuyPanel.GetComponent<CanvasGroup>() : null;
        if (fadeFailBuyPanel && canvasGroup == null && failBuyPanel != null)
            canvasGroup = failBuyPanel.AddComponent<CanvasGroup>();

        float duration = Mathf.Max(0.01f, failBuyPopDuration);
        float upDuration = duration * 0.65f;
        float downDuration = Mathf.Max(0.01f, duration - upDuration);
        Vector3 startScale = failBuyOriginalScale * Mathf.Max(0.01f, failBuyStartScale);
        Vector3 overshootScale = failBuyOriginalScale * Mathf.Max(1f, failBuyOvershootScale);

        target.localScale = startScale;
        if (canvasGroup != null)
            canvasGroup.alpha = fadeFailBuyPanel ? 0f : 1f;

        float elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / upDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.LerpUnclamped(startScale, overshootScale, eased);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / downDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.LerpUnclamped(overshootScale, failBuyOriginalScale, eased);
            yield return null;
        }

        target.localScale = failBuyOriginalScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        failBuyPopRoutine = null;
    }

    void StopFailBuyPanelPop(bool restoreScale)
    {
        if (failBuyPopRoutine != null)
        {
            StopCoroutine(failBuyPopRoutine);
            failBuyPopRoutine = null;
        }

        RectTransform target = GetFailBuyPanelRoot();
        if (restoreScale && target != null && hasFailBuyOriginalScale)
            target.localScale = failBuyOriginalScale;

        CanvasGroup canvasGroup = failBuyPanel != null ? failBuyPanel.GetComponent<CanvasGroup>() : null;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    RectTransform GetFailBuyPanelRoot()
    {
        if (failBuyPanelRoot != null)
            return failBuyPanelRoot;

        return failBuyPanel != null ? failBuyPanel.GetComponent<RectTransform>() : null;
    }

    void CaptureFailBuyOriginalScale(RectTransform target)
    {
        if (hasFailBuyOriginalScale || target == null)
            return;

        failBuyOriginalScale = target.localScale;
        hasFailBuyOriginalScale = true;
    }

    void WireFailBuyPanelButton()
    {
        if (failBuyOkButton == null)
            return;

        failBuyOkButton.onClick.RemoveListener(HideFailBuyPanel);
        failBuyOkButton.onClick.AddListener(HideFailBuyPanel);
    }

    void ResolveFailBuyPanelRefs()
    {
        if (!autoResolveFailBuyPanel)
            return;

        if (failBuyPanel == null)
        {
            Transform found = FindChildByName(transform, "FailBuyPanel");

            if (found == null && transform.parent != null)
                found = FindChildByName(transform.parent, "FailBuyPanel");

            if (found == null && transform.root != null)
                found = FindChildByName(transform.root, "FailBuyPanel");

            if (found != null)
                failBuyPanel = found.gameObject;
        }

        if (failBuyOkButton == null && failBuyPanel != null)
        {
            Transform okTransform = FindChildByName(failBuyPanel.transform, "OkButton");
            if (okTransform == null)
                okTransform = FindChildByName(failBuyPanel.transform, "OKButton");

            if (okTransform != null)
                failBuyOkButton = okTransform.GetComponent<Button>();

            if (failBuyOkButton == null)
                failBuyOkButton = failBuyPanel.GetComponentInChildren<Button>(true);
        }

        if (failBuyPanelRoot == null && failBuyPanel != null)
            failBuyPanelRoot = failBuyPanel.GetComponent<RectTransform>();
    }

    void ResolveCurrentSkinInfoRefs()
    {
        if (!autoResolveCurrentSkinInfo)
            return;

        Transform searchRoot = transform;
        Transform panelRoot = FindChildByName(searchRoot, "UseSkinPanel");
        Transform infoRoot = panelRoot != null ? panelRoot : searchRoot;

        if (usingText == null)
        {
            Transform textTransform = FindChildByName(infoRoot, "UsingText");
            if (textTransform != null)
                usingText = textTransform.GetComponent<TMP_Text>();
        }

        if (usingSkinImage == null)
        {
            Transform imageTransform = FindChildByName(infoRoot, "SkinImage");
            if (imageTransform != null)
                usingSkinImage = imageTransform.GetComponent<Image>();
        }
    }

    public Sprite GetSkinPreviewSprite(int skinIndex)
    {
        if (skinPreviewSprites != null &&
            skinIndex >= 0 &&
            skinIndex < skinPreviewSprites.Length &&
            skinPreviewSprites[skinIndex] != null)
        {
            return skinPreviewSprites[skinIndex];
        }

        if (skinCards != null)
        {
            for (int i = 0; i < skinCards.Length; i++)
            {
                SkinCardBinding card = skinCards[i];
                if (card == null || card.skinIndex != skinIndex || card.skinImage == null)
                    continue;

                if (card.skinImage.sprite != null)
                    return card.skinImage.sprite;
            }
        }

        return null;
    }

    TMP_Text FindPriceTextForCard(Transform cardRoot)
    {
        if (cardRoot == null)
            return null;

        Transform coinIcon = FindChildByName(cardRoot, "CoinIcon");
        if (coinIcon != null)
        {
            TMP_Text coinText = coinIcon.GetComponentInChildren<TMP_Text>(true);
            if (coinText != null)
                return coinText;
        }

        TMP_Text[] allTexts = cardRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null)
                continue;

            if (text.transform.GetComponentInParent<Button>() != null)
                continue;

            return text;
        }

        return null;
    }

    Button FindButtonInCard(Transform cardRoot, string exactName, string containsName)
    {
        if (cardRoot == null)
            return null;

        Transform exact = FindChildByName(cardRoot, exactName);
        if (exact != null)
        {
            Button exactButton = exact.GetComponent<Button>();
            if (exactButton != null)
                return exactButton;
        }

        Button[] allButtons = cardRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null)
                continue;

            string lower = button.name.ToLower();
            string key = containsName.ToLower();
            bool match = lower.Contains(key);
            if (!match && key == "equipbutton" && lower.Contains("equip"))
                match = true;
            if (!match && key == "unequipbutton" && (lower.Contains("unequip") || lower.Contains("equipped")))
                match = true;

            if (match)
            {
                if (key == "equipbutton" && lower.Contains("unequip"))
                    continue;

                return button;
            }
        }

        return null;
    }

    GameObject FindVisualInCard(Transform cardRoot, string exactName, string containsName)
    {
        if (cardRoot == null)
            return null;

        Transform exact = FindChildByName(cardRoot, exactName);
        if (exact != null)
            return exact.gameObject;

        Transform[] all = cardRoot.GetComponentsInChildren<Transform>(true);
        string key = containsName.ToLower();

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;

            string lower = t.name.ToLower();
            if (!lower.Contains(key))
                continue;

            if (key == "equipbutton" && lower.Contains("unequip"))
                continue;

            return t.gameObject;
        }

        return null;
    }

    Image FindImageInCard(Transform cardRoot, string exactName, string containsName)
    {
        if (cardRoot == null)
            return null;

        Transform exact = FindChildByName(cardRoot, exactName);
        if (exact != null)
        {
            Image exactImage = exact.GetComponent<Image>();
            if (exactImage != null)
                return exactImage;
        }

        Image[] allImages = cardRoot.GetComponentsInChildren<Image>(true);
        string key = containsName.ToLower();

        for (int i = 0; i < allImages.Length; i++)
        {
            Image image = allImages[i];
            if (image == null)
                continue;

            string lower = image.name.ToLower();
            if (lower.Contains(key))
                return image;
        }

        return null;
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

    void ApplyPriceTextStyle(TMP_Text text)
    {
        if (text == null || !forceSingleLinePriceText)
            return;

        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    GameObject GetEquipVisual(SkinCardBinding card)
    {
        if (card == null)
            return null;

        if (card.equipVisual != null)
            return card.equipVisual;

        if (card.equipButton != null)
            return card.equipButton.gameObject;

        return null;
    }

    GameObject GetEquippedVisual(SkinCardBinding card)
    {
        if (card == null)
            return null;

        if (card.equippedVisual != null)
            return card.equippedVisual;

        if (card.equippedButton != null)
            return card.equippedButton.gameObject;

        return null;
    }

    void SetButtonLabel(Button button, string value)
    {
        if (button == null || string.IsNullOrEmpty(value))
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }
}
