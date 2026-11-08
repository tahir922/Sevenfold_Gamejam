using UnityEngine;

namespace Sevenfold.Core
{
    public enum TileKind
    {
        Normal,
        Star,
        Trap,
        Charm,
        Gap,
        Gate,
        Start
    }

    public enum GameState
    {
        MainMenu,
        TurnStart,
        AwaitRoll,
        Rolling,
        AwaitFateChoice,
        PlayerMoving,
        ResolvingTile,
        TurnEnding,
        Paused,
        Victory,
        Defeat
    }

    public enum BlessingKind
    {
        FatesSight,
        FortuneWard,
        SecondChance
    }

    public enum DefeatReason
    {
        HeartsDepleted,
        TurnLimit
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExt
    {
        public static Vector2Int ToVector(this Direction direction)
        {
            return direction switch
            {
                Direction.Up => new Vector2Int(0, 1),
                Direction.Down => new Vector2Int(0, -1),
                Direction.Left => new Vector2Int(-1, 0),
                Direction.Right => new Vector2Int(1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}

