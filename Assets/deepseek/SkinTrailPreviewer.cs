using UnityEngine;

[ExecuteAlways]
public class SkinTrailPreviewer : MonoBehaviour
{
    public enum PreviewMotionMode
    {
        Static,
        PingPong,
        Circle
    }

    [System.Serializable]
    public class PreviewSkinTrailProfile
    {
        public int skinIndex;
        public bool useSpecialGradient;
        public Color basicColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        public Color specialColorA = new Color(1f, 0.2f, 0.2f, 0.95f);
        public Color specialColorB = new Color(1f, 0.85f, 0.1f, 0.85f);
        public Color specialColorC = new Color(1f, 0.45f, 0.05f, 0.25f);
    }

    [System.Serializable]
    public class PreviewSkinTransformProfile
    {
        public int skinIndex;
        public bool overridePosition;
        public Vector3 localPosition = Vector3.zero;
        public bool overrideEuler;
        public Vector3 localEuler = Vector3.zero;
        public bool overrideScale;
        public Vector3 localScale = Vector3.one;
        public bool overrideTargetDiameter;
        [Min(0.01f)] public float targetDiameter = 0.65f;
    }

    [Header("Skin Preview")]
    public GameObject[] skinPrefabs;
    [Min(0)] public int skinIndex;
    public bool rebuildOnValidate = true;

    [Header("Skin Transform")]
    public Vector3 skinLocalPosition = Vector3.zero;
    public Vector3 skinLocalEuler = Vector3.zero;
    public Vector3 skinLocalScale = Vector3.one;
    public bool disableColliders = true;

    [Header("Skin Size Normalize")]
    public bool normalizeSkinSize = true;
    [Min(0.01f)] public float targetBallDiameter = 0.65f;
    public bool autoCenterSkin = true;

    [Header("Preview Motion")]
    public PreviewMotionMode motionMode = PreviewMotionMode.PingPong;
    public bool animateInEditMode = true;
    [Min(0f)] public float moveDistance = 1.2f;
    [Min(0f)] public float moveSpeed = 1.5f;
    public Vector3 moveDirection = Vector3.right;

    [Header("Trail")]
    public bool enableTrail = true;
    public Material trailMaterial;
    public bool forceUnlitTrailMaterial = true;
    [Range(0.05f, 2f)] public float trailTime = 0.28f;
    [Range(0.001f, 1f)] public float trailStartWidth = 0.12f;
    [Range(0f, 1f)] public float trailEndWidth = 0.018f;
    [Range(0.001f, 0.5f)] public float minVertexDistance = 0.004f;
    public LineAlignment trailAlignment = LineAlignment.View;
    [Range(0, 16)] public int cornerVertices = 6;
    [Range(0, 16)] public int capVertices = 6;
    public bool fadeOutAtEnd = true;

    [Header("Trail Placement")]
    public bool parentTrailToMovingBall = true;
    public bool placeTrailBehindBall = true;
    [Min(0f)] public float trailBehindDistance = 0.28f;
    public Vector3 trailLocalOffset = Vector3.zero;
    public float trailVerticalOffset = 0f;

    [Header("Trail Color")]
    public int specialSkinStartIndex = 6;
    public int[] specialSkinIndices;
    public bool combineSpecialStartIndexWithManualList = true;
    public bool usePerSkinProfiles = true;
    public PreviewSkinTrailProfile[] skinTrailProfiles;
    public Color defaultBasicTrailColor = new Color(1f, 0.85f, 0.2f, 0.95f);
    public Color defaultSpecialTrailColorA = new Color(1f, 0.2f, 0.2f, 0.95f);
    public Color defaultSpecialTrailColorB = new Color(1f, 0.85f, 0.1f, 0.85f);
    public Color defaultSpecialTrailColorC = new Color(1f, 0.45f, 0.05f, 0.25f);
    [Range(0.05f, 0.6f)] public float specialMiddleColorTime = 0.25f;
    [Range(0.2f, 0.9f)] public float specialEndColorTime = 0.58f;

