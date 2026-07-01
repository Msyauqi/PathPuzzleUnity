using System.Collections.Generic;
using UnityEngine;

public class GameplayVisualBrightness : MonoBehaviour
{
    static readonly List<GameplayVisualBrightness> activeInstances = new List<GameplayVisualBrightness>();

    [Header("Apply")]
    public bool applyOnStart = true;
    public bool refreshContinuously = true;
    public float refreshInterval = 0.5f;

    [Header("Auto Find Runtime Renderers")]
    public bool autoFindRenderers = true;
    public Transform autoFindRoot;
    public string[] basicBallNameKeys = new string[] { "Ball" };
    public string[] gridNameKeys = new string[] { "Cell_" };

    [Header("Safety")]
    [Tooltip("Prefab skin bola sudah diatur oleh BallController. Jika ikut diproses di sini, warna/material special skin bisa tertimpa.")]
    public bool ignoreRuntimeSkinPrefabRenderers = true;
    public string[] runtimeSkinNameKeys = new string[] { "_RuntimeSkin" };

    [Header("Manual Renderers")]
    public Renderer[] basicBallRenderers;
    public Renderer[] pathRenderers;
    public Renderer[] gridRenderers;

    [Header("Basic Ball Brightness")]
    public bool brightenBasicBall = true;
    public Color basicBallTint = Color.white;
    [Range(0.1f, 5f)] public float basicBallBrightness = 2f;
    public bool useBasicBallEmission = true;
    [Range(0f, 5f)] public float basicBallEmissionStrength = 1.6f;

    [Header("Path Brightness")]
    public bool brightenPath = true;
    public Color pathTint = Color.white;
    [Range(0.1f, 5f)] public float pathBrightness = 1.25f;
    public bool usePathEmission = true;
    [Range(0f, 5f)] public float pathEmissionStrength = 0.35f;

    [Header("Grid Brightness")]
    public bool brightenGrid = true;
    public Color gridTint = Color.white;
    [Range(0.1f, 5f)] public float gridBrightness = 1.25f;
    public bool useGridEmission = true;
    [Range(0f, 5f)] public float gridEmissionStrength = 0.35f;

    [Header("Unlit Runtime Fix")]
    [Tooltip("Gunakan jika material asset store tetap gelap walaupun brightness/emission dinaikkan.")]
    public bool forceBasicBallUnlitShader = true;
    public bool forcePathUnlitShader = false;
    public bool forceGridUnlitShader = false;

    readonly Dictionary<Material, Color> baseColorByMaterial = new Dictionary<Material, Color>();
    Shader cachedUnlitShader;
    float refreshTimer;

    void OnEnable()
    {
        if (!activeInstances.Contains(this))
            activeInstances.Add(this);
    }

    void OnDisable()
    {
        activeInstances.Remove(this);
    }

    void Start()
    {
        if (applyOnStart)
            ApplyBrightness();
    }

    void Update()
    {
        if (!refreshContinuously)
            return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < Mathf.Max(0.05f, refreshInterval))
            return;

