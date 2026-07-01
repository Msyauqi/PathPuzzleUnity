using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathPuzzle;

public class BallController : MonoBehaviour
{
    [System.Serializable]
    public class SkinTrailColorProfile
    {
        public int skinIndex = 0;
        public bool useSpecialGradient = false;
        public Color basicColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        public Color specialColorA = new Color(1f, 0.2f, 0.2f, 0.95f);
        public Color specialColorB = new Color(1f, 0.95f, 0.2f, 0.85f);
        public Color specialColorC = new Color(1f, 0.45f, 0.05f, 0.2f);
    }

    [System.Serializable]
    public class SkinPrefabTransformProfile
    {
        public int skinIndex = 0;
        public bool overridePosition = false;
        public Vector3 localPosition = Vector3.zero;
        public bool overrideEuler = false;
        public Vector3 localEuler = Vector3.zero;
        public bool overrideScale = false;
        public Vector3 localScale = Vector3.one;
        public bool overrideDiameterMultiplier = false;
        [Range(0.1f, 3f)] public float diameterMultiplier = 1f;
    }

    public GridManager gridManager;
    public Transform ballVisual;
    public float moveSpeed = 3f;
    [Header("Motion Feel")]
    public bool easeEachTile = false;
    [Range(6, 24)] public int bezierLengthSamples = 12;
    [Header("Visual Orientation")]
    public bool resetVisualRotationOnLevelReset = true;
    public bool alignVisualToInitialMoveDirection = true;
    public Vector3 visualForwardAxisLocal = Vector3.forward;
    public float visualYawOffsetDegrees = 0f;
    public bool reorientVisualAtCorners = true;

    [Header("Ball Skin Prefab (Optional)")]
    public bool useSkinPrefabs = true;
    public GameObject[] skinVisualPrefabs;
    public Vector3 skinPrefabLocalPosition = Vector3.zero;
    public Vector3 skinPrefabLocalEuler = Vector3.zero;
    public Vector3 skinPrefabLocalScale = Vector3.one;
    public bool disableCollidersOnSkinPrefab = true;
    [Tooltip("Isi jika ada prefab skin tertentu yang perlu posisi/rotasi/ukuran berbeda, misalnya Y = 90 derajat.")]
    public bool usePerSkinPrefabTransformProfiles = true;
    public SkinPrefabTransformProfile[] skinPrefabTransformProfiles;

    [Header("Ball Visual Normalize")]
    public bool autoNormalizeBaseBallVisualSize = true;
    public bool autoNormalizeSkinPrefabSize = true;
    public bool autoCenterSkinPrefabOnBall = true;
    [Range(0.1f, 3f)] public float skinDiameterMultiplier = 1f;
    [Range(0.1f, 3f)] public float ballSizeMultiplier = 0.36f;
    public float ballHeightOffset = 0f;
    [Header("Ball Surface Height")]
    public bool autoPlaceBallOnTileSurface = true;
    public float ballSurfaceClearance = 0f;
    public bool limitLegacyHeightOffsetWhenAutoSurface = true;

    [Header("Ball Skin")]
    public Renderer ballRenderer;
    public Material[] skinMaterials;
    public bool autoApplySavedSkin = true;
    public bool generateTintSkinsWhenMissing = true;
    public Color[] generatedSkinColors = new Color[]
    {
        new Color(1f, 0.84f, 0.18f), // default gold
        new Color(0.35f, 0.85f, 1f), // aqua
        new Color(1f, 0.45f, 0.45f), // coral red
        new Color(0.72f, 0.55f, 1f), // purple
        new Color(0.45f, 1f, 0.55f), // green
        new Color(1f, 0.55f, 0.88f), // pink
    };

    [Header("Trail Effect")]
    public bool enableTrailEffect = true;
    public Transform trailEffectAnchor;
    public Vector3 trailEffectLocalOffset = Vector3.zero;
    public bool trailOnlyWhileMoving = true;
    public bool clearTrailOnReset = true;
    public bool parentTrailToBallRoot = true;
    public bool keepSimpleTrailInWorldRoot = true;

    [Header("Simple Built-in Trail (No URP)")]
    public Material simpleTrailMaterial;
    public bool forceUnlitColorTrailMaterial = true;
    [Range(0.05f, 2f)] public float simpleTrailTime = 0.28f;
    [Range(0.005f, 0.35f)] public float simpleTrailStartWidthRelativeToBall = 0.12f;
    [Range(0f, 0.2f)] public float simpleTrailEndWidthRelativeToBall = 0.018f;
    [Range(0.001f, 0.5f)] public float simpleTrailMinVertexDistance = 0.006f;
    [Tooltip("Jika aktif, lebar trail dihitung dari target ukuran bola berdasarkan grid. Ini lebih stabil untuk prefab skin yang bounds-nya besar.")]
    public bool useTargetDiameterForSimpleTrailWidth = true;
    [Tooltip("Jika aktif, lebar trail diprioritaskan dari ukuran bola yang benar-benar terlihat agar tidak membesar saat skala AR/grid berubah.")]
    public bool preferCurrentBallDiameterForTrailWidth = true;
    [Range(0.05f, 2f)] public float simpleTrailWidthDiameterMultiplier = 1f;
    [Tooltip("Batas maksimum lebar pangkal trail dibanding diameter bola yang terlihat.")]
    [Range(0.05f, 1.5f)] public float simpleTrailMaxStartWidthRelativeToCurrentBall = 0.65f;
    [Tooltip("Batas maksimum lebar ujung trail dibanding diameter bola yang terlihat.")]
    [Range(0f, 0.5f)] public float simpleTrailMaxEndWidthRelativeToCurrentBall = 0.12f;
    public LineAlignment simpleTrailAlignment = LineAlignment.View;
    public bool forceTrailFadeOutAtEnd = true;
    [Range(0, 16)] public int simpleTrailCornerVertices = 6;
    [Range(0, 16)] public int simpleTrailCapVertices = 6;
    [Range(0.1f, 1f)] public float specialTrailTimeMultiplier = 0.55f;

    [Header("Trail Color Concept (Baru)")]
    [Tooltip("Skin index >= nilai ini dianggap Special jika Special Skin Indices kosong.")]
    public int specialSkinStartIndex = 6;
    [Tooltip("Opsional: isi manual index skin yang termasuk Special. Jika ada isi, ini yang dipakai.")]
    public int[] specialSkinIndices;
    [Tooltip("Jika aktif, skin index >= Special Skin Start Index tetap dianggap Special meskipun Special Skin Indices juga diisi.")]
    public bool combineSpecialStartIndexWithManualList = true;
    public Color defaultBasicTrailColor = new Color(1f, 0.85f, 0.2f, 0.95f);
    public Color defaultSpecialTrailColorA = new Color(1f, 0.25f, 0.25f, 0.95f);
    public Color defaultSpecialTrailColorB = new Color(1f, 0.9f, 0.25f, 0.85f);
    public Color defaultSpecialTrailColorC = new Color(1f, 0.45f, 0.05f, 0.2f);
    public bool usePerSkinTrailColorProfiles = true;
    [Tooltip("Jika skin punya profile manual, checkbox Use Special Gradient di profile itu yang menentukan 1 warna atau 3 warna.")]
    public bool perSkinProfileOverridesSpecialMode = true;
    public SkinTrailColorProfile[] skinTrailColorProfiles;
    [Range(0.05f, 0.6f)] public float specialTrailMiddleColorTime = 0.25f;
    [Range(0.2f, 0.9f)] public float specialTrailEndColorTime = 0.58f;

