using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class ARPlacementManager : MonoBehaviour
{
    public static event System.Action ArenaPlaced;

    [Header("References")]
    public ARRaycastManager raycastManager;
    public GridManager gridManager;
    public ARPlaneVisualizer planeVisualizer;
    public ARPlaneManagerController planeController;

    [Header("UI")]
    public TextMeshProUGUI scanningTMP;
    public TextMeshProUGUI sizeTMP;

    [Header("Scanning Text")]
    public string defaultValidPlacementText = "Area valid. Tap untuk mulai";
    public bool useLevelZeroTutorialPlacementText = true;
    public int tutorialPlacementLevelIndex = 0;
    public string levelZeroValidPlacementText = "Tap layar untuk spawn arena";

    [Header("Board")]
    public int boardWidthCells = 8;
    public int boardHeightCells = 6;

    [Header("AR Comfort Size")]
    public bool useFixedArenaCellSize = true;
    public float fixedArenaCellSize = 0.06f;
    public bool keepFixedArenaInsidePlane = true;
    public float planeEdgePadding = 0.04f;

    [Header("Board Size Stability")]
    public bool clampCellSize = true;
    public float minCellSize = 0.035f;
    public float maxCellSize = 0.085f;
    public bool snapCellSizeToStep = true;
    public float cellSizeStep = 0.005f;

    [Header("Preview Visual")]
    public float previewYOffset = 0.004f;
    public float boardYOffset = 0.001f;
    public float smoothSpeed = 10f;
    public float fillThickness = 0.0015f;
    public float frameThickness = 0.008f;
    public float frameHeight = 0.004f;
    public float cornerLength = 0.06f;
    public float gridLineThickness = 0.003f;
    public float gridLineHeight = 0.002f;

    [Header("Preview Animation")]
    public float pulseSpeed = 3f;
    public float pulseIntensity = 0.18f;
    public float bobAmplitude = 0.0015f;

    [Header("Colors")]
    public Color validFillColor = new Color(0.12f, 0.95f, 0.60f, 0.16f);
    public Color invalidFillColor = new Color(1f, 0.28f, 0.25f, 0.14f);
    public Color validFrameColor = new Color(0.18f, 1f, 0.70f, 0.95f);
    public Color invalidFrameColor = new Color(1f, 0.35f, 0.35f, 0.95f);
    public Color validGridColor = new Color(0.85f, 1f, 0.92f, 0.50f);
    public Color invalidGridColor = new Color(1f, 0.85f, 0.85f, 0.35f);

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private bool placed;
    private bool hasPlaneCandidate;

    private ARPlane currentPlane;
    private Pose currentPose;
    private Vector2 currentDetectedSize;
    private Vector2 currentArenaSize;
    private float currentCellSize;

    private Transform previewRoot;
    private Renderer fillRenderer;
    private readonly Renderer[] edgeRenderers = new Renderer[4];
    private readonly Renderer[] cornerRenderers = new Renderer[8];
    private readonly List<Renderer> gridRenderers = new List<Renderer>();

    private Transform fillTransform;
    private readonly Transform[] edgeTransforms = new Transform[4];
    private readonly Transform[] cornerTransforms = new Transform[8];
    private readonly List<Transform> gridLineTransforms = new List<Transform>();

    private Vector3 smoothedPosition;
    private Quaternion smoothedRotation = Quaternion.identity;
    private Vector2 smoothedDetectedSize;
    private float previewTime;
    private bool previewInitialized;

    void Awake()
    {
        BuildPreviewVisual();
    }

    void Update()
    {
        if (placed) return;

        UpdatePlacementCandidate();
        UpdatePreviewVisual();
        UpdateScanningUI();
        HandlePlacementInput();
    }

    void UpdatePlacementCandidate()
    {
        hasPlaneCandidate = false;
        currentPlane = null;
        currentDetectedSize = Vector2.zero;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!TryGetHorizontalPlaneAt(screenCenter, out ARPlane plane, out Pose pose))
        {
            if (planeVisualizer != null)
                planeVisualizer.SetCandidatePlane(null, false);
            return;
        }

        currentPlane = plane;
        currentDetectedSize = plane.size;

        if (planeVisualizer != null)
            currentArenaSize = planeVisualizer.GetClampedArenaSize(plane);
        else
            currentArenaSize = currentDetectedSize;

        currentCellSize = CalculateCellSize(currentArenaSize);

        Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        pose.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        currentPose = pose;
        hasPlaneCandidate = true;

        if (planeVisualizer != null)
            planeVisualizer.SetCandidatePlane(currentPlane, IsUsablePlane(currentPlane));
    }

    bool TryGetHorizontalPlaneAt(Vector2 screenPosition, out ARPlane plane, out Pose pose)
    {
        plane = null;
        pose = default;

        if (raycastManager == null)
            return false;

        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            return false;

        for (int i = 0; i < hits.Count; i++)
        {
            plane = hits[i].trackable as ARPlane;
            if (plane == null)
                continue;

            if (plane.alignment != PlaneAlignment.HorizontalUp)
                continue;

            pose = hits[i].pose;
            return true;
        }

        return false;
    }

    bool IsUsablePlane(ARPlane plane)
    {
        if (planeVisualizer != null)
            return planeVisualizer.IsUsablePlane(plane);

        return false;
    }

    float CalculateCellSize(Vector2 arenaSize)
    {
        if (useFixedArenaCellSize)
        {
            float fixedResult = fixedArenaCellSize;

            if (keepFixedArenaInsidePlane)
            {
                float usableWidth = Mathf.Max(0.001f, arenaSize.x - (planeEdgePadding * 2f));
                float usableHeight = Mathf.Max(0.001f, arenaSize.y - (planeEdgePadding * 2f));
                float maxFitCellSize = Mathf.Min(usableWidth / boardWidthCells, usableHeight / boardHeightCells);
                fixedResult = Mathf.Min(fixedResult, maxFitCellSize);
            }

            if (clampCellSize)
                fixedResult = Mathf.Clamp(fixedResult, minCellSize, maxCellSize);

            if (snapCellSizeToStep && cellSizeStep > 0.0001f)
                fixedResult = Mathf.Round(fixedResult / cellSizeStep) * cellSizeStep;

            return Mathf.Max(0.001f, fixedResult);
        }

        float cellByWidth = arenaSize.x / boardWidthCells;
        float cellByHeight = arenaSize.y / boardHeightCells;
        float result = Mathf.Min(cellByWidth, cellByHeight);

        if (clampCellSize)
            result = Mathf.Clamp(result, minCellSize, maxCellSize);

        if (snapCellSizeToStep && cellSizeStep > 0.0001f)
            result = Mathf.Round(result / cellSizeStep) * cellSizeStep;

        return Mathf.Max(0.001f, result);
    }

    Vector2 GetBoardWorldSize(float cellSize)
    {
        return new Vector2(cellSize * boardWidthCells, cellSize * boardHeightCells);
    }

    void HandlePlacementInput()
    {
        if (!hasPlaneCandidate) return;
        if (!IsUsablePlane(currentPlane)) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (touch.phase != TouchPhase.Began)
            return;

        GameObject board = new GameObject("Board");
        board.transform.position = currentPose.position + (Vector3.up * boardYOffset);
        board.transform.rotation = currentPose.rotation;

        gridManager.transform.SetParent(board.transform, false);
        gridManager.transform.localPosition = Vector3.zero;
        gridManager.transform.localRotation = Quaternion.identity;

        gridManager.InitializeGrid(currentCellSize, boardWidthCells, boardHeightCells);

        placed = true;
        SfxManager.Instance?.PlayPlaceArena();
        ArenaPlaced?.Invoke();

        if (previewRoot != null)
            previewRoot.gameObject.SetActive(false);

        if (planeController != null)
            planeController.DisablePlaneDetection();

        if (raycastManager != null)
            raycastManager.enabled = false;

        if (scanningTMP != null)
            scanningTMP.gameObject.SetActive(false);

        if (sizeTMP != null)
            sizeTMP.gameObject.SetActive(false);
    }

    void UpdatePreviewVisual()
    {
        if (previewRoot == null)
            return;

        if (!hasPlaneCandidate)
        {
            previewRoot.gameObject.SetActive(false);
            previewInitialized = false;
            return;
        }

        previewRoot.gameObject.SetActive(true);

        if (!previewInitialized)
        {
            smoothedPosition = currentPose.position;
            smoothedRotation = currentPose.rotation;
            smoothedDetectedSize = currentDetectedSize;
            previewInitialized = true;
        }
        else
        {
            smoothedPosition = Vector3.Lerp(smoothedPosition, currentPose.position, Time.deltaTime * smoothSpeed);
            smoothedRotation = Quaternion.Slerp(smoothedRotation, currentPose.rotation, Time.deltaTime * smoothSpeed);
            smoothedDetectedSize = Vector2.Lerp(smoothedDetectedSize, currentDetectedSize, Time.deltaTime * smoothSpeed);
        }

        previewTime += Time.deltaTime;

        bool valid = IsUsablePlane(currentPlane);
        Vector2 boardSize = GetBoardWorldSize(currentCellSize);

        float bob = Mathf.Sin(previewTime * pulseSpeed) * bobAmplitude;
        previewRoot.position = smoothedPosition + new Vector3(0f, previewYOffset + bob, 0f);
        previewRoot.rotation = smoothedRotation;

        UpdatePreviewGeometry(boardSize, currentCellSize);
        UpdatePreviewColors(valid);
        UpdateSizeText(valid);
    }

    void UpdatePreviewGeometry(Vector2 boardSize, float cellSize)
    {
        float width = boardSize.x;
        float depth = boardSize.y;

        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        fillTransform.localPosition = Vector3.zero;
        fillTransform.localRotation = Quaternion.identity;
        fillTransform.localScale = new Vector3(width, fillThickness, depth);

        edgeTransforms[0].localPosition = new Vector3(0f, frameHeight * 0.5f, halfD);
        edgeTransforms[0].localScale = new Vector3(width, frameHeight, frameThickness);

        edgeTransforms[1].localPosition = new Vector3(0f, frameHeight * 0.5f, -halfD);
        edgeTransforms[1].localScale = new Vector3(width, frameHeight, frameThickness);

        edgeTransforms[2].localPosition = new Vector3(-halfW, frameHeight * 0.5f, 0f);
        edgeTransforms[2].localScale = new Vector3(frameThickness, frameHeight, depth);

        edgeTransforms[3].localPosition = new Vector3(halfW, frameHeight * 0.5f, 0f);
        edgeTransforms[3].localScale = new Vector3(frameThickness, frameHeight, depth);

        float cornerX = Mathf.Min(cornerLength, width * 0.35f);
        float cornerZ = Mathf.Min(cornerLength, depth * 0.35f);
        float cornerY = frameHeight * 0.8f;

        SetCornerPair(0, 1, -halfW, halfD, cornerX, cornerZ, cornerY);
        SetCornerPair(2, 3, halfW, halfD, cornerX, cornerZ, cornerY);
        SetCornerPair(4, 5, -halfW, -halfD, cornerX, cornerZ, cornerY);
        SetCornerPair(6, 7, halfW, -halfD, cornerX, cornerZ, cornerY);

        int index = 0;
        for (int x = 1; x < boardWidthCells; x++)
        {
            float posX = -halfW + (cellSize * x);
            gridLineTransforms[index].localPosition = new Vector3(posX, gridLineHeight * 0.5f, 0f);
            gridLineTransforms[index].localScale = new Vector3(gridLineThickness, gridLineHeight, depth);
            index++;
        }

        for (int z = 1; z < boardHeightCells; z++)
        {
            float posZ = -halfD + (cellSize * z);
            gridLineTransforms[index].localPosition = new Vector3(0f, gridLineHeight * 0.5f, posZ);
            gridLineTransforms[index].localScale = new Vector3(width, gridLineHeight, gridLineThickness);
            index++;
        }
    }

    void SetCornerPair(int hIndex, int vIndex, float x, float z, float lengthX, float lengthZ, float y)
    {
        float dirX = Mathf.Sign(x);
        float dirZ = Mathf.Sign(z);

        cornerTransforms[hIndex].localPosition = new Vector3(
            x - (dirX * (lengthX * 0.5f)),
            y * 0.5f,
            z);

        cornerTransforms[hIndex].localScale = new Vector3(lengthX, y, frameThickness * 1.2f);

        cornerTransforms[vIndex].localPosition = new Vector3(
            x,
            y * 0.5f,
            z - (dirZ * (lengthZ * 0.5f)));

        cornerTransforms[vIndex].localScale = new Vector3(frameThickness * 1.2f, y, lengthZ);
    }

    void UpdatePreviewColors(bool valid)
    {
        float pulse01 = (Mathf.Sin(previewTime * pulseSpeed) * 0.5f) + 0.5f;
        float boost = 1f + (pulse01 * pulseIntensity);

        Color fill = valid ? validFillColor : invalidFillColor;
        Color frame = valid ? validFrameColor : invalidFrameColor;
        Color grid = valid ? validGridColor : invalidGridColor;

        fillRenderer.material.color = fill;

        Color animatedFrame = new Color(
            Mathf.Clamp01(frame.r * boost),
            Mathf.Clamp01(frame.g * boost),
            Mathf.Clamp01(frame.b * boost),
            frame.a);

        for (int i = 0; i < edgeRenderers.Length; i++)
            edgeRenderers[i].material.color = animatedFrame;

        for (int i = 0; i < cornerRenderers.Length; i++)
            cornerRenderers[i].material.color = animatedFrame;

        for (int i = 0; i < gridRenderers.Count; i++)
            gridRenderers[i].material.color = grid;
    }

    void UpdateScanningUI()
    {
        if (scanningTMP == null)
            return;

        if (!hasPlaneCandidate || currentPlane == null)
        {
            scanningTMP.text = "Scan permukaan";
            scanningTMP.color = Color.white;
            return;
        }

        if (planeVisualizer != null && planeVisualizer.IsLikelyFloorPlane(currentPlane))
        {
            scanningTMP.text = "Lantai / permukaan tidak bisa dipakai";
            scanningTMP.color = Color.red;
            return;
        }

        if (!IsUsablePlane(currentPlane))
        {
            scanningTMP.text = "Permukaan terlalu kecil";
            scanningTMP.color = Color.red;
            return;
        }

        scanningTMP.text = GetValidPlacementText();
        scanningTMP.color = Color.green;
    }

    string GetValidPlacementText()
    {
        if (useLevelZeroTutorialPlacementText)
        {
            int selectedLevel = PlayerPrefs.GetInt(LevelProgress.SelectedLevelKey, 0);
            if (selectedLevel == tutorialPlacementLevelIndex)
                return levelZeroValidPlacementText;
        }

        return defaultValidPlacementText;
    }

    void UpdateSizeText(bool valid)
    {
        if (sizeTMP == null)
            return;

        if (currentPlane == null)
        {
            sizeTMP.text = "";
            return;
        }

        Vector2 actualBoardSize = GetBoardWorldSize(currentCellSize);
        bool clamped = planeVisualizer != null &&
            (currentDetectedSize.x > planeVisualizer.maxArenaWidth ||
             currentDetectedSize.y > planeVisualizer.maxArenaHeight);
        string sizeMode = useFixedArenaCellSize ? "tetap" : "mengikuti permukaan";

        sizeTMP.text =
            $"Ukuran permukaan: {currentDetectedSize.x * 100f:F0} x {currentDetectedSize.y * 100f:F0} cm\n" +
            $"Arena: {actualBoardSize.x * 100f:F0} x {actualBoardSize.y * 100f:F0} cm{(clamped ? " (maks)" : "")}\n" +
            $"Mode ukuran: {sizeMode}\n" +
            $"Status: {(valid ? "Bisa dipakai" : "Tidak bisa dipakai")}";
    }

    void BuildPreviewVisual()
    {
        previewRoot = new GameObject("ArenaPreview").transform;
        previewRoot.gameObject.SetActive(false);

        fillTransform = CreatePreviewPart("Fill", previewRoot, out fillRenderer);

        edgeTransforms[0] = CreatePreviewPart("EdgeTop", previewRoot, out edgeRenderers[0]);
        edgeTransforms[1] = CreatePreviewPart("EdgeBottom", previewRoot, out edgeRenderers[1]);
        edgeTransforms[2] = CreatePreviewPart("EdgeLeft", previewRoot, out edgeRenderers[2]);
        edgeTransforms[3] = CreatePreviewPart("EdgeRight", previewRoot, out edgeRenderers[3]);

        for (int i = 0; i < cornerTransforms.Length; i++)
            cornerTransforms[i] = CreatePreviewPart($"Corner_{i}", previewRoot, out cornerRenderers[i]);

        int totalGridLines = (boardWidthCells - 1) + (boardHeightCells - 1);
        for (int i = 0; i < totalGridLines; i++)
        {
            Renderer rend;
            Transform line = CreatePreviewPart($"GridLine_{i}", previewRoot, out rend);
            gridLineTransforms.Add(line);
            gridRenderers.Add(rend);
        }
    }

    Transform CreatePreviewPart(string objectName, Transform parent, out Renderer renderer)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            obj.layer = ignoreRaycastLayer;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        renderer = obj.GetComponent<Renderer>();
        renderer.material = CreateTransparentMaterial();

        return obj.transform;
    }

    Material CreateTransparentMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);

        mat.renderQueue = 3000;
        return mat;
    }
    
    public bool IsPlaced()
    {
        return placed;
    }
}
