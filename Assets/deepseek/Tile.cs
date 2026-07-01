using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;

    protected GridManager gridManager;

    private readonly List<Vector2Int> singleCellCache = new List<Vector2Int>(1);

    // =========================
    public void SetGridManager(GridManager gm)
    {
        gridManager = gm;

        if (gridManager == null)
            Debug.LogError($"[TILE] GridManager NULL di {name}");
    }

    // =========================
    public virtual void Initialize(Vector2Int pos)
    {
        gridPosition = pos;

        if (gridManager == null)
        {
            Debug.LogError($"[TILE INIT] GridManager belum diset di {name}");
            return;
        }

        UpdatePosition();
    }

    // =========================
    public virtual List<Vector2Int> GetOccupiedCells()
    {
        singleCellCache.Clear();
        singleCellCache.Add(gridPosition);
        return singleCellCache;
    }

    // =========================
    public virtual void UpdatePosition()
    {
        if (gridManager == null)
        {
            Debug.LogError($"[UPDATE POS] GridManager NULL di {name}");
            return;
        }

        float offsetX = (gridManager.width - 1) * 0.5f * gridManager.CellSize;
        float offsetZ = (gridManager.height - 1) * 0.5f * gridManager.CellSize;

        float y = GetTileHeight();

        transform.localPosition = new Vector3(
            gridPosition.x * gridManager.CellSize - offsetX,
            y,
            gridPosition.y * gridManager.CellSize - offsetZ
        );
    }

    // =========================
    protected virtual float GetTileHeight()
    {
        if (gridManager == null)
            return 0f;

        return gridManager.GridTopY
             + gridManager.gridCellHeight
             + (transform.localScale.y * 0.5f);
    }
}
