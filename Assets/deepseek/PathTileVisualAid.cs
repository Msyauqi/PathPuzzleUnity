using System.Collections.Generic;
using PathPuzzle;
using UnityEngine;

public class PathTileVisualAid : MonoBehaviour
{
    const string RootName = "PathVisualAid";

    [Header("Visibility")]
    public bool showCellBorders = true;
    public bool showTypeLabel = true;

    [Header("Layout")]
    [Range(0.001f, 0.08f)] public float borderThickness = 0.025f;
    [Range(0f, 0.25f)] public float borderInset = 0.045f;
    public float borderLocalY = 0.56f;
    public float borderHeight = 0.012f;
    public float labelLocalY = 0.64f;
    public float labelCharacterSize = 0.13f;
    public float labelZOffset = -0.28f;

    [Header("Colors")]
    public Color straightColor = new Color(0.15f, 0.75f, 1f, 1f);
    public Color cornerColor = new Color(1f, 0.78f, 0.12f, 1f);
    public Color startColor = new Color(0.2f, 1f, 0.25f, 1f);
    public Color finishColor = new Color(1f, 0.18f, 0.18f, 1f);
    public Color labelColor = Color.white;

    PathTile tile;
    Transform visualRoot;
    Material borderMaterial;

    public void Build(PathTile targetTile)
    {
        tile = targetTile != null ? targetTile : GetComponent<PathTile>();
        if (tile == null)
            return;

        Clear();

        visualRoot = new GameObject(RootName).transform;
        visualRoot.SetParent(tile.transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        Color typeColor = GetColorForType(tile.pathType);
        borderMaterial = CreateMaterial(typeColor);

        if (showCellBorders)
            BuildCellBorders(typeColor);

        if (showTypeLabel)
            BuildLabel(typeColor);
    }

    public void Clear()
    {
        Transform oldRoot = transform.Find(RootName);
        if (oldRoot != null)
        {
            if (Application.isPlaying)
                Destroy(oldRoot.gameObject);
            else
                DestroyImmediate(oldRoot.gameObject);
        }

        visualRoot = null;
    }

    void BuildCellBorders(Color typeColor)
    {
        Vector2Int[] offsets = GetShapeOffsets();
        float inset = Mathf.Clamp(borderInset, 0f, 0.45f);
        float size = Mathf.Max(0.05f, 1f - inset);
        float half = size * 0.5f;
        float thickness = Mathf.Max(0.001f, borderThickness);

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 center = new Vector3(offsets[i].x, borderLocalY, offsets[i].y);

            CreateBorderPiece($"Border_Top_{i}", center + new Vector3(0f, 0f, half), new Vector3(size, borderHeight, thickness));
            CreateBorderPiece($"Border_Bottom_{i}", center + new Vector3(0f, 0f, -half), new Vector3(size, borderHeight, thickness));
            CreateBorderPiece($"Border_Left_{i}", center + new Vector3(-half, 0f, 0f), new Vector3(thickness, borderHeight, size));
            CreateBorderPiece($"Border_Right_{i}", center + new Vector3(half, 0f, 0f), new Vector3(thickness, borderHeight, size));
        }
    }

    void BuildLabel(Color typeColor)
    {
        GameObject labelObject = new GameObject("PathTypeLabel");
        labelObject.transform.SetParent(visualRoot, false);
        labelObject.transform.localPosition = GetShapeCenter() + new Vector3(0f, labelLocalY, labelZOffset);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelObject.transform.localScale = Vector3.one;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = GetLabelText();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 64;
        label.characterSize = Mathf.Max(0.01f, labelCharacterSize);
        label.color = labelColor;

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateMaterial(labelColor);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    void CreateBorderPiece(string pieceName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = pieceName;
        piece.transform.SetParent(visualRoot, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.identity;
        piece.transform.localScale = localScale;

        Collider collider = piece.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = borderMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    Vector2Int[] GetShapeOffsets()
    {
        if (tile != null && tile.shapeOffsets != null && tile.shapeOffsets.Length > 0)
            return tile.shapeOffsets;

        return new[] { Vector2Int.zero };
    }

    Vector3 GetShapeCenter()
    {
        Vector2Int[] offsets = GetShapeOffsets();
        if (offsets.Length == 0)
            return Vector3.zero;

        Vector2 min = new Vector2(offsets[0].x, offsets[0].y);
        Vector2 max = min;

        for (int i = 1; i < offsets.Length; i++)
        {
            min.x = Mathf.Min(min.x, offsets[i].x);
            min.y = Mathf.Min(min.y, offsets[i].y);
            max.x = Mathf.Max(max.x, offsets[i].x);
            max.y = Mathf.Max(max.y, offsets[i].y);
        }

        Vector2 center = (min + max) * 0.5f;
        return new Vector3(center.x, 0f, center.y);
    }

    string GetLabelText()
    {
        string typeText = "?";

        if (tile != null)
        {
            if (tile.pathType == PathType.Straight) typeText = "S";
            else if (tile.pathType == PathType.Corner) typeText = "C";
            else if (tile.pathType == PathType.Start) typeText = "START";
            else if (tile.pathType == PathType.Finish) typeText = "FIN";
            else if (tile.pathType == PathType.Wall) typeText = "W";
        }

        return $"{typeText}\n{GetSizeText()}";
    }

    string GetSizeText()
    {
        Vector2Int[] offsets = GetShapeOffsets();
        if (offsets.Length == 1)
            return "1x1";

        int minX = offsets[0].x;
        int maxX = offsets[0].x;
        int minY = offsets[0].y;
        int maxY = offsets[0].y;

        for (int i = 1; i < offsets.Length; i++)
        {
            minX = Mathf.Min(minX, offsets[i].x);
            maxX = Mathf.Max(maxX, offsets[i].x);
            minY = Mathf.Min(minY, offsets[i].y);
            maxY = Mathf.Max(maxY, offsets[i].y);
        }

        int xSize = Mathf.Max(1, maxX - minX + 1);
        int ySize = Mathf.Max(1, maxY - minY + 1);

        return $"{xSize}x{ySize}";
    }

    Color GetColorForType(PathType type)
    {
        switch (type)
        {
            case PathType.Straight:
                return straightColor;
            case PathType.Corner:
                return cornerColor;
            case PathType.Start:
                return startColor;
            case PathType.Finish:
                return finishColor;
            default:
                return Color.white;
        }
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }
}
