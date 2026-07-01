using System;
using System.Collections.Generic;
using PathPuzzle;
using UnityEngine;

public class DeveloperCheats : MonoBehaviour
{
    [Serializable]
    public class AutoSolveStep
    {
        [Tooltip("Opsional. Kalau kosong, tile dicari dari Path Type + Tile Order.")]
        public PathTile tile;

        [Tooltip("Dipakai kalau Tile kosong.")]
        public PathType pathType = PathType.Straight;

        [Tooltip("Aktifkan supaya tile dibedakan juga berdasarkan ukuran, misalnya Straight 1x1 dan Straight 1x2.")]
        public bool matchTileSize = true;

        [Tooltip("Dipakai kalau Match Tile Size aktif.")]
        public TileSize tileSize = TileSize.Size1x1;

        [Tooltip("Urutan tile dengan Path Type yang sama. Urutan: atas ke bawah, kiri ke kanan.")]
        public int tileOrder = 0;

        public Vector2Int targetGridPosition;

        [Range(0, 3)]
        public int targetRotation;
    }

    [Serializable]
    public class LevelAutoSolveSolution
    {
        public int levelIndex;
        public AutoSolveStep[] steps;
    }

    class TileSnapshot
    {
        public PathTile tile;
        public Vector2Int position;
        public int rotation;
    }

    [Header("References")]
    public GameManager gameManager;
    public GridManager gridManager;
    public BallSkinShopUI shopUI;

    [Header("Coin Cheats")]
    public int addCoinAmount = 100;
    public int resetCoinValue = 0;

    [Header("Level Cheats")]
    public int maxLevelIndex = 20;
    [Range(1, 3)] public int unlockStars = 1;
    public bool refreshLevelButtonsAfterCheat = true;

    [Header("Skin Cheats")]
    public int maxSkinIndex = 20;
    public bool resetSelectedSkinWhenResetShop = true;
    public bool refreshShopAfterCheat = true;

    [Header("Reset All Gameplay")]
    public bool resetCoinOnResetAll = true;
    public bool resetLevelProgressOnResetAll = true;
    public bool resetSkinShopOnResetAll = true;
    public bool resetTutorialOnResetAll = true;
    public bool resetSelectedLevelOnResetAll = true;
    public bool resetAudioSettingsOnResetAll = false;

    [Header("Timer Cheat")]
    public float timerSeconds = 60f;
    public bool startTimerAfterSet = true;

    [Header("Auto Solve Path (Simple)")]
    public bool autoStartSimulationAfterSolve = false;
    public bool countAutoSolveAsOneMove = false;
    public bool includeStaticTilesWhenResolvingByOrder = false;
    public LevelAutoSolveSolution[] autoSolveSolutions;

    void Awake()
    {
        AutoResolveReferences();
    }

    void AutoResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>(true);

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>(true);