    [Header("Lighting Helper")]
    public bool createPreviewLight = true;
    public Color lightColor = Color.white;
    [Min(0f)] public float lightIntensity = 1.6f;

    [Header("Per Skin Transform")]
    public bool usePerSkinTransformProfiles = true;
    public PreviewSkinTransformProfile[] skinTransformProfiles;

    GameObject previewRoot;
    GameObject spawnedSkin;
    TrailRenderer trailRenderer;
    Light previewLight;
    Material runtimeTrailMaterial;
    int spawnedSkinIndex = -1;
    Vector3 lastMoveDirection = Vector3.right;

    void OnEnable()
    {
        RebuildPreview();
    }

    void Start()
    {
        RebuildPreview();
    }

    void OnDisable()
    {
        ClearPreview();
    }

    void OnValidate()
    {
        skinIndex = Mathf.Max(0, skinIndex);
        targetBallDiameter = Mathf.Max(0.01f, targetBallDiameter);
        moveDirection = moveDirection.sqrMagnitude <= 0.0001f ? Vector3.right : moveDirection.normalized;
        trailBehindDistance = Mathf.Max(0f, trailBehindDistance);

        if (rebuildOnValidate && isActiveAndEnabled)
            RebuildPreview();
    }

    void Update()
    {
        if (!Application.isPlaying && !animateInEditMode)
            return;

        AnimatePreviewRoot();
        ConfigureTrail();
    }

    [ContextMenu("Rebuild Preview")]
    public void RebuildPreview()
    {
        EnsurePreviewRoot();
        EnsureLight();
        SpawnSkin();
        EnsureTrail();
        ConfigureTrail();
        ClearTrail();
    }

    [ContextMenu("Next Skin")]
    public void NextSkin()
    {
        int max = skinPrefabs == null ? 0 : Mathf.Max(0, skinPrefabs.Length - 1);
        skinIndex = Mathf.Clamp(skinIndex + 1, 0, max);
        RebuildPreview();
    }

    [ContextMenu("Previous Skin")]
    public void PreviousSkin()
    {
        skinIndex = Mathf.Max(0, skinIndex - 1);
        RebuildPreview();
    }

    [ContextMenu("Clear Trail")]
    public void ClearTrail()
    {
        if (trailRenderer != null)
            trailRenderer.Clear();
    }

    void EnsurePreviewRoot()
    {
        if (previewRoot != null)
            return;

        Transform existing = transform.Find("SkinTrailPreview_Root");
        if (existing != null)
        {
            previewRoot = existing.gameObject;
            return;
        }

        previewRoot = new GameObject("SkinTrailPreview_Root");
        previewRoot.transform.SetParent(transform, false);
        previewRoot.transform.localPosition = Vector3.zero;
        previewRoot.transform.localRotation = Quaternion.identity;
        previewRoot.transform.localScale = Vector3.one;
    }

