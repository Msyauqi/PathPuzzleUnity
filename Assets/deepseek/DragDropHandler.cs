using UnityEngine;
using PathPuzzle;
using UnityEngine.EventSystems;
using System;

public class DragDropHandler : MonoBehaviour
{
    public static event Action<PathTile> TileDragStarted;
    public static event Action<PathTile> TileRotated;
    public static event Action<PathTile, Vector2Int> TileDropped;

    [Header("Drag Settings")]
    public Camera arCamera;

    [Header("References")]
    public GridManager gridManager;
    public GameManager gameManager;
    public ARPlacementManager arPlacementManager;

    [Header("Visual")]
    public float dragHeight = 0.02f;
    public float smoothSpeed = 15f;

    [Header("Snap Assistance")]
    [Range(0.1f, 1.0f)] public float snapForgivenessCells = 0.65f;
    [Range(0.1f, 1.25f)] public float edgeSnapForgivenessCells = 0.75f;
    public bool keepFingerGrabOffset = true;
    public bool blockByFingerRaycast = false;

    [Header("Rotate Snap Recovery")]
    public bool recenterGrabOffsetAfterRotate = true;
    public bool snapToNearestValidCellWhileDragging = true;
    public bool snapToNearestValidCellOnDrop = true;
    [Range(0.25f, 2.5f)] public float rotatedDropSearchRadiusCells = 1.25f;

    [Header("Blocked Feedback")]
    public float bumpDistance = 0.025f;
    public float bumpReturnSpeed = 14f;
    public float bumpShakeSpeed = 20f;
    public float bumpShakeAmount = 0.006f;

    private PathTile selectedTile;
    private bool isDragging = false;

    private Vector2Int originalGridPos;
    private Vector3 targetLocalPos;

    private PathTile draggingTile;
    private float lastRotateTime = 0f;

    private Vector2Int lastValidGridPos;
    private Vector3 lastValidLocalPos;
    private bool hasLastValidGridPos = false;
    private Vector3 dragGrabOffsetLocal = Vector3.zero;
    private bool hasDragGrabOffset = false;

    private bool isBumping = false;
    private Vector3 bumpOffset = Vector3.zero;
    private Vector3 bumpDirection = Vector3.zero;
    private float bumpTime = 0f;
    private float lastInvalidSfxTime = -999f;

    private Collider[] selectedTileColliders;

