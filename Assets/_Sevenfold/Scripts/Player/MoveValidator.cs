using Sevenfold.Board;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Player
{
    public enum MoveRejectReason
    {
        None,
        OutOfBounds,
        Gap,
        GateLocked,
        NoPoints
    }

    /// <summary>
    /// Pure C# static movement rule validator.
    /// Evaluates if a proposed 1-step move from the current session state is legal on the board.
    /// </summary>
    public static class MoveValidator
    {
        public static bool CanMove(
            GameSessionState s,
            BoardGrid g,
            Direction d,
            out Vector2Int target,
            out MoveRejectReason reason)
        {
            if (s == null)
            {
                target = Vector2Int.zero;
                reason = MoveRejectReason.OutOfBounds;
                return false;
            }

            if (!System.Enum.IsDefined(typeof(Direction), d))
            {
                target = s.PlayerCell;
                reason = MoveRejectReason.OutOfBounds;
                return false;
            }

            target = s.PlayerCell + d.ToVector();

            if (s.RemainingMovementPoints <= 0)
            {
                reason = MoveRejectReason.NoPoints;
                return false;
            }

            if (g == null || !g.InBounds(target))
            {
                reason = MoveRejectReason.OutOfBounds;
                return false;
            }

            if (g.IsGap(target))
            {
                reason = MoveRejectReason.Gap;
                return false;
            }

            bool isGate = target == g.GateCell || (g.GetTile(target) != null && g.GetTile(target).TileKind == TileKind.Gate);
            if (isGate && !s.GateOpen)
            {
                reason = MoveRejectReason.GateLocked;
                return false;
            }

            reason = MoveRejectReason.None;
            return true;
        }
    }
}