    void EnsureLight()
    {
        if (!createPreviewLight)
        {
            if (previewLight != null)
                DestroyObject(previewLight.gameObject);
            previewLight = null;
            return;
        }

        if (previewLight == null)
        {
            Transform existing = transform.Find("SkinTrailPreview_Light");
            if (existing != null)
                previewLight = existing.GetComponent<Light>();
        }

        if (previewLight == null)
        {
            GameObject lightObject = new GameObject("SkinTrailPreview_Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.5f, -2.5f);
            lightObject.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
        }

        previewLight.color = lightColor;
        previewLight.intensity = lightIntensity;
    }

    void SpawnSkin()
    {
        if (previewRoot == null)
            return;

        int clampedIndex = GetClampedSkinIndex();
        if (spawnedSkin != null && spawnedSkinIndex == clampedIndex)
        {
            ApplySkinLayout();
            return;
        }

        if (spawnedSkin != null)
            DestroyObject(spawnedSkin);

        spawnedSkin = null;
        spawnedSkinIndex = -1;

        GameObject prefab = GetSkinPrefab(clampedIndex);
        if (prefab == null)
            return;

        spawnedSkin = Instantiate(prefab, previewRoot.transform);
        spawnedSkin.name = $"{prefab.name}_Preview";
        spawnedSkinIndex = clampedIndex;

        if (disableColliders)
        {
            Collider[] colliders = spawnedSkin.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        ApplySkinLayout();
    }

    void ApplySkinLayout()
    {
        if (spawnedSkin == null)
            return;

        Transform skinTransform = spawnedSkin.transform;
        skinTransform.localPosition = Vector3.zero;
        skinTransform.localRotation = Quaternion.identity;
        skinTransform.localScale = Vector3.one;

        PreviewSkinTransformProfile transformProfile = null;
        TryGetTransformProfile(spawnedSkinIndex, out transformProfile);

        if (normalizeSkinSize)
        {
            float diameter = transformProfile != null && transformProfile.overrideTargetDiameter
                ? transformProfile.targetDiameter
                : targetBallDiameter;
            NormalizeSkin(skinTransform, diameter);
        }

        Vector3 finalPosition = skinLocalPosition;
        Vector3 finalEuler = skinLocalEuler;
        Vector3 finalScale = skinLocalScale;

        if (transformProfile != null)
        {
            if (transformProfile.overridePosition)
                finalPosition = transformProfile.localPosition;
            if (transformProfile.overrideEuler)
                finalEuler = transformProfile.localEuler;
            if (transformProfile.overrideScale)
                finalScale = transformProfile.localScale;
        }

        skinTransform.localPosition += finalPosition;
        skinTransform.localRotation = Quaternion.Euler(finalEuler);
        skinTransform.localScale = Vector3.Scale(skinTransform.localScale, finalScale);
    }

    void NormalizeSkin(Transform skinRoot, float targetDiameter)
    {
        if (skinRoot == null || !TryGetCombinedRendererBounds(skinRoot, out Bounds bounds))
            return;

        float sourceDiameter = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (sourceDiameter <= 0.0001f)
            return;

        float ratio = Mathf.Max(0.01f, targetDiameter) / sourceDiameter;
        skinRoot.localScale = Vector3.one * ratio;

        if (!autoCenterSkin)
            return;

        if (!TryGetCombinedRendererBounds(skinRoot, out Bounds centeredBounds))
            return;

        Transform parent = skinRoot.parent;
        Vector3 centerInParentSpace = parent != null
            ? parent.InverseTransformPoint(centeredBounds.center)
            : centeredBounds.center;

        skinRoot.localPosition -= centerInParentSpace;
    }

    void EnsureTrail()
    {
        if (previewRoot == null)
            return;

        if (!enableTrail)
        {
            if (trailRenderer != null)
                DestroyObject(trailRenderer.gameObject);
            trailRenderer = null;
            return;
        }

        if (trailRenderer == null)
        {
            Transform existing = previewRoot.transform.Find("SkinTrailPreview_Trail");
            if (existing != null)
                trailRenderer = existing.GetComponent<TrailRenderer>();
        }

        if (trailRenderer == null)
        {
            GameObject trailObject = new GameObject("SkinTrailPreview_Trail");
            trailObject.transform.SetParent(parentTrailToMovingBall ? previewRoot.transform : transform, false);
            trailObject.transform.localPosition = Vector3.zero;
            trailRenderer = trailObject.AddComponent<TrailRenderer>();
        }

        Transform desiredParent = parentTrailToMovingBall ? previewRoot.transform : transform;
        if (trailRenderer != null && trailRenderer.transform.parent != desiredParent)
            trailRenderer.transform.SetParent(desiredParent, true);
    }

    void ConfigureTrail()
    {
        if (trailRenderer == null)
            return;

        trailRenderer.emitting = enableTrail && motionMode != PreviewMotionMode.Static;
        trailRenderer.time = Mathf.Max(0.01f, trailTime);
        trailRenderer.startWidth = Mathf.Max(0.001f, trailStartWidth);
        trailRenderer.endWidth = Mathf.Max(0f, trailEndWidth);
        trailRenderer.minVertexDistance = Mathf.Max(0.001f, minVertexDistance);
        trailRenderer.alignment = trailAlignment;
        trailRenderer.numCornerVertices = Mathf.Clamp(cornerVertices, 0, 16);
        trailRenderer.numCapVertices = Mathf.Clamp(capVertices, 0, 16);
        trailRenderer.material = ResolveTrailMaterial();
        ApplyTrailPlacement();

        ResolveTrailColors(GetClampedSkinIndex(), out bool useGradient, out Color c1, out Color c2, out Color c3);
        trailRenderer.colorGradient = useGradient
            ? BuildThreeColorGradient(c1, c2, c3)
            : BuildSingleColorGradient(c1);

        if (runtimeTrailMaterial != null)
        {
            Color materialTint = useGradient ? Color.white : c1;
            if (runtimeTrailMaterial.HasProperty("_Color"))
                runtimeTrailMaterial.SetColor("_Color", materialTint);
            if (runtimeTrailMaterial.HasProperty("_BaseColor"))
                runtimeTrailMaterial.SetColor("_BaseColor", materialTint);
        }
    }

    void AnimatePreviewRoot()
    {
        if (previewRoot == null)
            return;

        if (motionMode == PreviewMotionMode.Static || moveDistance <= 0f || moveSpeed <= 0f)
        {
            previewRoot.transform.localPosition = Vector3.zero;
            return;
        }

        float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

        if (motionMode == PreviewMotionMode.Circle)
        {
            float angle = t * moveSpeed;
            previewRoot.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * moveDistance;
            lastMoveDirection = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
            return;
        }

        float offset = Mathf.Sin(t * moveSpeed) * moveDistance;
        previewRoot.transform.localPosition = moveDirection.normalized * offset;
        float directionSign = Mathf.Cos(t * moveSpeed) >= 0f ? 1f : -1f;
        lastMoveDirection = moveDirection.normalized * directionSign;
    }

    void ApplyTrailPlacement()
    {
        if (trailRenderer == null)
            return;

        Vector3 offset = trailLocalOffset + Vector3.up * trailVerticalOffset;
        if (placeTrailBehindBall && lastMoveDirection.sqrMagnitude > 0.0001f)
            offset -= lastMoveDirection.normalized * trailBehindDistance;

        trailRenderer.transform.localPosition = offset;
        trailRenderer.transform.localRotation = Quaternion.identity;
        trailRenderer.transform.localScale = Vector3.one;
    }

    void ResolveTrailColors(int index, out bool useGradient, out Color c1, out Color c2, out Color c3)
    {
        useGradient = IsSpecialSkinIndex(index);
        c1 = defaultBasicTrailColor;
        c2 = defaultBasicTrailColor;
        c3 = defaultBasicTrailColor;

        if (TryGetProfile(index, out PreviewSkinTrailProfile profile))
        {
            useGradient = profile.useSpecialGradient;
            if (useGradient)
            {
                c1 = profile.specialColorA;
                c2 = profile.specialColorB;
                c3 = profile.specialColorC;
            }
            else
            {
                c1 = profile.basicColor;
                c2 = profile.basicColor;
                c3 = profile.basicColor;
            }

            return;
        }

        if (useGradient)
        {
            c1 = defaultSpecialTrailColorA;
            c2 = defaultSpecialTrailColorB;
            c3 = defaultSpecialTrailColorC;
        }
    }

    bool TryGetProfile(int index, out PreviewSkinTrailProfile profile)
    {
        profile = null;
        if (!usePerSkinProfiles || skinTrailProfiles == null)
            return false;

        for (int i = 0; i < skinTrailProfiles.Length; i++)
        {
            PreviewSkinTrailProfile item = skinTrailProfiles[i];
            if (item != null && item.skinIndex == index)
            {
                profile = item;
                return true;
            }
        }

        return false;
    }

    bool TryGetTransformProfile(int index, out PreviewSkinTransformProfile profile)
    {
        profile = null;
        if (!usePerSkinTransformProfiles || skinTransformProfiles == null)
            return false;

        for (int i = 0; i < skinTransformProfiles.Length; i++)
        {
            PreviewSkinTransformProfile item = skinTransformProfiles[i];
            if (item != null && item.skinIndex == index)
            {
                profile = item;
                return true;
            }
        }

        return false;
    }

    bool IsSpecialSkinIndex(int index)
    {
        if (specialSkinIndices != null && specialSkinIndices.Length > 0)
        {
            for (int i = 0; i < specialSkinIndices.Length; i++)
            {
                if (specialSkinIndices[i] == index)
                    return true;
            }

            if (!combineSpecialStartIndexWithManualList)
                return false;
        }

        return index >= Mathf.Max(0, specialSkinStartIndex);
    }

    Gradient BuildSingleColorGradient(Color color)
    {
        Gradient gradient = new Gradient();
        float endAlpha = fadeOutAtEnd ? 0f : color.a;
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(endAlpha, 1f)
            }
        );
        return gradient;
    }