    void Update()
    {
        if (arPlacementManager != null && !arPlacementManager.IsPlaced())
            return;

        if (gameManager.GetCurrentState() != GameState.Setup)
            return;

        if (Input.touchCount > 0 &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            return;

        if (Input.touchCount >= 2 && draggingTile != null)
        {
            if (Time.time - lastRotateTime > 0.25f)
            {
                Vector2 primaryTouchPosition = Input.GetTouch(0).position;

                draggingTile.RotateClockwise();
                TileRotated?.Invoke(draggingTile);
                lastRotateTime = Time.time;

                if (recenterGrabOffsetAfterRotate)
                {
                    dragGrabOffsetLocal = Vector3.zero;
                    hasDragGrabOffset = false;
                }

                if (hasLastValidGridPos && !draggingTile.CanMoveTo(lastValidGridPos))
                    hasLastValidGridPos = false;

                RecoverSnapAfterRotation(primaryTouchPosition);

                Debug.Log("[ROTATE]");
            }
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchMoved(touch);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnded(touch);
                    break;
            }
        }

        UpdateBump();

        if (isDragging && selectedTile != null)
        {
            Vector3 finalTarget = targetLocalPos + bumpOffset;

            selectedTile.transform.localPosition = Vector3.Lerp(
                selectedTile.transform.localPosition,
                finalTarget,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    private void OnTouchBegan(Touch touch)
    {
        Ray ray = arCamera.ScreenPointToRay(touch.position);
        int layerMask = LayerMask.GetMask("Tile");

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask);
        if (hits == null || hits.Length == 0)
            return;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            PathTile tile = hits[i].collider.GetComponentInParent<PathTile>();
            if (tile == null)
                continue;

            if (!tile.isMovable ||
                tile.pathType == PathType.Wall ||
                tile.pathType == PathType.Start ||
                tile.pathType == PathType.Finish)
            {
                Debug.Log($"[SELECT BLOCKED] {tile.name}");
                return;
            }

            selectedTile = tile;
            draggingTile = tile;
            originalGridPos = tile.gridPosition;

            selectedTile.SetSelected(true);
            selectedTile.SetInvalid(false);

            lastValidGridPos = originalGridPos;
            lastValidLocalPos = GetGridLocalPosition(originalGridPos, true);
            hasLastValidGridPos = true;

            targetLocalPos = lastValidLocalPos;
            CacheDragGrabOffset(touch.position);

            ResetBump();

            Debug.Log($"[SELECT] {tile.pathType}");
            TileDragStarted?.Invoke(tile);
            SfxManager.Instance?.PlayTileDragStart();
            return;
        }
    }

    private void OnTouchMoved(Touch touch)
    {
        if (selectedTile == null) return;

        if (!isDragging)
        {
            isDragging = true;
            draggingTile = selectedTile;

            selectedTileColliders = selectedTile.GetComponentsInChildren<Collider>(true);
            SetSelectedTileCollidersEnabled(false);

            gridManager.ClearTile(selectedTile);
        }

        bool hasBoardPoint = TryGetBoardLocalPoint(touch.position, out Vector3 localBoardPoint);
        Vector3 desiredPivotLocalPoint = hasBoardPoint ? GetDragPivotLocalPoint(localBoardPoint) : Vector3.zero;
        bool hasGridPos = TryGetGridPosition(touch.position, out Vector2Int gridPos);

        if (blockByFingerRaycast && IsDragBlockedByStaticTile(touch.position))
        {
            if (hasLastValidGridPos)
                targetLocalPos = lastValidLocalPos;

            selectedTile.SetInvalid(true);

            Vector3 blockDir = Vector3.zero;
            if (hasBoardPoint)
            {
                Vector3 desired = new Vector3(
                    desiredPivotLocalPoint.x,
                    gridManager.gridCellHeight + dragHeight,
                    desiredPivotLocalPoint.z
                );

                blockDir = desired - lastValidLocalPos;
                blockDir.y = 0f;
            }

            StartBump(blockDir);
            PlayInvalidMoveSfx();
            return;
        }

        if ((!hasGridPos || !selectedTile.CanMoveTo(gridPos)) &&
            snapToNearestValidCellWhileDragging &&
            TryGetBestDropGridPosition(touch.position, out Vector2Int assistedGridPos))
        {
            lastValidGridPos = assistedGridPos;
            lastValidLocalPos = GetGridLocalPosition(assistedGridPos, true);
            hasLastValidGridPos = true;

            targetLocalPos = lastValidLocalPos;
            selectedTile.SetInvalid(false);
            ResetBump();
            return;
        }

        if (hasBoardPoint && !hasGridPos)
        {
            targetLocalPos = new Vector3(
                desiredPivotLocalPoint.x,
                gridManager.gridCellHeight + dragHeight,
                desiredPivotLocalPoint.z
            );

            selectedTile.SetInvalid(true);
            ResetBump();
            PlayInvalidMoveSfx();
            return;
        }

        if (hasGridPos && selectedTile.CanMoveTo(gridPos))
        {
            lastValidGridPos = gridPos;
            lastValidLocalPos = GetGridLocalPosition(gridPos, true);
            hasLastValidGridPos = true;

            targetLocalPos = lastValidLocalPos;
            selectedTile.SetInvalid(false);
            ResetBump();
            return;
        }

        if (hasGridPos && !selectedTile.CanMoveTo(gridPos))
        {
            if (hasLastValidGridPos)
                targetLocalPos = lastValidLocalPos;

            selectedTile.SetInvalid(true);

            Vector3 blockDir = Vector3.zero;
            if (hasBoardPoint)
            {
                Vector3 desired = new Vector3(
                    desiredPivotLocalPoint.x,
                    gridManager.gridCellHeight + dragHeight,
                    desiredPivotLocalPoint.z
                );

                blockDir = desired - lastValidLocalPos;
                blockDir.y = 0f;
            }

            StartBump(blockDir);
            PlayInvalidMoveSfx();
            return;
        }

        selectedTile.SetInvalid(true);
        PlayInvalidMoveSfx();
    }

    private void OnTouchEnded(Touch touch)
    {
        if (selectedTile == null)
        {
            ClearDrag();
            return;
        }

        bool hasValidGrid = TryGetBestDropGridPosition(touch.position, out Vector2Int gridPos);
        bool success = false;

        if (hasValidGrid)
        {
            selectedTile.gridPosition = gridPos;
            gridManager.RegisterTile(selectedTile);
            selectedTile.UpdatePosition();
            success = true;
        }
        else
        {
            selectedTile.gridPosition = originalGridPos;
            gridManager.RegisterTile(selectedTile);
            selectedTile.UpdatePosition();

            Debug.Log("[DROP FAIL -> SNAP BACK]");
            PlayInvalidMoveSfx(true);
        }

        if (success)
        {
            gameManager.IncrementMoveCount();
            Debug.Log($"[MOVE OK] {selectedTile.name} -> {gridPos}");
            TileDropped?.Invoke(selectedTile, gridPos);
            SfxManager.Instance?.PlayTileDrop();
        }

        ClearDrag();
    }

    private bool TryGetBoardLocalPoint(Vector2 screenPos, out Vector3 localPoint)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        Plane plane = new Plane(gridManager.transform.up, gridManager.transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 world = ray.GetPoint(distance);
            localPoint = gridManager.transform.InverseTransformPoint(world);
            return true;
        }

        localPoint = Vector3.zero;
        return false;
    }

