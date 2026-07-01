using UnityEngine;

namespace PathPuzzle
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public enum PathType
    {
        Straight,
        Corner,
        Wall,
        Start,
        Finish
    }

    public enum GameState
    {
        Setup,
        Simulation
    }

    public enum TileSize
    {
        Size1x1,
        Size1x2,
        Size2x2
    }

    public static class DirectionExtensions
    {
        // =========================
        // BASIC
        // =========================

        public static Vector2Int ToVector(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Down: return new Vector2Int(0, -1);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Right: return new Vector2Int(1, 0);
                default:
                    return Vector2Int.zero; // 🔥 no spam log
            }
        }

        public static Direction Opposite(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default:
                    return Direction.None;
            }
        }

        // =========================
        // ROTATION (UNITY Y)
        // =========================

        public static Quaternion ToRotation(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Quaternion.Euler(0, 0, 0);
                case Direction.Right: return Quaternion.Euler(0, 90, 0);
                case Direction.Down: return Quaternion.Euler(0, 180, 0);
                case Direction.Left: return Quaternion.Euler(0, 270, 0);
                default:
                    return Quaternion.identity;
            }
        }

        // =========================
        // ROTATION SYSTEM
        // =========================

        public static Direction RotateClockwise(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Direction.Right;
                case Direction.Right: return Direction.Down;
                case Direction.Down: return Direction.Left;
                case Direction.Left: return Direction.Up;
                default: return Direction.None;
            }
        }

        public static Direction RotateCounterClockwise(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return Direction.Left;
                case Direction.Left: return Direction.Down;
                case Direction.Down: return Direction.Right;
                case Direction.Right: return Direction.Up;
                default: return Direction.None;
            }
        }

        // =========================
        // HELPER
        // =========================

        public static bool IsHorizontal(this Direction dir)
        {
            return dir == Direction.Left || dir == Direction.Right;
        }

        public static bool IsVertical(this Direction dir)
        {
            return dir == Direction.Up || dir == Direction.Down;
        }

        public static bool IsNone(this Direction dir)
        {
            return dir == Direction.None;
        }

        public static string ToShortString(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return "↑";
                case Direction.Down: return "↓";
                case Direction.Left: return "←";
                case Direction.Right: return "→";
                default: return "•";
            }
        }
    }
}