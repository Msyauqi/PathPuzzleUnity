using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using PathPuzzle;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 8;
    public int height = 6;
    public float cellSize = 0.5f;

    [Header("Grid Visual")]
    public GameObject gridCellPrefab;
    public float cellGap = 0.01f;
    public float gridThickness = 0.004f;
    public float gridYOffset = -0.002f;

    [Header("Tile Height")]
    public float gridCellHeight = 0.01f;

    [Header("Path Readability")]
    public bool addPathVisualAids = true;
    public bool showPathCellBorders = true;
    public bool showPathTypeLabels = true;
    [Range(0.001f, 0.08f)] public float pathBorderThickness = 0.025f;
    [Range(0f, 0.25f)] public float pathBorderInset = 0.045f;
    public float pathBorderLocalY = 0.56f;
    public float pathLabelLocalY = 0.64f;
    public float pathLabelCharacterSize = 0.13f;
    public float pathLabelZOffset = -0.28f;
    public Color straightPathAidColor = new Color(0.15f, 0.75f, 1f, 1f);
    public Color cornerPathAidColor = new Color(1f, 0.78f, 0.12f, 1f);
    public Color startPathAidColor = new Color(0.2f, 1f, 0.25f, 1f);
    public Color finishPathAidColor = new Color(1f, 0.18f, 0.18f, 1f);
    public Color pathAidLabelColor = Color.white;

    [Header("Arena Spawn Animation")]
    public bool playArenaSpawnAnimation = true;
    public bool animateGridCellsOnSpawn = true;
    public bool animateTilesOnSpawn = true;
    public float gridCellSpawnDuration = 0.18f;
    public float gridCellStepDelay = 0.012f;
    public float tileSpawnDuration = 0.26f;
    public float tileStepDelay = 0.035f;
    [Range(0f, 1f)] public float spawnStartScale = 0.05f;
    public float tileSpawnLift = 0.035f;
    public bool spawnFromCenter = true;
    public AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Data")]
    public PathPrefabDatabase prefabDatabase;
    public List<LevelData> allLevels;

    [Header("Runtime")]
    public int currentLevelIndex = 0;
    public int requestedLevelIndex = 0;
    public LevelData currentLevel;

    private Tile[,] grid;
    private GameObject container;
    private Coroutine arenaSpawnCoroutine;
    private readonly List<Transform> spawnedGridVisuals = new List<Transform>();
    private readonly List<Transform> spawnedTileVisuals = new List<Transform>();
    private readonly List<PathTileLayoutState> initialPathLayout = new List<PathTileLayoutState>();

    public float CellSize => cellSize;
    public float GridTopY => gridYOffset + (gridThickness * 0.5f);

    [SerializeField] private int currentMoveCount = 0;
