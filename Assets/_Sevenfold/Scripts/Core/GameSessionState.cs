using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sevenfold.Core
{
    public class GameSessionState
    {
        public int TurnNumber { get; set; }
        public int Hearts { get; set; }
        public int StarsCollected { get; set; }
        public int Charms { get; set; }
        public int WardCharges { get; set; }
        public int RemainingMovementPoints { get; set; }
        public int LastDieA { get; set; }
        public int LastDieB { get; set; }
        public int LastRollTotal { get; set; }
        public bool GateOpen { get; set; }
        public Vector2Int PlayerCell { get; set; }
        public HashSet<string> CollectedPickupIds { get; set; } = new HashSet<string>();
        public HashSet<string> ConsumedTrapIds { get; set; } = new HashSet<string>();
        public HashSet<Vector2Int> RevealedCells { get; set; } = new HashSet<Vector2Int>();
        public DateTime RunStartTime { get; set; }

        public static GameSessionState CreateNew(GameConfigSO config, BoardDefinitionSO board)
        {
            var state = new GameSessionState
            {
                TurnNumber = 0,
                Hearts = config != null ? config.StartingHearts : 3,
                StarsCollected = 0,
                Charms = config != null ? config.StartingCharms : 1,
                WardCharges = 0,
                RemainingMovementPoints = 0,
                LastDieA = 0,
                LastDieB = 0,
                LastRollTotal = 0,
                GateOpen = false,
                PlayerCell = board != null ? board.StartCell : Vector2Int.zero,
                RunStartTime = DateTime.UtcNow
            };

            int width = board != null ? board.Width : 7;
            int height = board != null ? board.Height : 7;
            RevealAround(state.RevealedCells, state.PlayerCell, 1, width, height);

            return state;
        }

        public static void RevealAround(HashSet<Vector2Int> revealedCells, Vector2Int center, int radius, int width, int height)
        {
            if (revealedCells == null) return;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) <= radius)
                    {
                        var coord = new Vector2Int(center.x + dx, center.y + dy);
                        if (coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height)
                        {
                            revealedCells.Add(coord);
                        }
                    }
                }
            }
        }
    }
}
