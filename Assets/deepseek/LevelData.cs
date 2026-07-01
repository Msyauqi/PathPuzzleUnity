using UnityEngine;
using System.Collections.Generic;
using PathPuzzle;

[CreateAssetMenu(fileName = "LevelData", menuName = "Path Puzzle/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;
    public string levelName;
    public Sprite levelPreview;

    [Header("Grid Settings")]
    public Vector2Int startPosition;
    [Range(0, 3)] public int startRotation;

    public Vector2Int finishPosition;
    [Range(0, 3)] public int finishRotation;

    [Header("Paths")]
    public List<PathLevelData> paths = new List<PathLevelData>();

    [Header("Goals")]
    public int targetMoves;
    public int parScore;
    public float timeLimit = 0f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        startRotation = Mathf.Clamp(startRotation, 0, 3);
        finishRotation = Mathf.Clamp(finishRotation, 0, 3);

        if (startPosition == finishPosition)
            Debug.LogWarning($"[LEVEL {levelNumber}] Start dan Finish sama!");

        if (paths == null)
            paths = new List<PathLevelData>();

        foreach (var p in paths)
        {
            if (p == null) continue;

            p.rotation = Mathf.Clamp(p.rotation, 0, 3);

            if (p.pathType == PathType.Start || p.pathType == PathType.Finish)
            {
                Debug.LogWarning($"[LEVEL {levelNumber}] Start/Finish tidak perlu dimasukkan ke list paths.");
            }
        }
    }
#endif

    public bool HasTimeLimit()
    {
        return timeLimit > 0f;
    }

    public bool HasMoveLimit()
    {
        return targetMoves > 0;
    }
}

[System.Serializable]
public class PathLevelData
{
    [Header("Shape")]
    public PathType pathType;
    public TileSize tileSize;

    [Header("Transform")]
    public Vector2Int position;

    [Range(0, 3)]
    public int rotation;

    [Header("Visual")]
    public int skinIndex = 0;

    [Header("Gameplay")]
    public bool isMovable = true;

    public PathLevelData(
        PathType type,
        TileSize size,
        Vector2Int pos,
        int rot = 0,
        int skin = 0,
        bool movable = true)
    {
        pathType = type;
        tileSize = size;
        position = pos;
        rotation = Mathf.Clamp(rot, 0, 3);
        skinIndex = skin;
        isMovable = movable;
    }

    public override string ToString()
    {
        return $"[{pathType}] {position} rot={rotation} movable={isMovable}";
    }
}