public int CurrentMoveCount => currentMoveCount;

    private class PathTileLayoutState
    {
        public PathTile tile;
        public Vector2Int gridPosition;
        public int rotationIndex;
        public bool isMovable;
    }

    // =========================
    public void InitializeGrid(float newCellSize, int w, int h)
    {
        cellSize = newCellSize;
        width = Mathf.Max(1, w);
        height = Mathf.Max(1, h);

        LoadSelectedLevelFromPrefs();
        LoadLevel(currentLevelIndex);
    }

    // =========================
    void LoadSelectedLevelFromPrefs()
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            currentLevelIndex = 0;
            return;
        }

        int savedIndex = PlayerPrefs.GetInt(LevelProgress.SelectedLevelKey, 0);
        bool bypassLock = LevelProgress.IsSelectedLevelLockBypassed();

        if (!LevelProgress.CanPlayLevel(savedIndex, bypassLock))
        {
            Debug.Log($"[GRID] SelectedLevelIndex {savedIndex} masih locked. Fallback ke level 0.");
            savedIndex = 0;
            LevelProgress.ClearSelectedLevelLockBypass();
        }

        requestedLevelIndex = Mathf.Max(0, savedIndex);
        currentLevelIndex = Mathf.Clamp(savedIndex, 0, allLevels.Count - 1);

        Debug.Log($"[GRID] SelectedLevelIndex requested={requestedLevelIndex}, loaded={currentLevelIndex}");
    }

    // =========================
    public void LoadLevel(int index)
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogError("[GRID] Level kosong!");
            return;
        }

        bool bypassLock = LevelProgress.IsSelectedLevelLockBypassed();

        if (!LevelProgress.CanPlayLevel(index, bypassLock))
        {
            Debug.Log($"[GRID] Level {index} masih locked. Fallback ke level 0.");
            index = 0;
            LevelProgress.ClearSelectedLevelLockBypass();
        }

        requestedLevelIndex = Mathf.Max(0, index);
        int clampedIndex = Mathf.Clamp(index, 0, allLevels.Count - 1);

        ClearAll();

        currentLevelIndex = clampedIndex;
        currentLevel = allLevels[clampedIndex];

        CreateGrid();

        SpawnTile(new PathLevelData(
            PathType.Start,
            TileSize.Size1x1,
            currentLevel.startPosition,
            currentLevel.startRotation,
            0,
            false
        ));

        SpawnTile(new PathLevelData(
            PathType.Finish,
            TileSize.Size1x1,
            currentLevel.finishPosition,
            currentLevel.finishRotation,
            0,
            false
        ));

        if (currentLevel.paths != null)
        {
            foreach (var p in currentLevel.paths)
            {
                if (p == null) continue;
                SpawnTile(p);
            }
        }

        CaptureInitialPathLayout();

        PlayArenaSpawnAnimationIfNeeded();
        GameplayVisualBrightness.RequestRefreshAll();
        GameManager.Instance?.OnGridReady();
    }

    // =========================
    void SpawnTile(PathLevelData data)
    {
        GameObject prefab = prefabDatabase.GetPrefab(data.pathType, data.skinIndex);

        if (prefab == null)
        {
            Debug.LogError($"[SPAWN FAIL] Prefab null {data.pathType}");
            return;
        }

        GameObject obj = Instantiate(prefab, container.transform);

        obj.transform.localScale = new Vector3(
            cellSize,
            obj.transform.localScale.y,
            cellSize
        );

        PathTile tile = obj.GetComponent<PathTile>();

        if (tile == null)
        {
            Debug.LogError("[SPAWN FAIL] PathTile missing");
            Destroy(obj);
            return;
        }

        tile.SetGridManager(this);
        tile.Initialize(data.position);

        tile.pathType = data.pathType;
        tile.isMovable = IsStaticPathType(data.pathType) ? false : data.isMovable;

        for (int i = 0; i < data.rotation; i++)
        tile.RotateClockwise(true);


        if (!CanPlaceTile(tile))
        {
            Debug.LogError($"[SPAWN FAIL] posisi tidak valid {data.position}");
            Destroy(obj);
            return;
        }

        RegisterTile(tile);
        ApplyPathVisualAid(tile);
        spawnedTileVisuals.Add(obj.transform);
        Debug.Log($"[SPAWN] {data.pathType} pos={data.position} rot={data.rotation} movable={tile.isMovable}");

        var cells = tile.GetOccupiedCells();
        Debug.Log($"[REGISTER] {tile.name} pos={data.position} rot={data.rotation} cells={string.Join(",", cells)}");
    }

    // =========================
    void ApplyPathVisualAid(PathTile tile)
    {
        if (!addPathVisualAids || tile == null)
            return;

        PathTileVisualAid aid = tile.GetComponent<PathTileVisualAid>();
        if (aid == null)
            aid = tile.gameObject.AddComponent<PathTileVisualAid>();

        aid.showCellBorders = showPathCellBorders;
        aid.showTypeLabel = showPathTypeLabels;
        aid.borderThickness = pathBorderThickness;
        aid.borderInset = pathBorderInset;
        aid.borderLocalY = pathBorderLocalY;
        aid.labelLocalY = pathLabelLocalY;
        aid.labelCharacterSize = pathLabelCharacterSize;
        aid.labelZOffset = pathLabelZOffset;
        aid.straightColor = straightPathAidColor;
        aid.cornerColor = cornerPathAidColor;
        aid.startColor = startPathAidColor;
        aid.finishColor = finishPathAidColor;
        aid.labelColor = pathAidLabelColor;
        aid.Build(tile);
    }

    // =========================
    bool IsStaticPathType(PathType type)
    {
        return type == PathType.Wall ||
               type == PathType.Start ||
               type == PathType.Finish;
    }

    // =========================
    public bool CanPlaceTile(PathTile tile)
    {
        foreach (var cell in tile.GetOccupiedCells())
        {
            if (!IsValidPosition(cell))
                return false;

            Tile existing = grid[cell.x, cell.y];

            if (existing == null || existing == tile)
                continue;

            return false;
        }

        return true;
    }

    // =========================
    public void RegisterTile(PathTile tile)
    {
        foreach (var cell in tile.GetOccupiedCells())
        {
            if (!IsValidPosition(cell))
            {
                Debug.LogError($"[REGISTER ERROR] invalid {cell}");
                continue;
            }

            grid[cell.x, cell.y] = tile;
        }
    }

    // =========================
    public void ClearTile(PathTile tile)
    {
        foreach (var cell in tile.GetOccupiedCells())
        {
            if (IsValidPosition(cell) && grid[cell.x, cell.y] == tile)
                grid[cell.x, cell.y] = null;
        }
    }

    // =========================
    public Tile GetTileAt(Vector2Int pos)
    {
        if (!IsValidPosition(pos)) return null;
        return grid[pos.x, pos.y];
    }

    // =========================
    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    // =========================
    void CreateGrid()
    {
        grid = new Tile[width, height];

        float offsetX = (width - 1) * 0.5f * cellSize;
        float offsetZ = (height - 1) * 0.5f * cellSize;
        float visualSize = Mathf.Max(0.001f, cellSize - cellGap);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (gridCellPrefab == null) continue;

                GameObject cell = Instantiate(gridCellPrefab, container.transform);
                cell.name = $"Cell_{x}_{z}";

                cell.transform.localPosition = new Vector3(
                    x * cellSize - offsetX,
                    gridYOffset,
                    z * cellSize - offsetZ
                );

                cell.transform.localRotation = Quaternion.identity;
                cell.transform.localScale = new Vector3(
                    visualSize,
                    gridThickness,
                    visualSize
                );

                Collider col = cell.GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;

                spawnedGridVisuals.Add(cell.transform);
            }
        }

        GameplayVisualBrightness.RequestRefreshAll();
    }

    // =========================
    void ClearAll()
    {
        if (container != null)
            Destroy(container.gameObject);

        container = new GameObject("GridContainer");
        container.transform.SetParent(transform, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        grid = new Tile[width, height];
        spawnedGridVisuals.Clear();
        spawnedTileVisuals.Clear();
        initialPathLayout.Clear();

        if (arenaSpawnCoroutine != null)
        {
            StopCoroutine(arenaSpawnCoroutine);
            arenaSpawnCoroutine = null;
        }
    }

    void PlayArenaSpawnAnimationIfNeeded()
    {
        if (!playArenaSpawnAnimation)
            return;

        if (arenaSpawnCoroutine != null)
            StopCoroutine(arenaSpawnCoroutine);

        arenaSpawnCoroutine = StartCoroutine(AnimateArenaSpawn());
    }

    IEnumerator AnimateArenaSpawn()
    {
        List<Transform> gridTargets = CopyValidTransforms(spawnedGridVisuals);
        List<Transform> tileTargets = CopyValidTransforms(spawnedTileVisuals);

        SortSpawnTargets(gridTargets);
        SortSpawnTargets(tileTargets);

        if (animateGridCellsOnSpawn)
            PrepareSpawnTargets(gridTargets, false);

        if (animateTilesOnSpawn)
            PrepareSpawnTargets(tileTargets, true);

        if (animateGridCellsOnSpawn)
            yield return AnimateSpawnTargets(gridTargets, gridCellSpawnDuration, gridCellStepDelay, false);

        if (animateTilesOnSpawn)
            yield return AnimateSpawnTargets(tileTargets, tileSpawnDuration, tileStepDelay, true);

        arenaSpawnCoroutine = null;
        GameManager.Instance?.RefreshBallStartPositionAfterArenaSpawn();
    }

    List<Transform> CopyValidTransforms(List<Transform> source)
    {
        List<Transform> result = new List<Transform>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                result.Add(source[i]);
        }

        return result;
    }

    void SortSpawnTargets(List<Transform> targets)
    {
        if (!spawnFromCenter || targets == null)
            return;

        targets.Sort((a, b) =>
        {
            float da = new Vector2(a.localPosition.x, a.localPosition.z).sqrMagnitude;
            float db = new Vector2(b.localPosition.x, b.localPosition.z).sqrMagnitude;
            return da.CompareTo(db);
        });
    }

    void PrepareSpawnTargets(List<Transform> targets, bool liftUp)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null)
                continue;

            Vector3 baseScale = target.localScale;
            Vector3 basePosition = target.localPosition;
            target.localScale = baseScale * GetSafeSpawnStartScale();

            if (liftUp)
                target.localPosition = basePosition + Vector3.up * tileSpawnLift;
        }
    }

    IEnumerator AnimateSpawnTargets(List<Transform> targets, float duration, float stepDelay, bool dropDown)
    {
        if (targets == null || targets.Count == 0)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float safeStepDelay = Mathf.Max(0f, stepDelay);

        List<Vector3> baseScales = new List<Vector3>();
        List<Vector3> basePositions = new List<Vector3>();
        List<float> startTimes = new List<float>();

        float startTime = Time.time;
        float safeStartScale = GetSafeSpawnStartScale();
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            baseScales.Add(target != null ? target.localScale / safeStartScale : Vector3.one);
            basePositions.Add(target != null ? target.localPosition - (dropDown ? Vector3.up * tileSpawnLift : Vector3.zero) : Vector3.zero);
            startTimes.Add(startTime + (i * safeStepDelay));
        }

        float totalDuration = safeDuration + (Mathf.Max(0, targets.Count - 1) * safeStepDelay);
        while (Time.time - startTime < totalDuration)
        {
            float now = Time.time;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null)
                    continue;

                float t = Mathf.Clamp01((now - startTimes[i]) / safeDuration);
                float eased = spawnScaleCurve != null ? spawnScaleCurve.Evaluate(t) : t;
                Vector3 baseScale = baseScales[i];
                Vector3 basePosition = basePositions[i];

                target.localScale = Vector3.LerpUnclamped(baseScale * safeStartScale, baseScale, eased);

                if (dropDown)
                    target.localPosition = Vector3.LerpUnclamped(basePosition + Vector3.up * tileSpawnLift, basePosition, eased);
            }

            yield return null;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null)
                continue;

            target.localScale = baseScales[i];
            if (dropDown)
                target.localPosition = basePositions[i];
        }
    }

    float GetSafeSpawnStartScale()
    {
        return Mathf.Clamp(spawnStartScale, 0.001f, 1f);
    }

    // =========================
    public void ResetLevel()
    {
        Debug.Log($"[GRID] Reset Level {currentLevelIndex}");
        LoadLevel(currentLevelIndex);
    }

    public void ResetPathLayoutOnly()
    {
        if (initialPathLayout.Count == 0)
        {
            Debug.LogWarning("[GRID] Tidak ada data layout awal path untuk di-reset.");
            return;
        }

        grid = new Tile[width, height];

        foreach (PathTileLayoutState state in initialPathLayout)
        {
            if (state == null || state.tile == null)
                continue;

            PathTile tile = state.tile;
            tile.SetGridManager(this);
            tile.SetSelected(false);
            tile.SetInvalid(false);
            tile.isMovable = state.isMovable;
            tile.gridPosition = state.gridPosition;
            tile.SetRotationIndex(state.rotationIndex, true);
            tile.UpdatePosition();
        }

        foreach (PathTileLayoutState state in initialPathLayout)
        {
            if (state == null || state.tile == null)
                continue;

            RegisterTile(state.tile);
        }

        GameplayVisualBrightness.RequestRefreshAll();
        Debug.Log("[GRID] Path layout reset tanpa reset timer/moves.");
    }

    void CaptureInitialPathLayout()
    {
        initialPathLayout.Clear();

        if (container == null)
            return;

        PathTile[] tiles = container.GetComponentsInChildren<PathTile>(true);

        foreach (PathTile tile in tiles)
        {
            if (tile == null)
                continue;

            initialPathLayout.Add(new PathTileLayoutState
            {
                tile = tile,
                gridPosition = tile.gridPosition,
                rotationIndex = tile.RotationIndex,
                isMovable = tile.isMovable
            });
        }
    }

    // =========================
    public void LoadNextLevel()
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= allLevels.Count)
            nextIndex = 0;

        LevelProgress.SelectLevelForPlay(nextIndex, LevelProgress.IsSelectedLevelLockBypassed());

        LoadLevel(nextIndex);
    }

    // =========================
    public void LoadPreviousLevel()
    {
        if (allLevels == null || allLevels.Count == 0)
            return;

        int prevIndex = currentLevelIndex - 1;
        if (prevIndex < 0)
            prevIndex = allLevels.Count - 1;

        LevelProgress.SelectLevelForPlay(prevIndex, LevelProgress.IsSelectedLevelLockBypassed());

        LoadLevel(prevIndex);
    }

    // =========================
    public string GetCurrentLevelName()
    {
        if (currentLevel == null) return "Level";

        return string.IsNullOrWhiteSpace(currentLevel.levelName)
            ? $"Level {currentLevel.levelNumber}"
            : currentLevel.levelName;
    }

    // =========================
    public int GetCurrentTargetMoves()
    {
        return currentLevel != null ? currentLevel.targetMoves : 0;
    }

    // =========================
    public Sprite GetCurrentLevelPreview()
    {
        return currentLevel != null ? currentLevel.levelPreview : null;
    }

    // =========================
    public void SwapTiles(PathTile a, PathTile b)
    {
        if (a == null || b == null) return;

        Vector2Int posA = a.gridPosition;
        Vector2Int posB = b.gridPosition;

        ClearTile(a);
        ClearTile(b);

        a.gridPosition = posB;
        b.gridPosition = posA;

        if (!CanPlaceTile(a) || !CanPlaceTile(b))
        {
            Debug.LogWarning("[SWAP FAIL] tidak valid");

            a.gridPosition = posA;
            b.gridPosition = posB;

            RegisterTile(a);
            RegisterTile(b);
            return;
        }

        RegisterTile(a);
        RegisterTile(b);

        a.UpdatePosition();
        b.UpdatePosition();

        Debug.Log($"[SWAP] {a.pathType} <-> {b.pathType}");
    }

    private void ResetMoveCount()
{
    currentMoveCount = 0;
}

private void AddMove()
{
    currentMoveCount++;
    Debug.Log($"[MOVE COUNT] {currentMoveCount}");
}

}
