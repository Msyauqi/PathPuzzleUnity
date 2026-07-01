using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARPlaneManager))]
public class ARPlaneVisualizer : MonoBehaviour
{
    [Header("Plane Colors")]
    public Color usablePlaneColor = new Color(0.12f, 0.48f, 1f, 0.50f);
    public Color blockedPlaneColor = new Color(0.55f, 0.55f, 0.55f, 0.32f);

    [Header("Arena Constraint")]
    public float minSurfaceWidth = 0.30f;
    public float minSurfaceHeight = 0.35f;
    public float maxArenaWidth = 0.80f;
    public float maxArenaHeight = 1.00f;

    [Header("Fallback Without Classification")]
    public bool useHeightFallback = true;
    public float floorHeightDifferenceThreshold = 0.55f;

    [Header("Visual")]
    public Material planeMaterial;
    public float visualYOffset = 0.01f;
    public float pulseSpeed = 1.4f;
    public float pulseIntensity = 0.12f;
    public bool showDebugLogs = true;

    private ARPlaneManager planeManager;
    private readonly Dictionary<ARPlane, GameObject> planeVisuals = new Dictionary<ARPlane, GameObject>();

    private ARPlane currentCandidate;
    private bool currentCandidateUsable;

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();

        if (planeManager == null)
            Debug.LogError("[ARPlaneVisualizer] ARPlaneManager tidak ditemukan!");