    private bool TryGetGridPosition(Vector2 screenPos, out Vector2Int gridPos)
    {
        if (!TryGetBoardLocalPoint(screenPos, out Vector3 local))
        {
            gridPos = Vector2Int.zero;
            return false;
        }

        local = GetDragPivotLocalPoint(local);
        return TryGetGridPositionFromLocalPoint(local, out gridPos);
    }

    private bool TryGetGridPositionFromLocalPoint(Vector3 local, out Vector2Int gridPos)
    {
        float offsetX = (gridManager.width - 1) * 0.5f * gridManager.CellSize;
        float offsetZ = (gridManager.height - 1) * 0.5f * gridManager.CellSize;

        float rawX = (local.x + offsetX) / gridManager.CellSize;
        float rawZ = (local.z + offsetZ) / gridManager.CellSize;

        int x = Mathf.RoundToInt(rawX);
        int z = Mathf.RoundToInt(rawZ);

        bool nearBoardX = rawX >= -edgeSnapForgivenessCells && rawX <= (gridManager.width - 1) + edgeSnapForgivenessCells;
        bool nearBoardZ = rawZ >= -edgeSnapForgivenessCells && rawZ <= (gridManager.height - 1) + edgeSnapForgivenessCells;

        if (nearBoardX)
            x = Mathf.Clamp(x, 0, gridManager.width - 1);

        if (nearBoardZ)
            z = Mathf.Clamp(z, 0, gridManager.height - 1);

        float snapTolerance = Mathf.Max(0.1f, snapForgivenessCells);
        bool closeEnough =
            Mathf.Abs(rawX - x) <= snapTolerance &&
            Mathf.Abs(rawZ - z) <= snapTolerance;

        gridPos = new Vector2Int(x, z);
        return closeEnough && gridManager.IsValidPosition(gridPos);
    }

    private bool TryGetBestDropGridPosition(Vector2 screenPos, out Vector2Int gridPos)
    {
        if (TryGetGridPosition(screenPos, out gridPos) &&
            selectedTile != null &&
            selectedTile.CanMoveTo(gridPos))
        {
            return true;
        }

        if (!snapToNearestValidCellOnDrop || selectedTile == null)
        {
            gridPos = Vector2Int.zero;
            return false;
        }

        if (!TryGetBoardLocalPoint(screenPos, out Vector3 local))
        {
            gridPos = Vector2Int.zero;
            return false;
        }

        local = GetDragPivotLocalPoint(local);

        float offsetX = (gridManager.width - 1) * 0.5f * gridManager.CellSize;
        float offsetZ = (gridManager.height - 1) * 0.5f * gridManager.CellSize;

        float rawX = (local.x + offsetX) / gridManager.CellSize;
        float rawZ = (local.z + offsetZ) / gridManager.CellSize;

        int centerX = Mathf.RoundToInt(rawX);
        int centerZ = Mathf.RoundToInt(rawZ);
        int searchRadius = Mathf.CeilToInt(Mathf.Max(0.25f, rotatedDropSearchRadiusCells));
        float maxDistanceSqr = rotatedDropSearchRadiusCells * rotatedDropSearchRadiusCells;

        bool found = false;
        float bestDistanceSqr = float.MaxValue;
        Vector2Int bestPos = Vector2Int.zero;

        for (int x = centerX - searchRadius; x <= centerX + searchRadius; x++)
        {
            for (int z = centerZ - searchRadius; z <= centerZ + searchRadius; z++)
            {
                Vector2Int candidate = new Vector2Int(x, z);

                if (!gridManager.IsValidPosition(candidate))
                    continue;

                float dx = rawX - x;
                float dz = rawZ - z;
                float distanceSqr = (dx * dx) + (dz * dz);

                if (distanceSqr > maxDistanceSqr)
                    continue;

                if (!selectedTile.CanMoveTo(candidate))
                    continue;

                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestPos = candidate;
                    found = true;
                }
            }
        }

