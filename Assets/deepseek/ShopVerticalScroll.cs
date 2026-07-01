using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class ShopVerticalScroll : MonoBehaviour
{
    [Header("References")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;
    public bool disableOtherScrollRectsOnSetup = true;

    [Tooltip("Optional. Kalau diisi, viewport akan mengikuti rect object ini.")]
    public RectTransform referenceArea;

    [Header("Viewport Insets (dipakai jika Reference Area kosong)")]
    public float leftInset = 28f;
    public float rightInset = 28f;
    public float topInset = 320f;
    public float bottomInset = 190f;

    [Header("Content")]
    public bool autoFitContentHeight = true;
    public float minContentHeight = 1200f;
    public float extraTopPadding = 24f;
    public float extraBottomPadding = 24f;
    public bool enforceMinimumBottomPadding = true;
    public float minimumBottomPadding = 320f;
    public bool ensureScrollableHeight = true;
    public float ensuredScrollableExtraHeight = 260f;
    public bool autoAlignContentToTop = true;
    public float topAlignPadding = 0f;
    public bool normalizeContentForVerticalScroll = false;
    public bool refreshContinuously = false;

    [Header("Scroll Tuning")]
    public bool tuneScrollFeel = true;
    public float tunedScrollSensitivity = 36f;
    public float tunedDecelerationRate = 0.08f;
    public bool forceSnapToTopOnEnable = true;
    public int topSnapFrames = 2;

    [Header("Start Position")]
    public bool alignStartToReference = true;
    public RectTransform startTopReference;
    public float startTopReferenceOffset = 0f;

    [Header("Compatibility")]
    public bool disableHomePagerOnThisObject = true;

    [Header("Optional: Move Objects Into Scroll Content")]
    public bool moveListedObjectsToContent = false;
    public RectTransform[] objectsThatShouldScroll;

    Coroutine topSnapRoutine;
    bool contentHeightDirty = true;
    Vector2 lastViewportSize = new Vector2(-1f, -1f);
    int lastContentChildCount = -1;

    [ContextMenu("Setup Shop Scroll")]
    public void EnsureSetup()
    {
        MarkContentHeightDirty();
        AutoResolveReferences();
        EnsureScrollRect();
        EnsureViewport();
        FixCommonMisAssignments();
        NormalizeViewportToFillScrollRoot();
        EnsureContent();
        NormalizeContentForVerticalLayout();
        MoveListedObjectsIntoContent();
        ApplyScrollRootRect();
        ConfigureScrollRect();

        if (autoFitContentHeight)
            RefreshContentHeight();

        if (forceSnapToTopOnEnable)
            SnapToTopImmediate();

        if (alignStartToReference)
            AlignContentTopToReference();

        DisableHomePagerIfNeeded();
    }

    [ContextMenu("Refresh Content Height")]
    public void RefreshContentHeight()
    {
        if (content == null)
            return;

        float effectiveBottomPadding = enforceMinimumBottomPadding
            ? Mathf.Max(extraBottomPadding, minimumBottomPadding)
            : extraBottomPadding;

        if (autoAlignContentToTop)
            AlignChildrenToTop(topAlignPadding);

        float preferredHeight = CalculateChildrenBottomExtent(content);
        float targetHeight = Mathf.Max(minContentHeight, preferredHeight + extraTopPadding + effectiveBottomPadding);
        if (ensureScrollableHeight && viewport != null)
            targetHeight = Mathf.Max(targetHeight, viewport.rect.height + Mathf.Max(0f, ensuredScrollableExtraHeight));

        Vector2 size = content.sizeDelta;
        if (!Mathf.Approximately(size.y, targetHeight))
        {
            size.y = targetHeight;
            content.sizeDelta = size;
        }

        CacheLayoutSignature();
        contentHeightDirty = false;
    }

    void Reset()
    {
        EnsureSetup();
    }

    void Awake()
    {
        EnsureSetup();
    }

    void OnEnable()
    {
        EnsureSetup();

        if (forceSnapToTopOnEnable)
        {
            if (topSnapRoutine != null)
                StopCoroutine(topSnapRoutine);

            topSnapRoutine = StartCoroutine(SnapTopAfterFrames());
        }
    }

    void OnDisable()
    {
        if (topSnapRoutine != null)
        {
            StopCoroutine(topSnapRoutine);
            topSnapRoutine = null;
        }
    }

    void OnTransformChildrenChanged()
    {
        MarkContentHeightDirty();
    }

    void LateUpdate()
    {
        if (!autoFitContentHeight)
            return;

        bool layoutChanged = HasLayoutSignatureChanged();

        // Previously this recalculated content bounds every frame while dragging.
        // That made shop scrolling feel heavy when many skin cards were present.
        if (contentHeightDirty || layoutChanged)
            RefreshContentHeight();
    }

    [ContextMenu("Mark Content Height Dirty")]
    public void MarkContentHeightDirty()
    {
        contentHeightDirty = true;
    }

    IEnumerator SnapTopAfterFrames()
    {
        int frameCount = Mathf.Max(1, topSnapFrames);
        for (int i = 0; i < frameCount; i++)
            yield return null;

        Canvas.ForceUpdateCanvases();

        if (autoFitContentHeight)
            RefreshContentHeight();

        SnapToTopImmediate();

        if (alignStartToReference)
            AlignContentTopToReference();

        topSnapRoutine = null;
    }

    void AutoResolveReferences()
    {
        ScrollRect preferred = FindPreferredScrollRect();
        if (preferred != null)
        {
            if (scrollRect == null || scrollRect == GetComponent<ScrollRect>() || scrollRect.gameObject == gameObject)
                scrollRect = preferred;
        }
        else if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (viewport == null && scrollRect != null)
            viewport = scrollRect.viewport;

        if (viewport == null)
        {
            Transform defaultViewport = FindChildByName(transform, "Viewport");
            if (defaultViewport != null)
                viewport = defaultViewport as RectTransform;
        }

        if (content == null && scrollRect != null)
            content = scrollRect.content;

        if (content == null && viewport != null)
        {
            Transform defaultContent = FindChildByName(viewport, "Content");
            if (defaultContent != null)
                content = defaultContent as RectTransform;
        }

        if (content == null)
        {
            Transform existingContent = FindChildByName(transform, "BasicSkinContainer");
            if (existingContent != null)
                content = existingContent as RectTransform;
        }
    }

    ScrollRect FindPreferredScrollRect()
    {
        Transform namedShopViewport = FindChildByName(transform, "ShopViewport");
        if (namedShopViewport != null)
        {
            ScrollRect namedScrollRect = namedShopViewport.GetComponent<ScrollRect>();
            if (namedScrollRect != null)
                return namedScrollRect;
        }

        ScrollRect[] nested = GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < nested.Length; i++)
        {
            ScrollRect candidate = nested[i];
            if (candidate == null || candidate.gameObject == gameObject)
                continue;

            return candidate;
        }

        return GetComponent<ScrollRect>();
    }

    void EnsureScrollRect()
    {
        if (scrollRect == null)
            scrollRect = FindPreferredScrollRect();

        if (scrollRect == null)
            scrollRect = gameObject.AddComponent<ScrollRect>();

        if (!disableOtherScrollRectsOnSetup)
            return;

        ScrollRect[] allScrollRects = GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < allScrollRects.Length; i++)
        {
            ScrollRect other = allScrollRects[i];
            if (other == null || other == scrollRect)
                continue;

            other.enabled = false;
        }
    }

    void EnsureViewport()
    {
        if (viewport == null)
        {
            Transform existing = FindChildByName(transform, "ShopViewport");
            if (existing != null)
                viewport = existing as RectTransform;
        }

        if (viewport == null)
        {
            Transform existing = FindChildByName(transform, "Viewport");
            if (existing != null)
                viewport = existing as RectTransform;
        }

        if (viewport == null)
        {
            GameObject go = new GameObject("ShopViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport = go.GetComponent<RectTransform>();
            viewport.SetParent(transform, false);
        }

        Image image = viewport.GetComponent<Image>();
        if (image == null)
            image = viewport.gameObject.AddComponent<Image>();

        // Butuh raycast target supaya drag scroll responsif.
        // Jangan paksa alpha=0 karena bisa membuat mask tidak stabil di runtime
        // pada beberapa setup CanvasRenderer.
        if (image.color.a <= 0f)
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0.02f);
        image.raycastTarget = true;

        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null)
            mask = viewport.gameObject.AddComponent<Mask>();

        mask.showMaskGraphic = false;

        CanvasRenderer renderer = viewport.GetComponent<CanvasRenderer>();
        if (renderer != null)
            renderer.cullTransparentMesh = false;
    }

    void EnsureContent()
    {
        if (content == null)
        {
            GameObject go = new GameObject("ShopContent", typeof(RectTransform));
            content = go.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
        }
        else if (content.parent != viewport)
        {
            // Keep world position so existing manual layout does not jump.
            content.SetParent(viewport, true);
        }
    }

    void ApplyScrollRootRect()
    {
        RectTransform scrollRoot = GetScrollRootRect();
        if (scrollRoot == null)
            return;

        if (referenceArea != null)
        {
            scrollRoot.anchorMin = referenceArea.anchorMin;
            scrollRoot.anchorMax = referenceArea.anchorMax;
            scrollRoot.pivot = referenceArea.pivot;
            scrollRoot.anchoredPosition = referenceArea.anchoredPosition;
            scrollRoot.sizeDelta = referenceArea.sizeDelta;
            scrollRoot.localScale = Vector3.one;
            scrollRoot.localRotation = Quaternion.identity;
            return;
        }

        scrollRoot.anchorMin = Vector2.zero;
        scrollRoot.anchorMax = Vector2.one;
        scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        scrollRoot.offsetMin = new Vector2(leftInset, bottomInset);
        scrollRoot.offsetMax = new Vector2(-rightInset, -topInset);
        scrollRoot.localScale = Vector3.one;
        scrollRoot.localRotation = Quaternion.identity;
    }

    void ConfigureScrollRect()
    {
        if (scrollRect == null)
            return;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        if (tuneScrollFeel)
        {
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = Mathf.Max(1f, tunedScrollSensitivity);
            scrollRect.decelerationRate = Mathf.Clamp01(tunedDecelerationRate);
        }
    }

    bool HasLayoutSignatureChanged()
    {
        if (content == null)
            return false;

        Vector2 viewportSize = viewport != null ? viewport.rect.size : Vector2.zero;
        int childCount = content.childCount;

        return !Approximately(viewportSize, lastViewportSize) || childCount != lastContentChildCount;
    }

    void CacheLayoutSignature()
    {
        lastViewportSize = viewport != null ? viewport.rect.size : Vector2.zero;
        lastContentChildCount = content != null ? content.childCount : -1;
    }

    static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    void AlignContentTopToReference()
    {
        if (!alignStartToReference || startTopReference == null || viewport == null || content == null)
            return;

        Canvas.ForceUpdateCanvases();

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, content);
        float currentTopY = bounds.max.y;

        Vector3 world = startTopReference.TransformPoint(startTopReference.rect.center);
        Vector3 local = viewport.InverseTransformPoint(world);
        float targetTopY = local.y + startTopReferenceOffset;

        float delta = targetTopY - currentTopY;
        if (Mathf.Abs(delta) < 0.01f)
            return;

        Vector2 anchored = content.anchoredPosition;
        anchored.y += delta;
        content.anchoredPosition = anchored;
    }

    void SnapToTopImmediate()
    {
        if (scrollRect == null)
            return;

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void MoveListedObjectsIntoContent()
    {
        if (!moveListedObjectsToContent || content == null || objectsThatShouldScroll == null)
            return;

        for (int i = 0; i < objectsThatShouldScroll.Length; i++)
        {
            RectTransform target = objectsThatShouldScroll[i];
            if (target == null || target == content)
                continue;

            if (target.IsChildOf(content))
                continue;

            target.SetParent(content, true);
        }
    }

    void FixCommonMisAssignments()
    {
        if (scrollRect == null)
            return;

        RectTransform scrollRoot = scrollRect.transform as RectTransform;

        // If user accidentally assigns ShopViewport as Viewport,
        // try to switch to child "Viewport".
        if (viewport == null || viewport == scrollRoot)
        {
            Transform childViewport = FindChildByName(scrollRoot, "Viewport");
            if (childViewport is RectTransform childViewportRt)
                viewport = childViewportRt;
        }

        // If user accidentally assigns BasicsSection as Content,
        // promote to parent named "Content".
        if (content != null && content.parent is RectTransform parentRt && parentRt.name == "Content")
            content = parentRt;

        if (content == null && viewport != null)
        {
            Transform directContent = viewport.Find("Content");
            if (directContent is RectTransform directContentRt)
                content = directContentRt;
        }

        if (startTopReference == null)
        {
            Transform topRef = FindChildByName(transform, "GarisAtas");
            if (topRef is RectTransform topRefRt)
                startTopReference = topRefRt;
        }
    }

    void NormalizeViewportToFillScrollRoot()
    {
        if (scrollRect == null || viewport == null)
            return;

        RectTransform scrollRoot = scrollRect.transform as RectTransform;
        if (scrollRoot == null)
            return;

        // If viewport is a child object, keep it filling the scroll root.
        if (viewport != scrollRoot)
        {
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.anchoredPosition = Vector2.zero;
            viewport.sizeDelta = Vector2.zero;
            viewport.localScale = Vector3.one;
            viewport.localRotation = Quaternion.identity;
        }
    }

    void NormalizeContentForVerticalLayout()
    {
        if (content == null)
            return;

        if (!normalizeContentForVerticalScroll)
            return;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);

        Vector2 anchored = content.anchoredPosition;
        anchored.x = 0f;
        content.anchoredPosition = anchored;

        Vector2 size = content.sizeDelta;
        size.x = 0f;
        content.sizeDelta = size;
    }

    RectTransform GetScrollRootRect()
    {
        if (scrollRect != null)
            return scrollRect.transform as RectTransform;

        return transform as RectTransform;
    }

    void DisableHomePagerIfNeeded()
    {
        if (!disableHomePagerOnThisObject)
            return;

        HomeMenuPager pager = GetComponent<HomeMenuPager>();
        if (pager != null && pager.enabled)
            pager.enabled = false;
    }

    static float CalculateContentPreferredHeight(RectTransform root)
    {
        if (root == null)
            return 0f;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, root);
        return Mathf.Max(root.rect.height, bounds.size.y);
    }

    static float CalculateChildrenBottomExtent(RectTransform root)
    {
        if (root == null)
            return 0f;

        Bounds bounds;
        if (!TryCalculateChildrenBounds(root, out bounds))
            return Mathf.Max(0f, root.rect.height);

        return Mathf.Max(root.rect.height, -bounds.min.y);
    }

    void AlignChildrenToTop(float padding)
    {
        if (content == null)
            return;

        Bounds bounds;
        if (!TryCalculateChildrenBounds(content, out bounds))
            return;

        float desiredTop = -Mathf.Max(0f, padding);
        if (bounds.max.y >= desiredTop - 0.5f)
            return;

        float delta = desiredTop - bounds.max.y;
        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy)
                continue;

            Vector2 anchored = child.anchoredPosition;
            anchored.y += delta;
            child.anchoredPosition = anchored;
        }
    }

    static bool TryCalculateChildrenBounds(RectTransform root, out Bounds bounds)
    {
        bounds = new Bounds();
        if (root == null)
            return false;

        bool hasBounds = false;
        RectTransform[] all = root.GetComponentsInChildren<RectTransform>(true);
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < all.Length; i++)
        {
            RectTransform rect = all[i];
            if (rect == null || rect == root || !rect.gameObject.activeInHierarchy)
                continue;

            rect.GetWorldCorners(corners);
            for (int c = 0; c < 4; c++)
            {
                Vector3 local = root.InverseTransformPoint(corners[c]);
                if (!hasBounds)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return hasBounds;
    }

    static Transform FindChildByName(Transform root, string targetName)
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
}
