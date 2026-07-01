using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSkinDisplayUI : MonoBehaviour
{
    [Header("References")]
    public TMP_Text usingText;
    public Image skinImage;
    public bool autoResolveReferences = true;

    [Header("Auto Resolve Names")]
    public string panelName = "UseSkinPanel";
    public string textName = "UsingText";
    public string imageName = "SkinImage";

    [Header("Skin Preview")]
    [Tooltip("Index array = skin index. Contoh: element 0 untuk skin default, element 1 untuk skin index 1.")]
    public Sprite[] skinPreviewSprites;
    public bool useShopPreviewSpritesAsFallback = true;
    public BallSkinShopUI shopUIFallback;
    public string usingTextFormat = "Using Skin";
    public bool showSkinIndexInText = false;
    public bool hideImageWhenMissing = true;

    void OnEnable()
    {
        Refresh();
    }

    void Start()
    {
        Refresh();
    }

    public static void RefreshAllDisplays()
    {
        CurrentSkinDisplayUI[] displays = FindObjectsOfType<CurrentSkinDisplayUI>(true);
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null)
                displays[i].Refresh();
        }
    }

    [ContextMenu("Refresh Current Skin Display")]
    public void Refresh()
    {
        ResolveReferences();

        int selectedSkinIndex = BallSkinStore.GetSelectedSkinIndex();

        if (usingText != null)
        {
            usingText.text = showSkinIndexInText
                ? string.Format(string.IsNullOrEmpty(usingTextFormat) ? "Skin {0}" : usingTextFormat, selectedSkinIndex)
                : usingTextFormat;
        }

        if (skinImage == null)
            return;

        Sprite preview = GetPreviewSprite(selectedSkinIndex);
        skinImage.sprite = preview;
        skinImage.preserveAspect = true;

        if (hideImageWhenMissing)
            skinImage.enabled = preview != null;
    }

    void ResolveReferences()
    {
        if (!autoResolveReferences)
            return;

        Transform root = transform;
        Transform panel = FindChildByName(root, panelName);
        Transform searchRoot = panel != null ? panel : root;

        if (usingText == null)
        {
            Transform textTransform = FindChildByName(searchRoot, textName);
            if (textTransform != null)
                usingText = textTransform.GetComponent<TMP_Text>();
        }

        if (skinImage == null)
        {
            Transform imageTransform = FindChildByName(searchRoot, imageName);
            if (imageTransform != null)
                skinImage = imageTransform.GetComponent<Image>();
        }
    }

    Sprite GetPreviewSprite(int skinIndex)
    {
        if (skinPreviewSprites != null && skinIndex >= 0 && skinIndex < skinPreviewSprites.Length && skinPreviewSprites[skinIndex] != null)
            return skinPreviewSprites[skinIndex];

        if (!useShopPreviewSpritesAsFallback)
            return null;

        ResolveShopUIFallback();

        if (shopUIFallback == null)
            return null;

        return shopUIFallback.GetSkinPreviewSprite(skinIndex);
    }

    void ResolveShopUIFallback()
    {
        if (shopUIFallback != null)
            return;

        shopUIFallback = FindObjectOfType<BallSkinShopUI>(true);
    }

    Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}