        gridPos = bestPos;
        return found;
    }

    private void RecoverSnapAfterRotation(Vector2 screenPos)
    {
        if (selectedTile == null)
            return;

        if (TryGetBestDropGridPosition(screenPos, out Vector2Int recoveredGridPos))
        {
            lastValidGridPos = recoveredGridPos;
            lastValidLocalPos = GetGridLocalPosition(recoveredGridPos, true);
            hasLastValidGridPos = true;

            targetLocalPos = lastValidLocalPos;
            selectedTile.SetInvalid(false);
            ResetBump();
            return;
        }

        if (hasLastValidGridPos)
        {
            targetLocalPos = lastValidLocalPos;
            selectedTile.SetInvalid(!selectedTile.CanMoveTo(lastValidGridPos));
        }
    }

    private void CacheDragGrabOffset(Vector2 screenPos)
    {
        dragGrabOffsetLocal = Vector3.zero;
        hasDragGrabOffset = false;

        if (!keepFingerGrabOffset)
            return;

        if (!TryGetBoardLocalPoint(screenPos, out Vector3 localTouchPoint))
            return;

        dragGrabOffsetLocal = localTouchPoint - lastValidLocalPos;
        dragGrabOffsetLocal.y = 0f;
        hasDragGrabOffset = true;
    }

    private Vector3 GetDragPivotLocalPoint(Vector3 localTouchPoint)
    {
        if (!keepFingerGrabOffset || !hasDragGrabOffset)
            return localTouchPoint;

        Vector3 pivotPoint = localTouchPoint - dragGrabOffsetLocal;
        pivotPoint.y = localTouchPoint.y;
        return pivotPoint;
    }

    private Vector3 GetGridLocalPosition(Vector2Int pos, bool dragging)
    {
        float offsetX = (gridManager.width - 1) * 0.5f * gridManager.CellSize;
        float offsetZ = (gridManager.height - 1) * 0.5f * gridManager.CellSize;
        float y = gridManager.gridCellHeight + (dragging ? dragHeight : 0f);

        return new Vector3(
            pos.x * gridManager.CellSize - offsetX,
            y,
            pos.y * gridManager.CellSize - offsetZ
        );
    }

    private bool IsDragBlockedByStaticTile(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        Plane boardPlane = new Plane(gridManager.transform.up, gridManager.transform.position);

        if (!boardPlane.Raycast(ray, out float boardDistance))
            return false;

        int layerMask = LayerMask.GetMask("Tile");
        RaycastHit[] hits = Physics.RaycastAll(ray, boardDistance + 0.05f, layerMask);

        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            PathTile tile = hits[i].collider.GetComponentInParent<PathTile>();
            if (tile == null)
                continue;

            if (tile == selectedTile)
                continue;

            if (!tile.isMovable ||
                tile.pathType == PathType.Wall ||
                tile.pathType == PathType.Start ||
                tile.pathType == PathType.Finish)
            {
                Debug.Log($"[DRAG BLOCKED] by {tile.name}");
                return true;
            }
        }

        return false;
    }

    private void SetSelectedTileCollidersEnabled(bool enabled)
    {
        if (selectedTileColliders == null) return;

        for (int i = 0; i < selectedTileColliders.Length; i++)
        {
            if (selectedTileColliders[i] != null)
                selectedTileColliders[i].enabled = enabled;
        }
    }

    private void StartBump(Vector3 worldLikeDirection)
    {
        if (worldLikeDirection.sqrMagnitude <= 0.00001f)
            return;

        bumpDirection = worldLikeDirection.normalized;
        bumpTime = 0f;
        isBumping = true;
    }

    private void ResetBump()
    {
        isBumping = false;
        bumpTime = 0f;
        bumpOffset = Vector3.zero;
        bumpDirection = Vector3.zero;
    }

    private void UpdateBump()
    {
        if (!isBumping)
        {
            bumpOffset = Vector3.Lerp(bumpOffset, Vector3.zero, Time.deltaTime * bumpReturnSpeed);
            return;
        }

        bumpTime += Time.deltaTime * bumpShakeSpeed;

        float pulse = Mathf.Sin(bumpTime) * 0.5f + 0.5f;
        float damp = Mathf.Clamp01(1f - (bumpTime / 6f));

        bumpOffset =
            (-bumpDirection * bumpDistance * damp) +
            (Vector3.Cross(Vector3.up, bumpDirection).normalized * Mathf.Sin(bumpTime * 1.7f) * bumpShakeAmount * damp);

        if (damp <= 0.01f)
            isBumping = false;
    }

    private void ClearDrag()
    {
        SetSelectedTileCollidersEnabled(true);
        selectedTileColliders = null;

        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);
            selectedTile.SetInvalid(false);
        }

        selectedTile = null;
        draggingTile = null;
        isDragging = false;
        hasLastValidGridPos = false;
        hasDragGrabOffset = false;
        dragGrabOffsetLocal = Vector3.zero;
        ResetBump();
    }

    private void PlayInvalidMoveSfx(bool force = false)
    {
        if (!force && Time.time - lastInvalidSfxTime < 0.25f)
            return;

        lastInvalidSfxTime = Time.time;
        SfxManager.Instance?.PlayInvalidMove();
    }
}
