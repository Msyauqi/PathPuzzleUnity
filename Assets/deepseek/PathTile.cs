using UnityEngine;
using System.Collections.Generic;
using PathPuzzle;

public class PathTile : Tile
{
    public PathType pathType;

    [Header("Shape (Grid)")]
    public Vector2Int[] shapeOffsets;

    [Header("Internal Path")]
    public List<Direction> basePath = new List<Direction>();

    [Header("Gameplay")]
    public bool isMovable = true;

    [Header("Highlight")]
    public Color selectedColor = new Color(1.00f, 0.92f, 0.20f);
    public Color invalidColorA = new Color(1.00f, 0.15f, 0.15f);
    public Color invalidColorB = new Color(1.00f, 0.15f, 0.15f);
    public float invalidPulseSpeed = 10f;
    public float invalidEmissionIntensity = 1.2f;
    public float selectedEmissionIntensity = 0.35f;
    public bool pulseInvalidColor = false;

    private int rotationIndex = 0;

    private Renderer[] renderers;
    private Material[] cachedMaterials;
    private Color[] originalColors;

    private bool isSelected;
    private bool isInvalid;
    private float invalidPulseTime;

    public int RotationIndex => rotationIndex;
    public bool IsVisualOverrideActive => isSelected || isInvalid;

    void Awake()
    {
        CacheRenderersAndOriginalColors();
        RefreshVisualState();
    }

    void Update()
    {
        if (!isInvalid) return;

        Color pulseColor = invalidColorA;
        if (pulseInvalidColor)
        {
            invalidPulseTime += Time.deltaTime * invalidPulseSpeed;
            float pulse = 0.5f + (Mathf.Sin(invalidPulseTime) * 0.5f);
            pulseColor = Color.Lerp(invalidColorA, invalidColorB, pulse);
        }

        ApplyColorToAll(pulseColor);
        ApplyEmissionToAll(pulseColor * invalidEmissionIntensity);
    }