    Gradient BuildThreeColorGradient(Color colorA, Color colorB, Color colorC)
    {
        Gradient gradient = new Gradient();
        float middleTime = Mathf.Clamp(specialMiddleColorTime, 0.05f, 0.6f);
        float endTime = Mathf.Clamp(specialEndColorTime, middleTime + 0.05f, 0.9f);
        float endAlpha = fadeOutAtEnd ? 0f : colorC.a;
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorA, 0f),
                new GradientColorKey(colorB, middleTime),
                new GradientColorKey(colorC, endTime),
                new GradientColorKey(colorC, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(colorA.a, 0f),
                new GradientAlphaKey(colorB.a, middleTime),
                new GradientAlphaKey(colorC.a, endTime),
                new GradientAlphaKey(endAlpha, 1f)
            }
        );
        return gradient;
    }

    Material ResolveTrailMaterial()
    {
        if (!forceUnlitTrailMaterial && trailMaterial != null)
        {
            if (runtimeTrailMaterial == null || runtimeTrailMaterial.shader != trailMaterial.shader)
            {
                runtimeTrailMaterial = new Material(trailMaterial);
                ConfigureMaterial(runtimeTrailMaterial);
            }
            return runtimeTrailMaterial;
        }

        if (runtimeTrailMaterial != null)
            return runtimeTrailMaterial;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return trailMaterial;

        runtimeTrailMaterial = new Material(shader);
        ConfigureMaterial(runtimeTrailMaterial);
        return runtimeTrailMaterial;
    }

    void ConfigureMaterial(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        material.renderQueue = 3000;
    }

    int GetClampedSkinIndex()
    {
        if (skinPrefabs == null || skinPrefabs.Length == 0)
            return 0;

        return Mathf.Clamp(skinIndex, 0, skinPrefabs.Length - 1);
    }

    GameObject GetSkinPrefab(int index)
    {
        if (skinPrefabs == null || skinPrefabs.Length == 0)
            return null;

        int clamped = Mathf.Clamp(index, 0, skinPrefabs.Length - 1);
        return skinPrefabs[clamped];
    }

    bool TryGetCombinedRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    void ClearPreview()
    {
        if (spawnedSkin != null)
            DestroyObject(spawnedSkin);
        spawnedSkin = null;
        spawnedSkinIndex = -1;

        if (previewRoot != null)
            DestroyObject(previewRoot);
        previewRoot = null;

        trailRenderer = null;

        if (previewLight != null)
            DestroyObject(previewLight.gameObject);
        previewLight = null;

        if (runtimeTrailMaterial != null)
            DestroyObject(runtimeTrailMaterial);
        runtimeTrailMaterial = null;
    }

    void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
