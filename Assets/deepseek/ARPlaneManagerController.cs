using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARPlaneManagerController : MonoBehaviour
{
    [Header("Plane Detection Settings")]
    public bool showDebugLogs = true;
    public float detectionDelay = 0.5f;

    [Header("Auto Hide")]
    public bool hidePlanesOnDisable = true;

    private ARPlaneManager planeManager;
    private float lastDetectionTime;

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();

        if (planeManager == null)
            Debug.LogError("[ARPlaneController] ARPlaneManager tidak ditemukan!");
    }

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesDetected;
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesDetected;
    }

    // =========================
    private void OnPlanesDetected(ARPlanesChangedEventArgs args)
    {
        if (!showDebugLogs) return;

        if (Time.time - lastDetectionTime < detectionDelay) return;
        lastDetectionTime = Time.time;

        foreach (var plane in args.added)
        {
            if (plane == null) continue;

            Vector2 size = plane.size;
            Debug.Log($"[PLANE] Detected | Size={size.x:F2} x {size.y:F2} | Alignment={plane.alignment}");
        }
    }

    // =========================
    public void DisablePlaneDetection()
    {
        if (planeManager == null) return;

        if (showDebugLogs)
            Debug.Log("[PLANE] Detection DISABLED");

        planeManager.planesChanged -= OnPlanesDetected;
        planeManager.enabled = false;

        if (hidePlanesOnDisable)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane != null)
                    plane.gameObject.SetActive(false);
            }
        }
    }

    // =========================
    public void EnablePlaneDetection()
    {
        if (planeManager == null) return;

        if (showDebugLogs)
            Debug.Log("[PLANE] Detection ENABLED");

        planeManager.enabled = true;
        planeManager.planesChanged -= OnPlanesDetected;
        planeManager.planesChanged += OnPlanesDetected;

        foreach (var plane in planeManager.trackables)
        {
            if (plane != null)
                plane.gameObject.SetActive(true);
        }
    }
}