    [Header("Trail Placement")]
    public bool autoScaleTrailToBall = true;
    [Range(0.05f, 2f)] public float trailSizeRelativeToBall = 0.75f;
    [Range(0.01f, 3f)] public float trailScaleMultiplier = 1f;
    public bool positionTrailBehindBall = true;
    [Range(0f, 2f)] public float trailBehindDistanceMultiplier = 0.55f;
    [Range(-1f, 1f)] public float trailVerticalOffsetMultiplier = -0.1f;

    private const float TURN_CURVE_STRENGTH = 0.18f;
    private const string RUNTIME_SIMPLE_TRAIL_NAME = "RuntimeSimpleTrail";
    private const string RUNTIME_SKIN_SUFFIX = "_RuntimeSkin";

    private Vector2Int currentPos;
    private Direction currentDir;
    private bool moving = false;
    private Renderer baseBallRenderer;
    private GameObject spawnedSkinVisual;
    private int spawnedSkinIndex = -1;
    private float cachedBaseVisualDiameter = -1f;
    private Transform cachedDiameterSource;
    private Quaternion initialBallVisualLocalRotation = Quaternion.identity;
    private Transform cachedInitialRotationSource;
    private Quaternion initialSpawnedSkinLocalRotation = Quaternion.identity;
    private GameObject spawnedTrailEffect;
    private TrailRenderer[] cachedTrailRenderers = new TrailRenderer[0];
    private int activeTrailSkinIndex = -1;
    private Vector3 lastTrailMoveDirectionWorld = Vector3.forward;
    private Material runtimeSimpleTrailMaterial;
    private bool isRuntimeSimpleTrail;

    public Direction CurrentDirection => currentDir;

    void Start()
    {
        ResolveBallVisual();
        CacheInitialBallVisualRotation();
        ApplyBallSize();
        ResolveBallRenderer();
        baseBallRenderer = ballRenderer;
        EnsureSkinMaterials();

        if (autoApplySavedSkin || useSkinPrefabs)
            ApplySelectedSkin();
        else
            ApplyTrailProfileForSkin(BallSkinStore.GetSelectedSkinIndex(), true);

        InitializeTrailEffect();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        cachedBaseVisualDiameter = -1f;
        cachedDiameterSource = null;
        ResolveBallVisual();
        CacheInitialBallVisualRotation();
    }

    // =========================
    void ApplyBallSize()
    {
        ResolveBallVisual();
        if (gridManager == null || ballVisual == null) return;

        float targetDiameter = GetTargetBallDiameter();
        float nextUniformScale = targetDiameter;

        if (autoNormalizeBaseBallVisualSize)
        {
            float sourceDiameter = GetBaseVisualDiameterCached();
            if (sourceDiameter > 0.00001f)
                nextUniformScale = targetDiameter / sourceDiameter;
        }

        ballVisual.localScale = Vector3.one * nextUniformScale;
    }

    void ResolveBallRenderer()
    {
        if (ballRenderer != null)
            return;

        if (ballVisual != null)
        {
            ballRenderer = ballVisual.GetComponent<Renderer>();
            if (ballRenderer == null)
                ballRenderer = ballVisual.GetComponentInChildren<Renderer>(true);
        }

        if (ballRenderer == null)
            ballRenderer = GetComponentInChildren<Renderer>(true);
    }