    // =========================
    void CacheRenderersAndOriginalColors()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        cachedMaterials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            cachedMaterials[i] = renderers[i].material;
            originalColors[i] = GetMaterialColor(cachedMaterials[i]);
        }
    }

    // =========================
    Color GetMaterialColor(Material mat)
    {
        if (mat == null) return Color.white;

        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        if (mat.HasProperty("_Color"))
            return mat.color;

        return Color.white;
    }

    // =========================
    void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null) return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.color = color;
    }

    // =========================
    Direction RotateDir(Direction dir)
    {
        for (int i = 0; i < rotationIndex; i++)
            dir = dir.RotateClockwise();

        return dir;
    }

    // =========================
    Vector2Int RotateOffset(Vector2Int o)
    {
        switch (rotationIndex % 4)
        {
            case 0: return new Vector2Int(o.x, o.y);
            case 1: return new Vector2Int(o.y, -o.x);
            case 2: return new Vector2Int(-o.x, -o.y);
            case 3: return new Vector2Int(-o.y, o.x);
            default: return o;
        }
    }

    // =========================
    public override List<Vector2Int> GetOccupiedCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        if (shapeOffsets == null || shapeOffsets.Length == 0)
        {
            Debug.LogError($"[SHAPE ERROR] {name} belum punya shapeOffsets!");
            return cells;
        }

        foreach (var offset in shapeOffsets)
        {
            Vector2Int rotated = RotateOffset(offset);
            cells.Add(gridPosition + rotated);
        }

        return cells;
    }

    // =========================
    // Format:
    // Start tile  : [step1, step2, ...]
    // Normal tile : [enterSide, move1, move2, ..., exitStep]
    // =========================
    public List<Direction> GetPath(Direction enterDir)
    {
        if (basePath == null || basePath.Count == 0)
        {
            Debug.LogError($"[PATH ERROR] {name} basePath kosong");
            return null;
        }

        if (pathType == PathType.Start)
        {
            List<Direction> startSteps = new List<Direction>();
            for (int i = 0; i < basePath.Count; i++)
                startSteps.Add(RotateDir(basePath[i]));

            return startSteps;
        }

        if (pathType == PathType.Wall)
        {
            Debug.LogWarning($"[PATH FAIL] {name} adalah wall");
            return null;
        }

        if (basePath.Count < 2)
        {
            Debug.LogError($"[PATH ERROR] {name} invalid basePath");
            return null;
        }

        Direction forwardEnter = RotateDir(basePath[0]);
        Direction reverseEnter = RotateDir(basePath[basePath.Count - 1]);

        Debug.Log($"[ROTATED] forwardEnter={forwardEnter} reverseEnter={reverseEnter} rotIdx={rotationIndex}");

        if (enterDir == forwardEnter)
            return BuildForwardSteps();

        if (enterDir == reverseEnter)
            return BuildReverseSteps();

        Debug.LogWarning($"[PATH FAIL] {name} enter={enterDir} expected={forwardEnter} / {reverseEnter}");
        return null;
    }

    // =========================
    List<Direction> BuildForwardSteps()
    {
        List<Direction> steps = new List<Direction>();

        for (int i = 1; i < basePath.Count; i++)
            steps.Add(RotateDir(basePath[i]));

        Debug.Log($"[FORWARD STEPS] {string.Join(",", steps)}");
        return steps;
    }

    // =========================
    List<Direction> BuildReverseSteps()
    {
        List<Direction> steps = new List<Direction>();

        for (int i = basePath.Count - 2; i >= 1; i--)
            steps.Add(RotateDir(basePath[i]).Opposite());

        steps.Add(RotateDir(basePath[0]));

        Debug.Log($"[REVERSE STEPS] {string.Join(",", steps)}");
        return steps;
    }

    // =========================
    public void RotateClockwise(bool force = false)
{
    if (!isMovable && !force) return;

    transform.Rotate(0f, 90f, 0f);
    rotationIndex = (rotationIndex + 1) % 4;

    Debug.Log($"[ROTATE] {name} rot={rotationIndex} force={force}");
}

    public void SetRotationIndex(int targetRotation, bool force = false)
    {
        int safeTarget = ((targetRotation % 4) + 4) % 4;
        int guard = 0;

        while (rotationIndex != safeTarget && guard < 4)
        {
            RotateClockwise(force);
            guard++;
        }
    }


    // =========================
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisualState();
    }

    // =========================
    public void SetInvalid(bool invalid)
    {
        isInvalid = invalid;

        if (isInvalid)
        {
            invalidPulseTime = 0f;
            ApplyColorToAll(invalidColorA);
            ApplyEmissionToAll(invalidColorA * invalidEmissionIntensity);
            return;
        }

        if (!isInvalid)
        {
            invalidPulseTime = 0f;
            RefreshVisualState();
        }
    }

    // =========================
    void RefreshVisualState()
    {
        if (isInvalid)
            return;

        if (isSelected)
        {
            ApplyColorToAll(selectedColor);
            ApplyEmissionToAll(selectedColor * selectedEmissionIntensity);
            return;
        }

        RestoreOriginalColors();
        ApplyEmissionToAll(Color.black);
        GameplayVisualBrightness.RequestRefreshAll();
    }

    // =========================
    void RestoreOriginalColors()
    {
        if (cachedMaterials == null) return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null) continue;
            if (originalColors == null || i >= originalColors.Length) continue;
            SetMaterialColor(cachedMaterials[i], originalColors[i]);
        }
    }

    public void CaptureCurrentColorsAsOriginal()
    {
        if (cachedMaterials == null || originalColors == null) return;
        if (IsVisualOverrideActive) return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null) continue;
            if (i >= originalColors.Length) continue;
            originalColors[i] = GetMaterialColor(cachedMaterials[i]);
        }
    }

    // =========================
    void ApplyColorToAll(Color color)
    {
        if (cachedMaterials == null) return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null) continue;
            SetMaterialColor(cachedMaterials[i], color);
        }
    }

    // =========================
    void ApplyEmissionToAll(Color emissionColor)
    {
        if (cachedMaterials == null) return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null) continue;

            if (cachedMaterials[i].HasProperty("_EmissionColor"))
            {
                cachedMaterials[i].EnableKeyword("_EMISSION");
                cachedMaterials[i].SetColor("_EmissionColor", emissionColor);
            }
        }
    }

    // =========================
    public void MoveTo(Vector2Int newPos)
    {
        Vector2Int oldPos = gridPosition;

        gridManager.ClearTile(this);
        gridPosition = newPos;

        if (!gridManager.CanPlaceTile(this))
        {
            Debug.LogWarning($"[MOVE FAIL] {name} ke {newPos}");
            gridPosition = oldPos;
            gridManager.RegisterTile(this);
            return;
        }

        gridManager.RegisterTile(this);
        UpdatePosition();
    }

    // =========================
    public bool CanMoveTo(Vector2Int targetPos)
    {
        if (!isMovable) return false;

        Vector2Int original = gridPosition;
        gridPosition = targetPos;
        bool valid = gridManager.CanPlaceTile(this);
        gridPosition = original;

        return valid;
    }
}