        refreshTimer = 0f;
        ApplyBrightness();
    }

    [ContextMenu("Apply Brightness")]
    public void ApplyBrightness()
    {
        if (brightenBasicBall)
            ApplyGroup(GetRenderers(basicBallRenderers, basicBallNameKeys), basicBallTint, basicBallBrightness, useBasicBallEmission, basicBallEmissionStrength, forceBasicBallUnlitShader);

        if (brightenPath)
            ApplyGroup(GetPathRenderers(), pathTint, pathBrightness, usePathEmission, pathEmissionStrength, forcePathUnlitShader);

        if (brightenGrid)
            ApplyGroup(GetGridRenderers(), gridTint, gridBrightness, useGridEmission, gridEmissionStrength, forceGridUnlitShader);
    }

    public static void RequestRefreshAll()
    {
        for (int i = 0; i < activeInstances.Count; i++)
        {
            GameplayVisualBrightness instance = activeInstances[i];
            if (instance != null && instance.isActiveAndEnabled)
                instance.ApplyBrightness();
        }
    }

    Renderer[] GetRenderers(Renderer[] manualRenderers, string[] nameKeys)
    {
        if (!autoFindRenderers)
            return manualRenderers;

        List<Renderer> renderers = new List<Renderer>();
        AddRenderers(renderers, manualRenderers);

        Renderer[] candidates = autoFindRoot != null
            ? autoFindRoot.GetComponentsInChildren<Renderer>(true)
            : FindObjectsOfType<Renderer>();

        for (int i = 0; i < candidates.Length; i++)
        {
            Renderer candidate = candidates[i];
            if (candidate == null || ContainsRenderer(renderers, candidate))
                continue;

            if (NameMatches(candidate.transform, nameKeys))
                renderers.Add(candidate);
        }

        return renderers.ToArray();
    }

    Renderer[] GetPathRenderers()
    {
        if (!autoFindRenderers)
            return pathRenderers;

        List<Renderer> renderers = new List<Renderer>();
        AddRenderers(renderers, pathRenderers);

        Renderer[] candidates = autoFindRoot != null
            ? autoFindRoot.GetComponentsInChildren<Renderer>(true)
            : FindObjectsOfType<Renderer>();

        for (int i = 0; i < candidates.Length; i++)
        {
            Renderer candidate = candidates[i];
            if (candidate == null || ContainsRenderer(renderers, candidate))
                continue;

            if (candidate.GetComponentInParent<PathTile>() != null)
                renderers.Add(candidate);
        }

        return renderers.ToArray();
    }

    Renderer[] GetGridRenderers()
    {
        if (!autoFindRenderers)
            return gridRenderers;

        List<Renderer> renderers = new List<Renderer>();
        AddRenderers(renderers, gridRenderers);

        Renderer[] candidates = autoFindRoot != null
            ? autoFindRoot.GetComponentsInChildren<Renderer>(true)
            : FindObjectsOfType<Renderer>();

        for (int i = 0; i < candidates.Length; i++)
        {
            Renderer candidate = candidates[i];
            if (candidate == null || ContainsRenderer(renderers, candidate))
                continue;

            if (candidate.GetComponentInParent<PathTile>() != null)
                continue;

            if (ignoreRuntimeSkinPrefabRenderers && NameMatches(candidate.transform, runtimeSkinNameKeys))
                continue;

            if (NameMatches(candidate.transform, gridNameKeys))
                renderers.Add(candidate);
        }

        return renderers.ToArray();
    }

    void AddRenderers(List<Renderer> target, Renderer[] source)
    {
        if (target == null || source == null)
            return;

        for (int i = 0; i < source.Length; i++)
        {
            Renderer renderer = source[i];
            if (renderer != null && !ContainsRenderer(target, renderer))
                target.Add(renderer);
        }
    }

    bool ContainsRenderer(List<Renderer> renderers, Renderer renderer)
    {
        if (renderers == null || renderer == null)
            return false;

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == renderer)
                return true;
        }

        return false;
    }

    bool NameMatches(Transform target, string[] keys)
    {
        if (target == null || keys == null || keys.Length == 0)
            return false;

        Transform current = target;
        while (current != null)
        {
            string lowerName = current.name.ToLower();
            for (int i = 0; i < keys.Length; i++)
            {
                if (!string.IsNullOrEmpty(keys[i]) && lowerName.Contains(keys[i].ToLower()))
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    void ApplyGroup(Renderer[] renderers, Color tint, float brightness, bool useEmission, float emissionStrength, bool forceUnlitShader)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
            ApplyToRenderer(renderers[i], tint, brightness, useEmission, emissionStrength, forceUnlitShader);
    }

    void ApplyToRenderer(Renderer renderer, Color tint, float brightness, bool useEmission, float emissionStrength, bool forceUnlitShader)
    {
        if (renderer == null || renderer is TrailRenderer)
            return;

        if (!IsRuntimeSceneRenderer(renderer))
            return;

        if (ignoreRuntimeSkinPrefabRenderers && NameMatches(renderer.transform, runtimeSkinNameKeys))
            return;

        PathTile pathTile = renderer.GetComponentInParent<PathTile>();
        if (pathTile != null && pathTile.IsVisualOverrideActive)
            return;

        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
                continue;

            Texture mainTexture = GetMainTexture(material);
            Color sourceColor = mainTexture != null ? Color.white : LiftDarkColor(GetStableBaseColor(material));
            Color finalColor = MultiplyColor(sourceColor, tint, brightness);
            Color emissionColor = MultiplyColor(sourceColor, tint, emissionStrength);

            if (forceUnlitShader)
                ForceUnlitMaterial(material, mainTexture, finalColor);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", finalColor);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", finalColor);

            if (useEmission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        if (pathTile != null)
            pathTile.CaptureCurrentColorsAsOriginal();
    }

    bool IsRuntimeSceneRenderer(Renderer renderer)
    {
        if (renderer == null || renderer.gameObject == null)
            return false;

        UnityEngine.SceneManagement.Scene scene = renderer.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    void ForceUnlitMaterial(Material material, Texture mainTexture, Color color)
    {
        if (material == null)
            return;

        Shader unlitShader = GetUnlitShader();
        if (unlitShader == null)
            return;

        if (material.shader != unlitShader)
            material.shader = unlitShader;

        if (mainTexture != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", mainTexture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", mainTexture);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    Shader GetUnlitShader()
    {
        if (cachedUnlitShader != null)
            return cachedUnlitShader;

        cachedUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (cachedUnlitShader == null)
            cachedUnlitShader = Shader.Find("Unlit/Texture");
        if (cachedUnlitShader == null)
            cachedUnlitShader = Shader.Find("Unlit/Color");

        return cachedUnlitShader;
    }

    Color GetStableBaseColor(Material material)
    {
        if (material == null)
            return Color.white;

        if (!baseColorByMaterial.TryGetValue(material, out Color color))
        {
            color = GetMaterialColor(material);
            baseColorByMaterial.Add(material, color);
        }

        return color;
    }

    Color GetMaterialColor(Material material)
    {
        if (material == null)
            return Color.white;

        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");

        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");

        return Color.white;
    }

    Texture GetMainTexture(Material material)
    {
        if (material == null)
            return null;

        if (material.HasProperty("_BaseMap"))
            return material.GetTexture("_BaseMap");

        if (material.HasProperty("_MainTex"))
            return material.GetTexture("_MainTex");

        return null;
    }

    Color LiftDarkColor(Color color)
    {
        float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        const float minimumVisibleChannel = 0.35f;

        if (maxChannel >= minimumVisibleChannel)
            return color;

        if (maxChannel <= 0.0001f)
            return new Color(minimumVisibleChannel, minimumVisibleChannel, minimumVisibleChannel, color.a);

        float scale = minimumVisibleChannel / maxChannel;
        return new Color(
            Mathf.Clamp01(color.r * scale),
            Mathf.Clamp01(color.g * scale),
            Mathf.Clamp01(color.b * scale),
            color.a
        );
    }

    Color MultiplyColor(Color source, Color tint, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(source.r * tint.r * multiplier),
            Mathf.Clamp01(source.g * tint.g * multiplier),
            Mathf.Clamp01(source.b * tint.b * multiplier),
            source.a);
    }
}