    void ResolveBallVisual()
    {
        if (IsBallVisualReferenceValid())
            return;

        if (ballRenderer != null)
        {
            Transform candidate = ballRenderer.transform;
            if (candidate == transform || candidate.IsChildOf(transform))
            {
                ballVisual = candidate;
                return;
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            Transform candidate = r.transform;
            if (candidate == transform || candidate.IsChildOf(transform))
            {
                ballVisual = candidate;
                return;
            }
        }

        ballVisual = transform;
    }

    void CacheInitialBallVisualRotation()
    {
        if (ballVisual == null)
            return;

        if (cachedInitialRotationSource == ballVisual)
            return;

        cachedInitialRotationSource = ballVisual;
        initialBallVisualLocalRotation = ballVisual.localRotation;
    }

    bool IsBallVisualReferenceValid()
    {
        if (ballVisual == null)
            return false;

        return ballVisual == transform || ballVisual.IsChildOf(transform);
    }

    public void ApplySelectedSkin()
    {
        ResolveBallRenderer();
        EnsureSkinMaterials();
        CleanupRuntimeSkinVisualChildren();

        int selectedSkin = BallSkinStore.GetSelectedSkinIndex();
        int appliedSkinIndex = Mathf.Max(0, selectedSkin);

        if (TryApplyPrefabSkin(selectedSkin, out int prefabSkinIndex))
        {
            appliedSkinIndex = prefabSkinIndex;
            ApplyTrailProfileForSkin(appliedSkinIndex, true);
            GameManager.Instance?.RefreshStatusBallImage();
            return;
        }

        ClearSpawnedSkinVisual();
        ApplyBallSize();
        SetBaseBallRendererVisible(true);

        if (ballRenderer != null && skinMaterials != null && skinMaterials.Length > 0)
        {
            int clamped = Mathf.Clamp(selectedSkin, 0, skinMaterials.Length - 1);
            if (!BallSkinStore.IsUnlocked(clamped))
                clamped = 0;
            Material chosen = skinMaterials[clamped];
            if (chosen == null)
                chosen = skinMaterials[0];

            if (chosen != null)
                ballRenderer.sharedMaterial = chosen;

            appliedSkinIndex = clamped;
        }

        ApplyTrailProfileForSkin(appliedSkinIndex, true);
        GameManager.Instance?.RefreshStatusBallImage();
    }

    public bool BuyOrSelectSkin(int skinIndex, out string message)
    {
        bool success = BallSkinStore.TryBuyAndSelectSkin(skinIndex, out message);
        if (success)
            ApplySelectedSkin();
        return success;
    }

    bool TryApplyPrefabSkin(int selectedSkin, out int appliedSkinIndex)
    {
        appliedSkinIndex = Mathf.Max(0, selectedSkin);

        if (!useSkinPrefabs || ballVisual == null || skinVisualPrefabs == null || skinVisualPrefabs.Length == 0)
            return false;

        int clamped = Mathf.Clamp(selectedSkin, 0, skinVisualPrefabs.Length - 1);
        if (!BallSkinStore.IsUnlocked(clamped))
            clamped = 0;
        appliedSkinIndex = clamped;

        GameObject prefab = skinVisualPrefabs[clamped];
        if (prefab == null)
            return false;

        CleanupRuntimeSkinVisualChildren();

        if (spawnedSkinVisual != null && spawnedSkinIndex == clamped)
        {
            ApplySpawnedSkinVisualLayout();
            SetBaseBallRendererVisible(false);
            return true;
        }

        ClearSpawnedSkinVisual();

        spawnedSkinVisual = Instantiate(prefab, ballVisual);
        spawnedSkinVisual.name = $"{prefab.name}_RuntimeSkin";
        spawnedSkinIndex = clamped;

        ApplySpawnedSkinVisualLayout();

        if (disableCollidersOnSkinPrefab)
        {
            Collider[] colliders = spawnedSkinVisual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        SetBaseBallRendererVisible(false);
        return true;
    }

    void ApplySpawnedSkinVisualLayout()
    {
        if (ballVisual == null || spawnedSkinVisual == null)
            return;

        // Prefab skins already contain their own mesh size, so keep the holder at 1
        // and normalize the prefab itself against the latest grid cell size.
        ballVisual.localScale = Vector3.one;

        Transform rt = spawnedSkinVisual.transform;
        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        SkinPrefabTransformProfile profile = null;
        TryGetSkinPrefabTransformProfile(spawnedSkinIndex, out profile);

        Vector3 finalPosition = skinPrefabLocalPosition;
        Vector3 finalEuler = skinPrefabLocalEuler;
        Vector3 finalScale = skinPrefabLocalScale;
        float finalDiameterMultiplier = skinDiameterMultiplier;

        if (profile != null)
        {
            if (profile.overridePosition)
                finalPosition = profile.localPosition;
            if (profile.overrideEuler)
                finalEuler = profile.localEuler;
            if (profile.overrideScale)
                finalScale = profile.localScale;
            if (profile.overrideDiameterMultiplier)
                finalDiameterMultiplier = profile.diameterMultiplier;
        }

        // Apply rotation before normalization so non-symmetric prefab bounds are measured correctly.
        rt.localRotation = Quaternion.Euler(finalEuler);

        if (autoNormalizeSkinPrefabSize)
            NormalizeSpawnedSkinVisual(rt, finalDiameterMultiplier);

        rt.localPosition += finalPosition;
        rt.localScale = Vector3.Scale(rt.localScale, finalScale);
        initialSpawnedSkinLocalRotation = rt.localRotation;
    }

    void ClearSpawnedSkinVisual()
    {
        if (spawnedSkinVisual != null)
        {
            spawnedSkinVisual.SetActive(false);
            DestroyRuntimeObject(spawnedSkinVisual);
        }

        spawnedSkinVisual = null;
        spawnedSkinIndex = -1;
        CleanupRuntimeSkinVisualChildren();
    }

    void CleanupRuntimeSkinVisualChildren()
    {
        if (ballVisual == null)
            return;

        for (int i = ballVisual.childCount - 1; i >= 0; i--)
        {
            Transform child = ballVisual.GetChild(i);
            if (child == null)
                continue;

            GameObject childObject = child.gameObject;
            if (childObject == null || childObject == spawnedSkinVisual)
                continue;

            if (!childObject.name.EndsWith(RUNTIME_SKIN_SUFFIX))
                continue;

            DestroyRuntimeObject(childObject);
        }
    }

    void SetBaseBallRendererVisible(bool visible)
    {
        if (baseBallRenderer == null && ballVisual != null)
            baseBallRenderer = ballVisual.GetComponent<Renderer>();

        if (baseBallRenderer != null)
            baseBallRenderer.enabled = visible;
    }

    void EnsureSkinMaterials()
    {
        if (skinMaterials != null && skinMaterials.Length > 0)
            return;

        if (!generateTintSkinsWhenMissing)
            return;

        ResolveBallRenderer();
        if (ballRenderer == null)
            return;

        Material source = ballRenderer.sharedMaterial;
        if (source == null)
            source = ballRenderer.material;

        if (source == null)
            return;

        if (generatedSkinColors == null || generatedSkinColors.Length == 0)
            return;

        skinMaterials = new Material[generatedSkinColors.Length];
        for (int i = 0; i < generatedSkinColors.Length; i++)
        {
            Material m = new Material(source);
            if (m.HasProperty("_Color"))
                m.color = generatedSkinColors[i];
            skinMaterials[i] = m;
        }
    }

    // =========================
    public void ResetToStart()
    {
        StopMovement();
        transform.localScale = Vector3.one;
        CacheInitialBallVisualRotation();
        ApplyBallSize();
        ApplySelectedSkin();
        
        currentPos = gridManager.currentLevel.startPosition;

        PathTile startTile = gridManager.GetTileAt(currentPos) as PathTile;
        
        if (startTile != null)  
        {
            var path = startTile.GetPath(Direction.None);

            currentDir = (path != null && path.Count > 0)
                ? path[0]
                : Direction.Right;
                
        }
        else
        {
            currentDir = Direction.Right;
            
        }

        ResetVisualOrientation(currentDir);

        UpdatePositionInstant();
        SetTrailActive(!trailOnlyWhileMoving, clearTrailOnReset);
        GameManager.Instance?.SetBallReadyStatus(currentDir);
        
    }

    // =========================
    public void StartMovement()
    {
        if (moving) return;

        moving = true;
        SetTrailActive(true, false);
        SfxManager.Instance?.StartBallRolling();
        GameManager.Instance?.SetBallDirectionStatus(currentDir);
        StartCoroutine(MoveRoutine());
    }

    public void StopMovement()
    {
        StopAllCoroutines();
        moving = false;
        SfxManager.Instance?.StopBallRolling();
        SetTrailActive(!trailOnlyWhileMoving, clearTrailOnReset);
    }

    public void RefreshPositionOnCurrentTile()
    {
        if (moving || gridManager == null)
            return;

        UpdatePositionInstant();
    }

    // =========================
    IEnumerator MoveRoutine()
    {
        int safety = 0;

        while (moving)
        {
            safety++;
            if (safety > 200)
            {
                Debug.LogError("[SAFE STOP]");
                moving = false;
                yield break;
            }

            PathTile tile = gridManager.GetTileAt(currentPos) as PathTile;

            if (tile == null) { Lose(currentPos); yield break; }
            if (tile.pathType == PathType.Finish) { Win(); yield break; }

            Direction enter = currentDir.Opposite();

            Debug.Log($"[DEBUG] pos={currentPos} tile={tile.name} enter={enter}");

            List<Direction> steps = tile.GetPath(enter);

            if (steps == null || steps.Count == 0) { Lose(currentPos); yield break; }

            Debug.Log($"[DEBUG] steps={string.Join(",", steps)}");

            for (int i = 0; i < steps.Count; i++)
            {
                Direction step = steps[i];
                Direction prevStep = (i == 0) ? currentDir : steps[i - 1];
                Direction nextStep = (i < steps.Count - 1) ? steps[i + 1] : Direction.None;

                Vector2Int nextPos = currentPos + step.ToVector();

                if (!gridManager.IsValidPosition(nextPos)) { Lose(nextPos); yield break; }

                bool cornerStep = IsCorner(prevStep, step);
                if (reorientVisualAtCorners && cornerStep)
                    ResetVisualOrientation(step);


                Debug.Log($"[STEP] {currentPos} → {step} → {nextPos}");

                yield return MoveSmooth(nextPos, prevStep, step, nextStep);
                if (reorientVisualAtCorners && cornerStep)
                    ResetVisualOrientation(step);


                currentPos = nextPos;
                currentDir = step;
                UpdateTrailPlacementByCurrentDirection(true);

                if (i == steps.Count - 1)
                {
                    PathTile nextTile = gridManager.GetTileAt(currentPos) as PathTile;
                    Debug.Log($"[NEXT TILE] pos={currentPos} tile={nextTile?.name ?? "NULL"}");

                    if (nextTile == null) { Lose(currentPos); yield break; }
                    if (nextTile.pathType == PathType.Finish) { Win(); yield break; }
                }
            }
        }
    }

    // =========================
    IEnumerator MoveSmooth(Vector2Int targetPos, Direction prevDir, Direction moveDir, Direction nextDir)
    {
        Vector3 start = transform.localPosition;
        Vector3 end = GetWorldPosition(targetPos);
        Vector3 control = GetTurnControlPoint(start, end, prevDir, moveDir, nextDir);

        float t = 0f;
        Vector3 previousPos = start;
        float estimatedLength = EstimateQuadraticBezierLength(start, control, end, Mathf.Max(6, bezierLengthSamples));
        if (estimatedLength <= 0.00001f)
        {
            transform.localPosition = end;
            yield break;
        }

        while (t < 1f)
        {
            float stepDistance = moveSpeed * Time.deltaTime;
            float stepT = stepDistance / estimatedLength;
            t = Mathf.Min(1f, t + stepT);

            float normalized = Mathf.Clamp01(t);
            float evalT = easeEachTile ? Mathf.SmoothStep(0f, 1f, normalized) : normalized;

            Vector3 current = EvaluateQuadraticBezier(start, control, end, evalT);
            transform.localPosition = current;

            RotateBall(previousPos, current);
            UpdateTrailRuntimeFromDelta(previousPos, current, false);
            previousPos = current;

            yield return null;
        }

        transform.localPosition = end;
        RotateBall(previousPos, end);
        UpdateTrailRuntimeFromDelta(previousPos, end, true);
    }

    // =========================
    Vector3 GetTurnControlPoint(Vector3 start, Vector3 end, Direction prevDir, Direction moveDir, Direction nextDir)
    {
        Vector3 control = (start + end) * 0.5f;
        float curveAmount = gridManager.CellSize * TURN_CURVE_STRENGTH;

        if (IsCorner(prevDir, moveDir))
            control += DirectionToLocalVector(prevDir) * (curveAmount * 0.35f);

        if (IsCorner(moveDir, nextDir))
            control += DirectionToLocalVector(nextDir) * (curveAmount * 0.35f);

        return control;
    }

    // =========================
    bool IsCorner(Direction a, Direction b)
    {
        if (a == Direction.None || b == Direction.None) return false;
        if (a == b) return false;
        if (a == b.Opposite()) return false;
        return true;
    }

    // =========================
    Vector3 DirectionToLocalVector(Direction dir)
    {
        Vector2Int v = dir.ToVector();
        return new Vector3(v.x, 0f, v.y).normalized;
    }

    // =========================
    Vector3 EvaluateQuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    float EstimateQuadraticBezierLength(Vector3 a, Vector3 b, Vector3 c, int samples)
    {
        int safeSamples = Mathf.Max(2, samples);
        float length = 0f;
        Vector3 prev = a;

        for (int i = 1; i <= safeSamples; i++)
        {
            float t = i / (float)safeSamples;
            Vector3 p = EvaluateQuadraticBezier(a, b, c, t);
            length += Vector3.Distance(prev, p);
            prev = p;
        }

        return length;
    }

    // =========================
    void RotateBall(Vector3 from, Vector3 to)
    {
        Transform rollTarget = GetRollVisualTransform();
        if (rollTarget == null) return;

        Vector3 deltaLocal = to - from;
        Transform movementSpace = transform.parent;
        Vector3 upAxisWorld = movementSpace != null ? movementSpace.up : Vector3.up;
        Vector3 deltaWorld = movementSpace != null
            ? movementSpace.TransformVector(deltaLocal)
            : deltaLocal;
        deltaWorld = Vector3.ProjectOnPlane(deltaWorld, upAxisWorld);

        float distance = deltaWorld.magnitude;
        if (distance <= 0.00001f) return;

        float radius = GetTargetBallRadius();
        if (radius <= 0.00001f) return;

        Vector3 axis = Vector3.Cross(upAxisWorld, deltaWorld.normalized);
        if (axis.sqrMagnitude <= 0.0000001f) return;

        float angle = (distance / radius) * Mathf.Rad2Deg;

        rollTarget.rotation = Quaternion.AngleAxis(angle, axis.normalized) * rollTarget.rotation;
    }

    Transform GetRollVisualTransform()
    {
        if (spawnedSkinVisual != null)
            return spawnedSkinVisual.transform;

        return ballVisual;
    }

    Vector3 GetWorldDirectionFromGridDirection(Direction direction)
    {
        Vector3 localMoveDir = DirectionToLocalVector(direction);
        Transform movementSpace = transform.parent;
        Vector3 worldDir = movementSpace != null
            ? movementSpace.TransformDirection(localMoveDir)
            : localMoveDir;

        Vector3 upAxisWorld = movementSpace != null ? movementSpace.up : Vector3.up;
        worldDir = Vector3.ProjectOnPlane(worldDir, upAxisWorld);
        if (worldDir.sqrMagnitude <= 0.000001f)
            return Vector3.zero;

        return worldDir.normalized;
    }

    void AlignVisualYawTowards(Vector3 targetWorldDir, float t)
    {
        if (ballVisual == null || targetWorldDir.sqrMagnitude <= 0.000001f)
            return;

        Transform movementSpace = transform.parent;
        Vector3 upAxisWorld = movementSpace != null ? movementSpace.up : Vector3.up;

        Vector3 correctedTargetDir = Quaternion.AngleAxis(visualYawOffsetDegrees, upAxisWorld) * targetWorldDir.normalized;
        correctedTargetDir = Vector3.ProjectOnPlane(correctedTargetDir, upAxisWorld);
        if (correctedTargetDir.sqrMagnitude <= 0.000001f)
            return;
        correctedTargetDir.Normalize();

        Vector3 localForwardAxis = visualForwardAxisLocal.sqrMagnitude > 0.000001f
            ? visualForwardAxisLocal.normalized
            : Vector3.forward;
        Vector3 currentVisualForward = ballVisual.TransformDirection(localForwardAxis);
        currentVisualForward = Vector3.ProjectOnPlane(currentVisualForward, upAxisWorld);
        if (currentVisualForward.sqrMagnitude <= 0.000001f)
            return;
        currentVisualForward.Normalize();

        float angle = Vector3.SignedAngle(currentVisualForward, correctedTargetDir, upAxisWorld);
        float clampedT = Mathf.Clamp01(t);
        float stepAngle = angle * clampedT;
        ballVisual.Rotate(upAxisWorld, stepAngle, Space.World);
    }

    void ResetVisualOrientation(Direction initialDirection)
    {
        if (ballVisual == null || !resetVisualRotationOnLevelReset)
            return;

        ballVisual.localRotation = initialBallVisualLocalRotation;
        if (spawnedSkinVisual != null)
            spawnedSkinVisual.transform.localRotation = initialSpawnedSkinLocalRotation;

        if (!alignVisualToInitialMoveDirection || initialDirection == Direction.None)
            return;

        Vector3 targetWorldDir = GetWorldDirectionFromGridDirection(initialDirection);
        if (targetWorldDir.sqrMagnitude <= 0.000001f)
            return;

        AlignVisualYawTowards(targetWorldDir, 1f);
    }

    // =========================
    Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        float offsetX = (gridManager.width - 1) * 0.5f * gridManager.CellSize;
        float offsetZ = (gridManager.height - 1) * 0.5f * gridManager.CellSize;

        float halfBallHeight = GetCurrentBallWorldRadius();
        if (halfBallHeight <= 0.00001f)
            halfBallHeight = GetTargetBallRadius();

        float surfaceY = GetBallSurfaceY(gridPos);
        float y = surfaceY
            + halfBallHeight
            + GetEffectiveBallHeightOffset();

        return new Vector3(
            gridPos.x * gridManager.CellSize - offsetX,
            y,
            gridPos.y * gridManager.CellSize - offsetZ
        );
    }

    // =========================
    void UpdatePositionInstant()
    {
        transform.localPosition = GetWorldPosition(currentPos);
        UpdateTrailPlacementByCurrentDirection(true);
    }

    float GetBallSurfaceY(Vector2Int gridPos)
    {
        float baseSurfaceY = gridManager.GridTopY + gridManager.gridCellHeight;

        if (!autoPlaceBallOnTileSurface)
            return baseSurfaceY;

        Tile tile = gridManager.GetTileAt(gridPos);
        if (tile == null)
            return baseSurfaceY;

        Transform tileTransform = tile.transform;
        float tileTopY = tileTransform.localPosition.y + (tileTransform.localScale.y * 0.5f);
        return Mathf.Max(baseSurfaceY, tileTopY) + GetEffectiveBallSurfaceClearance();
    }

    float GetEffectiveBallSurfaceClearance()
    {
        if (!autoPlaceBallOnTileSurface)
            return ballSurfaceClearance;

        float maxAutoClearance = gridManager != null ? gridManager.CellSize * 0.003f : 0.001f;
        return Mathf.Min(ballSurfaceClearance, Mathf.Max(0f, maxAutoClearance));
    }

    float GetEffectiveBallHeightOffset()
    {
        if (!autoPlaceBallOnTileSurface || !limitLegacyHeightOffsetWhenAutoSurface)
            return ballHeightOffset;

        // Nilai lama sering dipakai untuk mengangkat bola dari grid dasar.
        // Saat surface tile sudah dihitung otomatis, offset besar akan membuat bola melayang.
        float maxAutoOffset = gridManager != null ? gridManager.CellSize * 0.01f : 0.002f;
        return Mathf.Min(ballHeightOffset, Mathf.Max(0f, maxAutoOffset));
    }

    // =========================
    void Lose(Vector2Int pos)
    {
        moving = false;
        SfxManager.Instance?.StopBallRolling();
        SetTrailActive(!trailOnlyWhileMoving, true);
        Debug.LogError($"[BALL LOSE] di {pos}");
        GameManager.Instance?.OnGameLose();
    }

    void Win()
    {
        moving = false;
        SfxManager.Instance?.StopBallRolling();
        SetTrailActive(!trailOnlyWhileMoving, true);
        Debug.Log("WIN");
        GameManager.Instance?.OnGameWin();
    }

    void ApplyTrailProfileForSkin(int skinIndex, bool refreshRuntimeEffect)
    {
        activeTrailSkinIndex = Mathf.Max(0, skinIndex);

        if (!refreshRuntimeEffect)
            return;

        if (spawnedTrailEffect == null)
            InitializeTrailEffect();

        ApplyTrailColorForSkin(activeTrailSkinIndex);
    }

    void InitializeTrailEffect()
    {
        if (!enableTrailEffect)
        {
            SetTrailActive(false, true);
            return;
        }

        if (activeTrailSkinIndex < 0)
            activeTrailSkinIndex = Mathf.Max(0, BallSkinStore.GetSelectedSkinIndex());

        isRuntimeSimpleTrail = true;
        Transform parent = GetTrailParent();
        CleanupRuntimeSimpleTrailDuplicates();

        if (spawnedTrailEffect == null)
            spawnedTrailEffect = CreateRuntimeSimpleTrail(parent);

        if (spawnedTrailEffect != null)
        {
            if (spawnedTrailEffect.transform.parent != parent)
                spawnedTrailEffect.transform.SetParent(parent, true);

            spawnedTrailEffect.transform.localPosition = trailEffectLocalOffset;
            spawnedTrailEffect.transform.localRotation = Quaternion.identity;
            spawnedTrailEffect.transform.localScale = Vector3.one;

            CacheTrailComponents(spawnedTrailEffect);
            ApplyTrailScale();
            ApplyTrailColorForSkin(activeTrailSkinIndex);
            UpdateTrailPlacementByCurrentDirection(true);
        }
        else
        {
            CacheTrailComponents(null);
        }

        SetTrailActive(!trailOnlyWhileMoving && enableTrailEffect, clearTrailOnReset);
    }

    Transform GetTrailParent()
    {
        if (keepSimpleTrailInWorldRoot)
            return null;

        if (trailEffectAnchor != null)
            return trailEffectAnchor;

        if (parentTrailToBallRoot)
            return transform;

        if (ballVisual != null)
            return ballVisual;

        return transform;
    }

    void CleanupRuntimeSimpleTrailDuplicates()
    {
        TrailRenderer[] childTrails = GetComponentsInChildren<TrailRenderer>(true);
        for (int i = childTrails.Length - 1; i >= 0; i--)
        {
            TrailRenderer trail = childTrails[i];
            if (trail == null)
                continue;

            GameObject trailObject = trail.gameObject;
            if (trailObject == null || trailObject == spawnedTrailEffect)
                continue;

            if (trailObject.name != RUNTIME_SIMPLE_TRAIL_NAME)
                continue;

            DestroyRuntimeObject(trailObject);
        }
    }

    void DestroyRuntimeObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    void CacheTrailComponents(GameObject root)
    {
        if (root == null)
        {
            cachedTrailRenderers = new TrailRenderer[0];
            return;
        }

        cachedTrailRenderers = root.GetComponentsInChildren<TrailRenderer>(true);
    }

    void SetTrailActive(bool active, bool clear)
    {
        if (!enableTrailEffect)
            active = false;

        if (spawnedTrailEffect == null && enableTrailEffect)
            InitializeTrailEffect();

        for (int i = 0; i < cachedTrailRenderers.Length; i++)
        {
            TrailRenderer tr = cachedTrailRenderers[i];
            if (tr == null)
                continue;

            bool layerActive = i == 0;
            tr.enabled = layerActive;
            tr.emitting = active && layerActive;
            if (clear || !layerActive)
                tr.Clear();
        }

        if (spawnedTrailEffect != null && cachedTrailRenderers.Length == 0)
            spawnedTrailEffect.SetActive(active);

        if (active)
            UpdateTrailPlacementByCurrentDirection(true);
    }

    void UpdateTrailRuntimeFromDelta(Vector3 fromLocal, Vector3 toLocal, bool forceUpdate)
    {
        if (spawnedTrailEffect == null)
            return;

        Transform movementSpace = transform.parent;
        Vector3 deltaLocal = toLocal - fromLocal;
        Vector3 upAxisWorld = movementSpace != null ? movementSpace.up : Vector3.up;
        Vector3 deltaWorld = movementSpace != null
            ? movementSpace.TransformVector(deltaLocal)
            : deltaLocal;

        deltaWorld = Vector3.ProjectOnPlane(deltaWorld, upAxisWorld);
        if (deltaWorld.sqrMagnitude > 0.000001f)
            lastTrailMoveDirectionWorld = deltaWorld.normalized;

        // Saat bola melewati tikungan, arah aktual frame ini bisa berbeda dari
        // currentDir yang baru diperbarui setelah coroutine selesai satu step.
        UpdateTrailPlacementWorld(forceUpdate);
    }

    void UpdateTrailPlacementByCurrentDirection(bool forceUpdate)
    {
        if (spawnedTrailEffect == null)
            return;

        Vector3 worldDirection = GetWorldDirectionFromGridDirection(currentDir);
        if (worldDirection.sqrMagnitude > 0.000001f)
            lastTrailMoveDirectionWorld = worldDirection;

        UpdateTrailPlacementWorld(forceUpdate);
    }

    void UpdateTrailPlacementWorld(bool forceUpdate)
    {
        if (spawnedTrailEffect == null)
            return;

        if (!positionTrailBehindBall && !forceUpdate)
            return;

        Transform movementSpace = transform.parent;
        Vector3 upAxisWorld = movementSpace != null ? movementSpace.up : Vector3.up;
        Vector3 moveDir = Vector3.ProjectOnPlane(lastTrailMoveDirectionWorld, upAxisWorld);
        if (moveDir.sqrMagnitude > 0.000001f)
            moveDir.Normalize();

        float ballRadius = GetCurrentBallWorldRadius();
        if (ballRadius <= 0.00001f)
            ballRadius = GetTargetBallRadius();
        Vector3 worldPos = transform.position;

        worldPos += upAxisWorld * (ballRadius * trailVerticalOffsetMultiplier);

        if (positionTrailBehindBall && moveDir.sqrMagnitude > 0.000001f)
            worldPos -= moveDir * (ballRadius * trailBehindDistanceMultiplier);

        Transform parent = spawnedTrailEffect.transform.parent;
        if (parent != null)
            worldPos += parent.TransformVector(trailEffectLocalOffset);
        else
            worldPos += trailEffectLocalOffset;

        spawnedTrailEffect.transform.position = worldPos;
    }

    void ApplyTrailScale()
    {
        if (spawnedTrailEffect == null)
            return;

        if (isRuntimeSimpleTrail)
        {
            ConfigureSimpleTrailRenderer(cachedTrailRenderers);
            spawnedTrailEffect.transform.localScale = Vector3.one;
            return;
        }

        float finalScale = Mathf.Max(0.001f, trailScaleMultiplier);

        if (autoScaleTrailToBall)
        {
            float targetDiameter = GetTargetBallDiameter() * Mathf.Max(0.05f, trailSizeRelativeToBall);
            float sourceDiameter = GetCombinedRendererDiameter(spawnedTrailEffect.transform);

            if (sourceDiameter > 0.00001f)
                finalScale = Mathf.Max(0.001f, (targetDiameter / sourceDiameter) * trailScaleMultiplier);
        }

        spawnedTrailEffect.transform.localScale = Vector3.one * finalScale;
    }

    GameObject CreateRuntimeSimpleTrail(Transform parent)
    {
        GameObject root = new GameObject(RUNTIME_SIMPLE_TRAIL_NAME);
        root.hideFlags = HideFlags.DontSave;
        if (parent != null)
            root.transform.SetParent(parent, false);

        root.transform.localPosition = trailEffectLocalOffset;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Material mat = ResolveSimpleTrailMaterial();

        TrailRenderer tr = root.AddComponent<TrailRenderer>();
        tr.autodestruct = false;
        tr.time = GetEffectiveTrailTime();
        tr.minVertexDistance = Mathf.Max(0.001f, simpleTrailMinVertexDistance);
        tr.alignment = simpleTrailAlignment;
        tr.textureMode = LineTextureMode.Stretch;
        tr.numCapVertices = Mathf.Clamp(simpleTrailCapVertices, 0, 16);
        tr.numCornerVertices = Mathf.Clamp(simpleTrailCornerVertices, 0, 16);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        tr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        tr.generateLightingData = false;
        tr.emitting = false;

        if (mat != null)
            tr.sharedMaterial = mat;

        return root;
    }

    void ConfigureSimpleTrailRenderer(TrailRenderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
            return;

        float currentDiameter = GetCurrentBallWorldDiameter();
        float targetDiameter = GetTargetBallDiameter();
        float diameter = ResolveSafeTrailDiameter(currentDiameter, targetDiameter);

        if (diameter <= 0.00001f)
            diameter = 0.2f;

        diameter *= Mathf.Max(0.05f, simpleTrailWidthDiameterMultiplier);

        float visibleBallDiameter = diameter;
        float widthScale = Mathf.Max(0.01f, trailScaleMultiplier);
        float startWidth = Mathf.Max(0.001f, diameter * simpleTrailStartWidthRelativeToBall * widthScale);
        float endWidth = Mathf.Max(0f, diameter * simpleTrailEndWidthRelativeToBall * widthScale);
        float maxStartWidth = Mathf.Max(0.001f, visibleBallDiameter * simpleTrailMaxStartWidthRelativeToCurrentBall);
        float maxEndWidth = Mathf.Max(0f, visibleBallDiameter * simpleTrailMaxEndWidthRelativeToCurrentBall);
        startWidth = Mathf.Min(startWidth, maxStartWidth);
        endWidth = Mathf.Min(endWidth, Mathf.Min(startWidth, maxEndWidth));

        for (int i = 0; i < renderers.Length; i++)
        {
            TrailRenderer tr = renderers[i];
            if (tr == null)
                continue;

            tr.time = GetEffectiveTrailTime();
            tr.minVertexDistance = Mathf.Max(0.001f, simpleTrailMinVertexDistance);
            tr.alignment = simpleTrailAlignment;
            tr.numCapVertices = Mathf.Clamp(simpleTrailCapVertices, 0, 16);
            tr.numCornerVertices = Mathf.Clamp(simpleTrailCornerVertices, 0, 16);
            tr.startWidth = startWidth;
            tr.endWidth = endWidth;
            tr.widthMultiplier = 1f;
        }
    }

    float ResolveSafeTrailDiameter(float currentDiameter, float targetDiameter)
    {
        bool hasCurrent = currentDiameter > 0.00001f;
        bool hasTarget = targetDiameter > 0.00001f;

        if (preferCurrentBallDiameterForTrailWidth && hasCurrent)
        {
            if (!hasTarget)
                return currentDiameter;

            // Some imported skin prefabs have oversized renderer bounds. Cap the
            // measured diameter against the gameplay target so the trail stays a tail.
            return Mathf.Min(currentDiameter, targetDiameter * 1.25f);
        }

        if (useTargetDiameterForSimpleTrailWidth && hasTarget)
        {
            if (!hasCurrent)
                return targetDiameter;

            return Mathf.Min(targetDiameter, currentDiameter * 1.25f);
        }

        if (hasCurrent && hasTarget)
            return Mathf.Min(currentDiameter, targetDiameter);

        if (hasCurrent)
            return currentDiameter;

        return hasTarget ? targetDiameter : 0f;
    }

    void ApplyTrailColorForSkin(int skinIndex)
    {
        if (cachedTrailRenderers == null || cachedTrailRenderers.Length == 0)
            return;

        bool useGradient;
        Color c1;
        Color c2;
        Color c3;
        ResolveTrailColorsForSkin(skinIndex, out useGradient, out c1, out c2, out c3);

        for (int i = 0; i < cachedTrailRenderers.Length; i++)
        {
            TrailRenderer tr = cachedTrailRenderers[i];
            if (tr == null)
                continue;

            bool layerActive = i == 0;
            tr.enabled = layerActive;
            tr.emitting = tr.emitting && layerActive;

            if (!layerActive)
            {
                tr.Clear();
                continue;
            }

            tr.widthMultiplier = 1f;
            tr.colorGradient = useGradient
                ? BuildThreeColorTrailGradient(c1, c2, c3)
                : BuildSingleColorTrailGradient(c1);
        }

        if (runtimeSimpleTrailMaterial != null)
        {
            Color materialTint = useGradient ? Color.white : c1;

            if (runtimeSimpleTrailMaterial.HasProperty("_Color"))
                runtimeSimpleTrailMaterial.SetColor("_Color", materialTint);
            if (runtimeSimpleTrailMaterial.HasProperty("_BaseColor"))
                runtimeSimpleTrailMaterial.SetColor("_BaseColor", materialTint);
        }
    }

    void ResolveTrailColorsForSkin(int skinIndex, out bool useGradient, out Color c1, out Color c2, out Color c3)
    {
        bool isSpecial = IsSpecialSkinIndex(skinIndex);
        useGradient = isSpecial;
        c1 = defaultBasicTrailColor;
        c2 = defaultBasicTrailColor;
        c3 = defaultBasicTrailColor;

        if (TryGetSkinTrailColorProfile(skinIndex, out SkinTrailColorProfile profile))
        {
            useGradient = perSkinProfileOverridesSpecialMode
                ? profile.useSpecialGradient
                : isSpecial || profile.useSpecialGradient;

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

    Gradient BuildSingleColorTrailGradient(Color color)
    {
        float endAlpha = forceTrailFadeOutAtEnd ? 0f : color.a;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(endAlpha, 1f),
            }
        );
        return gradient;
    }

    Gradient BuildThreeColorTrailGradient(Color colorA, Color colorB, Color colorC)
    {
        float endAlpha = forceTrailFadeOutAtEnd ? 0f : colorC.a;
        float middleTime = Mathf.Clamp(specialTrailMiddleColorTime, 0.05f, 0.6f);
        float endColorTime = Mathf.Clamp(specialTrailEndColorTime, middleTime + 0.05f, 0.9f);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorA, 0f),
                new GradientColorKey(colorB, middleTime),
                new GradientColorKey(colorC, endColorTime),
                new GradientColorKey(colorC, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(colorA.a, 0f),
                new GradientAlphaKey(colorB.a, middleTime),
                new GradientAlphaKey(colorC.a, endColorTime),
                new GradientAlphaKey(endAlpha, 1f),
            }
        );
        return gradient;
    }

    float GetEffectiveTrailTime()
    {
        float time = Mathf.Max(0.01f, simpleTrailTime);

        if (IsSpecialSkinIndex(activeTrailSkinIndex))
            time *= Mathf.Clamp(specialTrailTimeMultiplier, 0.1f, 1f);

        return Mathf.Max(0.01f, time);
    }

    bool IsSpecialSkinIndex(int skinIndex)
    {
        if (specialSkinIndices != null && specialSkinIndices.Length > 0)
        {
            for (int i = 0; i < specialSkinIndices.Length; i++)
            {
                if (specialSkinIndices[i] == skinIndex)
                    return true;
            }

            if (!combineSpecialStartIndexWithManualList)
                return false;
        }

        return skinIndex >= Mathf.Max(0, specialSkinStartIndex);
    }

    bool TryGetSkinTrailColorProfile(int skinIndex, out SkinTrailColorProfile profile)
    {
        profile = null;
        if (!usePerSkinTrailColorProfiles || skinTrailColorProfiles == null || skinTrailColorProfiles.Length == 0)
            return false;

        for (int i = 0; i < skinTrailColorProfiles.Length; i++)
        {
            SkinTrailColorProfile p = skinTrailColorProfiles[i];
            if (p == null)
                continue;

            if (p.skinIndex != skinIndex)
                continue;

            profile = p;
            return true;
        }

        return false;
    }

    bool TryGetSkinPrefabTransformProfile(int skinIndex, out SkinPrefabTransformProfile profile)
    {
        profile = null;
        if (!usePerSkinPrefabTransformProfiles || skinPrefabTransformProfiles == null || skinPrefabTransformProfiles.Length == 0)
            return false;

        for (int i = 0; i < skinPrefabTransformProfiles.Length; i++)
        {
            SkinPrefabTransformProfile p = skinPrefabTransformProfiles[i];
            if (p == null)
                continue;

            if (p.skinIndex != skinIndex)
                continue;

            profile = p;
            return true;
        }

        return false;
    }

    Material ResolveSimpleTrailMaterial()
    {
        if (!forceUnlitColorTrailMaterial && simpleTrailMaterial != null)
        {
            if (runtimeSimpleTrailMaterial == null || runtimeSimpleTrailMaterial.shader != simpleTrailMaterial.shader)
            {
                runtimeSimpleTrailMaterial = new Material(simpleTrailMaterial);
                ConfigureTrailMaterialForTransparency(runtimeSimpleTrailMaterial);
            }

            if (runtimeSimpleTrailMaterial.HasProperty("_Color"))
                runtimeSimpleTrailMaterial.SetColor("_Color", defaultBasicTrailColor);
            if (runtimeSimpleTrailMaterial.HasProperty("_BaseColor"))
                runtimeSimpleTrailMaterial.SetColor("_BaseColor", defaultBasicTrailColor);

            return runtimeSimpleTrailMaterial;
        }

        if (runtimeSimpleTrailMaterial != null)
            return runtimeSimpleTrailMaterial;

        string[] candidateShaders = new string[]
        {
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Additive",
            "Mobile/Particles/Additive",
            "Sprites/Default"
        };

        Shader chosen = null;
        for (int i = 0; i < candidateShaders.Length; i++)
        {
            Shader shader = Shader.Find(candidateShaders[i]);
            if (shader != null && shader.isSupported)
            {
                chosen = shader;
                break;
            }
        }

        if (chosen == null)
            return null;

        runtimeSimpleTrailMaterial = new Material(chosen);
        ConfigureTrailMaterialForTransparency(runtimeSimpleTrailMaterial);
        if (runtimeSimpleTrailMaterial.HasProperty("_Color"))
            runtimeSimpleTrailMaterial.SetColor("_Color", defaultBasicTrailColor);
        if (runtimeSimpleTrailMaterial.HasProperty("_BaseColor"))
            runtimeSimpleTrailMaterial.SetColor("_BaseColor", defaultBasicTrailColor);

        return runtimeSimpleTrailMaterial;
    }

    void ConfigureTrailMaterialForTransparency(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f); // transparent (URP style)

        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f); // fade (legacy standard style)

        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.renderQueue = 3000;
    }

    float GetCombinedRendererDiameter(Transform root)
    {
        if (root == null)
            return 1f;

        if (!TryGetCombinedRendererBounds(root, out Bounds localBounds))
            return 1f;

        return Mathf.Max(0.0001f, GetLargestAxis(localBounds.size));
    }

    float GetTargetBallDiameter()
    {
        if (gridManager == null)
            return 0.2f;

        return gridManager.CellSize * Mathf.Max(0.1f, ballSizeMultiplier);
    }

    float GetTargetBallRadius()
    {
        return GetTargetBallDiameter() * 0.5f;
    }

    float GetCurrentBallWorldRadius()
    {
        return GetCurrentBallWorldDiameter() * 0.5f;
    }

    float GetCurrentBallWorldDiameter()
    {
        Transform visualRoot = GetRollVisualTransform();
        if (visualRoot == null)
            visualRoot = ballVisual != null ? ballVisual : transform;

        if (!TryGetCombinedWorldRendererBounds(visualRoot, out Bounds worldBounds))
            return 0f;

        return Mathf.Max(0.0001f, GetLargestAxis(worldBounds.size));
    }

    bool TryGetCombinedWorldRendererBounds(Transform root, out Bounds combined)
    {
        combined = default;
        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            if (!found)
            {
                combined = r.bounds;
                found = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    float GetLargestAxis(Vector3 size)
    {
        return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
    }

    float GetBaseVisualDiameterCached()
    {
        if (cachedDiameterSource != ballVisual)
        {
            cachedBaseVisualDiameter = -1f;
            cachedDiameterSource = ballVisual;
        }

        if (cachedBaseVisualDiameter > 0.00001f)
            return cachedBaseVisualDiameter;

        if (!TryGetCombinedRendererBounds(ballVisual, out Bounds localBounds))
            return 1f;

        cachedBaseVisualDiameter = GetLargestAxis(localBounds.size);
        if (cachedBaseVisualDiameter <= 0.00001f)
            cachedBaseVisualDiameter = 1f;

        return cachedBaseVisualDiameter;
    }

    void NormalizeSpawnedSkinVisual(Transform skinRoot, float diameterMultiplier)
    {
        if (skinRoot == null)
            return;

        if (!TryGetCombinedRendererBounds(skinRoot, out Bounds skinBounds))
            return;

        float sourceDiameter = GetLargestAxis(skinBounds.size);
        float targetDiameter = GetTargetBallDiameter() * Mathf.Max(0.0001f, diameterMultiplier);

        if (sourceDiameter <= 0.00001f || targetDiameter <= 0.00001f)
            return;

        float ratio = targetDiameter / sourceDiameter;
        skinRoot.localScale = Vector3.one * ratio;

        if (!autoCenterSkinPrefabOnBall)
            return;

        if (!TryGetCombinedRendererBounds(skinRoot, out Bounds centeredBounds))
            return;

        Vector3 center = centeredBounds.center;
        skinRoot.localPosition = -center;
    }

    bool TryGetCombinedRendererBounds(Transform root, out Bounds localBounds)
    {
        localBounds = default;
        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasAny = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds worldBounds = renderer.bounds;
            Vector3 worldCenter = worldBounds.center;
            Vector3 ext = worldBounds.extents;

            Vector3[] corners = new Vector3[8]
            {
                worldCenter + new Vector3(-ext.x, -ext.y, -ext.z),
                worldCenter + new Vector3(-ext.x, -ext.y, ext.z),
                worldCenter + new Vector3(-ext.x, ext.y, -ext.z),
                worldCenter + new Vector3(-ext.x, ext.y, ext.z),
                worldCenter + new Vector3(ext.x, -ext.y, -ext.z),
                worldCenter + new Vector3(ext.x, -ext.y, ext.z),
                worldCenter + new Vector3(ext.x, ext.y, -ext.z),
                worldCenter + new Vector3(ext.x, ext.y, ext.z),
            };

            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localPoint = root.InverseTransformPoint(corners[c]);

                if (!hasAny)
                {
                    min = localPoint;
                    max = localPoint;
                    hasAny = true;
                }
                else
                {
                    min = Vector3.Min(min, localPoint);
                    max = Vector3.Max(max, localPoint);
                }
            }
        }

        if (!hasAny)
            return false;

        localBounds = new Bounds((min + max) * 0.5f, max - min);
        return true;
    }
}