        if (shopUI == null)
            shopUI = FindObjectOfType<BallSkinShopUI>(true);
    }

    [ContextMenu("Cheat/Add Coin")]
    public void AddCoin()
    {
        CoinWallet.AddCoins(addCoinAmount);
        Debug.Log($"[CHEAT] Add coin +{addCoinAmount}. Total={CoinWallet.GetCoins()}");
    }

    [ContextMenu("Cheat/Reset Coin")]
    public void ResetCoin()
    {
        CoinWallet.SetCoins(resetCoinValue);
        Debug.Log($"[CHEAT] Reset coin ke {resetCoinValue}.");
    }

    [ContextMenu("Cheat/Unlock All Level")]
    public void UnlockAllLevel()
    {
        int safeMax = Mathf.Max(0, maxLevelIndex);
        int safeStars = Mathf.Clamp(unlockStars, 1, 3);

        for (int i = 0; i <= safeMax; i++)
            LevelProgress.SetStarsForTesting(i, safeStars);

        LevelProgress.ClearSelectedLevelLockBypass();
        RefreshLevelButtons();
        Debug.Log($"[CHEAT] Unlock all level 0..{safeMax} dengan {safeStars} star.");
    }

    [ContextMenu("Cheat/Lock All Level")]
    public void LockAllLevel()
    {
        int safeMax = Mathf.Max(0, maxLevelIndex);
        LevelProgress.ResetAllStarsForTesting(safeMax);
        PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelKey);
        PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelBypassLockKey);
        PlayerPrefs.Save();

        RefreshLevelButtons();
        Debug.Log($"[CHEAT] Lock ulang semua level. Level 0 tetap terbuka otomatis.");
    }

    [ContextMenu("Cheat/Unlock All Skins")]
    public void UnlockAllSkins()
    {
        int safeMax = Mathf.Max(0, maxSkinIndex);

        for (int i = 1; i <= safeMax; i++)
            BallSkinStore.Unlock(i);

        RefreshShop();
        Debug.Log($"[CHEAT] Unlock all skin 0..{safeMax}.");
    }

    [ContextMenu("Cheat/Reset Skin Shop")]
    public void ResetSkinShop()
    {
        BallSkinStore.ResetUnlockedSkins(maxSkinIndex, resetSelectedSkinWhenResetShop);
        RefreshRuntimeBallSkin();
        RefreshShop();
        Debug.Log($"[CHEAT] Reset skin shop. Skin default tetap free.");
    }

    [ContextMenu("Cheat/Reset All Gameplay")]
    public void ResetAllGameplay()
    {
        AutoResolveReferences();

        if (resetLevelProgressOnResetAll)
        {
            int safeMaxLevel = Mathf.Max(Mathf.Max(0, maxLevelIndex), 99);
            LevelProgress.ResetAllStarsForTesting(safeMaxLevel);
        }

        if (resetSelectedLevelOnResetAll)
        {
            PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelKey);
            PlayerPrefs.DeleteKey(LevelProgress.SelectedLevelBypassLockKey);
        }

        if (resetCoinOnResetAll)
            CoinWallet.SetCoins(resetCoinValue);

        if (resetSkinShopOnResetAll)
        {
            int safeMaxSkin = Mathf.Max(Mathf.Max(0, maxSkinIndex), 99);
            BallSkinStore.ResetUnlockedSkins(safeMaxSkin, true);
            RefreshRuntimeBallSkin();
        }

        if (resetTutorialOnResetAll)
            LevelZeroTutorial.ResetTutorialProgress();

        if (resetAudioSettingsOnResetAll)
        {
            PlayerPrefs.DeleteKey("MusicVolume");
            PlayerPrefs.DeleteKey("SfxVolume");
        }

        PlayerPrefs.Save();
        RefreshLevelButtons();
        RefreshShop();

        Debug.Log("[CHEAT] Reset all gameplay selesai. Level 0 tetap terbuka, coin/skin/tutorial kembali seperti awal.");
    }

    [ContextMenu("Cheat/Set Timer")]
    public void SetTimer()
    {
        AutoResolveReferences();

        if (gameManager == null)
        {
            Debug.LogWarning("[CHEAT] GameManager tidak ditemukan, timer tidak bisa di-set.");
            return;
        }

        gameManager.SetRemainingTimeForTesting(timerSeconds, startTimerAfterSet);
        Debug.Log($"[CHEAT] Timer diset ke {timerSeconds:0.##} detik.");
    }

    [ContextMenu("Cheat/Auto Solve Path")]
    public void AutoSolvePath()
    {
        AutoResolveReferences();

        if (gridManager == null)
        {
            Debug.LogWarning("[CHEAT] GridManager tidak ditemukan, Auto Solve dibatalkan.");
            return;
        }

        LevelAutoSolveSolution solution = FindCurrentLevelSolution();
        if (solution == null)
        {
            Debug.LogWarning($"[CHEAT] Belum ada Auto Solve Solution untuk level {GetCurrentLevelIndex()}.");
            return;
        }

        if (!ApplyAutoSolveSolution(solution))
            return;

        if (countAutoSolveAsOneMove && gameManager != null)
            gameManager.IncrementMoveCount();

        if (autoStartSimulationAfterSolve && gameManager != null)
            gameManager.StartSimulation();
    }

    [ContextMenu("Cheat/Log Auto Solve Tile Order")]
    public void LogAutoSolveTileOrder()
    {
        AutoResolveReferences();

        if (gridManager == null)
        {
            Debug.LogWarning("[CHEAT] GridManager tidak ditemukan.");
            return;
        }

        PathType[] pathTypes = (PathType[])Enum.GetValues(typeof(PathType));
        for (int t = 0; t < pathTypes.Length; t++)
        {
            PathType type = pathTypes[t];
            List<PathTile> matches = GetTilesByTypeSorted(type, false, TileSize.Size1x1);

            if (matches.Count == 0)
                continue;

            for (int i = 0; i < matches.Count; i++)
            {
                PathTile tile = matches[i];
                Debug.Log($"[CHEAT TILE ORDER] {type} size={InferTileSize(tile)} order={i} name={tile.name} pos={tile.gridPosition} rot={tile.RotationIndex} movable={tile.isMovable}");
            }
        }
    }

    LevelAutoSolveSolution FindCurrentLevelSolution()
    {
        if (autoSolveSolutions == null || autoSolveSolutions.Length == 0)
            return null;

        int currentLevel = GetCurrentLevelIndex();
        for (int i = 0; i < autoSolveSolutions.Length; i++)
        {
            LevelAutoSolveSolution solution = autoSolveSolutions[i];
            if (solution != null && solution.levelIndex == currentLevel)
                return solution;
        }

        return null;
    }

    int GetCurrentLevelIndex()
    {
        if (gridManager == null)
            return 0;

        return Mathf.Max(0, gridManager.requestedLevelIndex);
    }

    bool ApplyAutoSolveSolution(LevelAutoSolveSolution solution)
    {
        if (solution.steps == null || solution.steps.Length == 0)
        {
            Debug.LogWarning($"[CHEAT] Solution level {solution.levelIndex} kosong.");
            return false;
        }

        List<PathTile> tiles = new List<PathTile>();
        List<PathTile> stepTiles = new List<PathTile>();
        List<TileSnapshot> snapshots = new List<TileSnapshot>();

        for (int i = 0; i < solution.steps.Length; i++)
        {
            PathTile tile = ResolveTile(solution.steps[i], stepTiles);
            if (tile == null)
            {
                Debug.LogWarning($"[CHEAT] Step {i} gagal menemukan tile. Cek Tile / Path Type / Tile Order.");
                RollbackTiles(snapshots);
                return false;
            }

            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
                snapshots.Add(new TileSnapshot
                {
                    tile = tile,
                    position = tile.gridPosition,
                    rotation = tile.RotationIndex
                });
            }

            stepTiles.Add(tile);
        }

        for (int i = 0; i < tiles.Count; i++)
            gridManager.ClearTile(tiles[i]);

        for (int i = 0; i < solution.steps.Length; i++)
        {
            AutoSolveStep step = solution.steps[i];
            PathTile tile = i < stepTiles.Count ? stepTiles[i] : null;
            if (tile == null)
                continue;

            tile.SetRotationIndex(step.targetRotation, true);
            tile.gridPosition = step.targetGridPosition;
            tile.SetSelected(false);
            tile.SetInvalid(false);
        }

        if (!ValidateSolvedTiles(tiles))
        {
            Debug.LogWarning("[CHEAT] Auto Solve gagal: ada posisi target yang tabrakan / keluar grid.");
            RollbackTiles(snapshots);
            return false;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            gridManager.RegisterTile(tiles[i]);
            tiles[i].UpdatePosition();
        }

        Debug.Log($"[CHEAT] Auto Solve level {solution.levelIndex} berhasil.");
        return true;
    }

    PathTile ResolveTile(AutoSolveStep step, List<PathTile> alreadyResolved)
    {
        if (step == null)
            return null;

        if (gridManager == null)
            return null;

        if (step.tile != null)
        {
            if (IsRuntimeTileInCurrentGrid(step.tile))
            {
                if (alreadyResolved != null && alreadyResolved.Contains(step.tile))
                {
                    Debug.LogWarning(
                        $"[CHEAT] Tile '{step.tile.name}' dipakai lebih dari satu step. " +
                        "Setiap step Auto Solve harus memakai tile runtime yang berbeda.");
                    return null;
                }

                step.tile.SetGridManager(gridManager);
                return step.tile;
            }

            Debug.LogWarning(
                $"[CHEAT] Tile '{step.tile.name}' bukan tile runtime di GridManager saat ini. " +
                "Auto Solve akan pakai Path Type + Tile Size + Tile Order sebagai fallback.");
        }

        List<PathTile> matches = GetTilesByTypeSorted(step.pathType, step.matchTileSize, step.tileSize);
        if (matches.Count == 0)
            return null;

        int index = Mathf.Clamp(step.tileOrder, 0, matches.Count - 1);
        PathTile resolvedTile = matches[index];

        if (alreadyResolved != null && alreadyResolved.Contains(resolvedTile))
        {
            Debug.LogWarning(
                $"[CHEAT] Tile order {step.tileOrder} untuk {step.pathType} size={step.tileSize} " +
                $"mengarah ke tile yang sudah dipakai: {resolvedTile.name}. Mencari tile lain yang belum dipakai.");

            resolvedTile = null;
            for (int i = 0; i < matches.Count; i++)
            {
                if (!alreadyResolved.Contains(matches[i]))
                {
                    resolvedTile = matches[i];
                    break;
                }
            }

            if (resolvedTile == null)
                return null;
        }

        resolvedTile.SetGridManager(gridManager);
        return resolvedTile;
    }

    bool IsRuntimeTileInCurrentGrid(PathTile tile)
    {
        if (tile == null || gridManager == null)
            return false;

        if (!tile.gameObject.scene.IsValid())
            return false;

        return tile.transform.IsChildOf(gridManager.transform);
    }

    List<PathTile> GetTilesByTypeSorted(PathType pathType, bool matchTileSize, TileSize tileSize)
    {
        List<PathTile> matches = new List<PathTile>();

        if (gridManager == null)
            return matches;

        PathTile[] allTiles = gridManager.GetComponentsInChildren<PathTile>(true);

        for (int i = 0; i < allTiles.Length; i++)
        {
            PathTile tile = allTiles[i];
            if (tile == null)
                continue;

            if (tile.pathType != pathType)
                continue;

            if (matchTileSize && InferTileSize(tile) != tileSize)
                continue;

            if (!includeStaticTilesWhenResolvingByOrder && !tile.isMovable)
                continue;

            matches.Add(tile);
        }

        matches.Sort((a, b) =>
        {
            if (a.gridPosition.y != b.gridPosition.y)
                return b.gridPosition.y.CompareTo(a.gridPosition.y);

            if (a.gridPosition.x != b.gridPosition.x)
                return a.gridPosition.x.CompareTo(b.gridPosition.x);

            return string.CompareOrdinal(a.name, b.name);
        });

        return matches;
    }

    TileSize InferTileSize(PathTile tile)
    {
        if (tile == null || tile.shapeOffsets == null || tile.shapeOffsets.Length <= 1)
            return TileSize.Size1x1;

        int minX = tile.shapeOffsets[0].x;
        int maxX = tile.shapeOffsets[0].x;
        int minY = tile.shapeOffsets[0].y;
        int maxY = tile.shapeOffsets[0].y;

        for (int i = 1; i < tile.shapeOffsets.Length; i++)
        {
            Vector2Int offset = tile.shapeOffsets[i];
            minX = Mathf.Min(minX, offset.x);
            maxX = Mathf.Max(maxX, offset.x);
            minY = Mathf.Min(minY, offset.y);
            maxY = Mathf.Max(maxY, offset.y);
        }

        int width = Mathf.Abs(maxX - minX) + 1;
        int height = Mathf.Abs(maxY - minY) + 1;

        if (width >= 2 && height >= 2)
            return TileSize.Size2x2;

        return TileSize.Size1x2;
    }

    bool ValidateSolvedTiles(List<PathTile> tiles)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        for (int i = 0; i < tiles.Count; i++)
        {
            PathTile tile = tiles[i];
            if (tile == null)
                continue;

            List<Vector2Int> cells = tile.GetOccupiedCells();
            for (int c = 0; c < cells.Count; c++)
            {
                Vector2Int cell = cells[c];
                if (!gridManager.IsValidPosition(cell))
                {
                    Debug.LogWarning($"[CHEAT] Auto Solve invalid: {tile.name} cell {cell} keluar grid. Pos={tile.gridPosition}, Rot={tile.RotationIndex}");
                    return false;
                }

                if (!occupied.Add(cell))
                {
                    Debug.LogWarning($"[CHEAT] Auto Solve invalid: {tile.name} cell {cell} tabrakan dengan tile auto-solve lain. Pos={tile.gridPosition}, Rot={tile.RotationIndex}");
                    return false;
                }

                Tile existing = gridManager.GetTileAt(cell);
                if (existing != null && !tiles.Contains(existing as PathTile))
                {
                    Debug.LogWarning($"[CHEAT] Auto Solve invalid: {tile.name} cell {cell} menabrak tile statis/existing {existing.name}. Pos={tile.gridPosition}, Rot={tile.RotationIndex}");
                    return false;
                }
            }
        }

        return true;
    }

    void RollbackTiles(List<TileSnapshot> snapshots)
    {
        if (gridManager == null || snapshots == null)
            return;

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i]?.tile != null)
                gridManager.ClearTile(snapshots[i].tile);
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            TileSnapshot snapshot = snapshots[i];
            if (snapshot == null || snapshot.tile == null)
                continue;

            snapshot.tile.SetRotationIndex(snapshot.rotation, true);
            snapshot.tile.gridPosition = snapshot.position;
            gridManager.RegisterTile(snapshot.tile);
            snapshot.tile.UpdatePosition();
        }
    }

    void RefreshLevelButtons()
    {
        if (!refreshLevelButtonsAfterCheat)
            return;

        MainMenuLevelButton[] mainButtons = FindObjectsOfType<MainMenuLevelButton>(true);
        for (int i = 0; i < mainButtons.Length; i++)
        {
            if (mainButtons[i] != null)
                mainButtons[i].RefreshVisuals();
        }

        LevelSelectButton[] visualButtons = FindObjectsOfType<LevelSelectButton>(true);
        for (int i = 0; i < visualButtons.Length; i++)
        {
            if (visualButtons[i] != null)
                visualButtons[i].RefreshVisuals();
        }
    }

    void RefreshShop()
    {
        if (!refreshShopAfterCheat)
            return;

        if (shopUI == null)
            shopUI = FindObjectOfType<BallSkinShopUI>(true);

        if (shopUI != null)
            shopUI.RefreshAllVisuals();
    }

    void RefreshRuntimeBallSkin()
    {
        BallController runtimeBall = FindObjectOfType<BallController>(true);
        if (runtimeBall != null)
            runtimeBall.ApplySelectedSkin();
    }
}