        SetupMaterial();
    }

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;

        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null && !planeVisuals.ContainsKey(plane))
                    CreatePlaneVisual(plane);
            }
        }
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void SetupMaterial()
    {
        if (planeMaterial != null) return;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        planeMaterial = new Material(shader);

        if (planeMaterial.HasProperty("_Mode"))
            planeMaterial.SetFloat("_Mode", 3f);

        if (planeMaterial.HasProperty("_Surface"))
            planeMaterial.SetFloat("_Surface", 1f);

        if (planeMaterial.HasProperty("_SrcBlend"))
            planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (planeMaterial.HasProperty("_DstBlend"))
            planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (planeMaterial.HasProperty("_ZWrite"))
            planeMaterial.SetInt("_ZWrite", 0);

        if (planeMaterial.HasProperty("_Cull"))
            planeMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        planeMaterial.renderQueue = 3000;
        planeMaterial.color = blockedPlaneColor;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
            CreatePlaneVisual(plane);

        foreach (var plane in args.updated)
            UpdatePlaneVisual(plane);

        foreach (var plane in args.removed)
            RemovePlaneVisual(plane);
    }

    void CreatePlaneVisual(ARPlane plane)
    {
        if (plane == null || planeVisuals.ContainsKey(plane))
            return;

        GameObject visualObj = new GameObject($"Plane_{plane.trackableId}");
        visualObj.transform.SetParent(plane.transform, false);
        visualObj.transform.localPosition = new Vector3(0f, visualYOffset, 0f);
        visualObj.transform.localRotation = Quaternion.identity;

        MeshFilter mf = visualObj.AddComponent<MeshFilter>();
        MeshRenderer mr = visualObj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = $"PlaneMesh_{plane.trackableId}";
        mf.mesh = mesh;

        UpdatePlaneMesh(plane, mesh);

        mr.material = new Material(planeMaterial);

        PlaneVisualController controller = visualObj.AddComponent<PlaneVisualController>();
        controller.Initialize(this, mr.material);

        planeVisuals[plane] = visualObj;
        UpdatePlaneColor(plane, mr.material);

        if (showDebugLogs)
            Debug.Log($"[PLANE VIS] Created | class={plane.classification} | size={plane.size.x:F2} x {plane.size.y:F2}");
    }

    void UpdatePlaneVisual(ARPlane plane)
    {
        if (plane == null) return;

        if (!planeVisuals.TryGetValue(plane, out GameObject obj))
        {
            CreatePlaneVisual(plane);
            return;
        }

        obj.transform.localPosition = new Vector3(0f, visualYOffset, 0f);

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
            UpdatePlaneMesh(plane, mf.mesh);

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
            UpdatePlaneColor(plane, mr.material);
    }

    void UpdatePlaneMesh(ARPlane plane, Mesh mesh)
    {
        Vector2 size = plane.size;
        float w = size.x * 0.5f;
        float h = size.y * 0.5f;

        mesh.Clear();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-w, 0, -h),
            new Vector3(w, 0, -h),
            new Vector3(w, 0, h),
            new Vector3(-w, 0, h)
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        // Dibalik supaya normal menghadap ke atas
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void RemovePlaneVisual(ARPlane plane)
    {
        if (!planeVisuals.TryGetValue(plane, out GameObject obj))
            return;

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null && mf.mesh != null)
            Destroy(mf.mesh);

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null && mr.material != null)
            Destroy(mr.material);

        Destroy(obj);
        planeVisuals.Remove(plane);
    }

    public void SetCandidatePlane(ARPlane plane, bool usable)
    {
        currentCandidate = plane;
        currentCandidateUsable = usable;

        foreach (var kvp in planeVisuals)
        {
            if (kvp.Key == null || kvp.Value == null) continue;

            MeshRenderer mr = kvp.Value.GetComponent<MeshRenderer>();
            if (mr != null)
                UpdatePlaneColor(kvp.Key, mr.material);
        }
    }

    public bool IsClassificationAvailable(ARPlane plane)
    {
        return plane != null && plane.classification != PlaneClassification.None;
    }

    public bool IsLikelyFloorPlane(ARPlane plane)
    {
        if (plane == null) return false;
        if (plane.alignment != PlaneAlignment.HorizontalUp) return false;

        if (plane.classification == PlaneClassification.Floor)
            return true;

        if (plane.classification == PlaneClassification.Table)
            return false;

        if (!useHeightFallback || Camera.main == null)
            return false;

        float heightDifference = Camera.main.transform.position.y - plane.transform.position.y;
        return heightDifference > floorHeightDifferenceThreshold;
    }

    public bool IsUsablePlane(ARPlane plane)
    {
        if (plane == null) return false;
        if (plane.alignment != PlaneAlignment.HorizontalUp) return false;
        if (IsLikelyFloorPlane(plane)) return false;

        Vector2 size = plane.size;
        return size.x >= minSurfaceWidth && size.y >= minSurfaceHeight;
    }

    public Vector2 GetClampedArenaSize(ARPlane plane)
    {
        Vector2 size = plane.size;

        return new Vector2(
            Mathf.Clamp(size.x, minSurfaceWidth, maxArenaWidth),
            Mathf.Clamp(size.y, minSurfaceHeight, maxArenaHeight)
        );
    }

    public void UpdatePlaneColor(ARPlane plane, Material mat)
    {
        if (plane == null || mat == null) return;

        bool isCandidate = plane == currentCandidate;
        bool isUsable = isCandidate
            ? currentCandidateUsable
            : (!IsLikelyFloorPlane(plane) && IsUsablePlane(plane));

        Color targetColor = isUsable ? usablePlaneColor : blockedPlaneColor;
        mat.color = targetColor;

        if (planeVisuals.TryGetValue(plane, out GameObject obj))
        {
            PlaneVisualController ctrl = obj.GetComponent<PlaneVisualController>();
            if (ctrl != null)
                ctrl.SetPulse(isUsable, targetColor);
        }
    }

    public void ClearAllPlanes()
    {
        foreach (var obj in planeVisuals.Values)
        {
            if (obj != null)
                Destroy(obj);
        }

        planeVisuals.Clear();
    }
}

public class PlaneVisualController : MonoBehaviour
{
    private ARPlaneVisualizer visualizer;
    private Material material;
    private bool pulseEnabled;
    private Color baseColor;
    private float pulse;

    public void Initialize(ARPlaneVisualizer visualizer, Material material)
    {
        this.visualizer = visualizer;
        this.material = material;
    }

    public void SetPulse(bool enabled, Color color)
    {
        pulseEnabled = enabled;
        baseColor = color;
    }

    void Update()
    {
        if (!pulseEnabled || material == null) return;

        pulse += Time.deltaTime * visualizer.pulseSpeed;
        float value = Mathf.Sin(pulse) * visualizer.pulseIntensity;

        material.color = new Color(
            Mathf.Clamp01(baseColor.r + value),
            Mathf.Clamp01(baseColor.g + value),
            Mathf.Clamp01(baseColor.b + value),
            baseColor.a
        );
    }
}
