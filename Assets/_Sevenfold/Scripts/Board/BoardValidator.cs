using System.Collections.Generic;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Board
{
    public static class BoardValidator
    {
        public static List<string> Validate(BoardDefinitionSO board, GameConfigSO config)
        {
            var errors = new List<string>();

            if (board == null)
            {
                errors.Add("BoardDefinitionSO is null.");
                return errors;
            }

            if (config == null)
            {
                errors.Add("GameConfigSO is null.");
                return errors;
            }

            if (board.Width <= 0 || board.Height <= 0)
            {
                errors.Add($"Board dimensions must be positive (got {board.Width}x{board.Height}).");
            }

            if (board.DefaultNormalTile == null)
            {
                errors.Add("BoardDefinitionSO defaultNormalTile is not assigned.");
            }
            else if (board.DefaultNormalTile.TileKind != TileKind.Normal)
            {
                errors.Add($"BoardDefinitionSO defaultNormalTile must have TileKind.Normal (was {board.DefaultNormalTile.TileKind}).");
            }

            // 1. Start and Gate in bounds and not gaps or traps
            bool startInBounds = board.Width > 0 && board.Height > 0 &&
                                 board.StartCell.x >= 0 && board.StartCell.x < board.Width &&
                                 board.StartCell.y >= 0 && board.StartCell.y < board.Height;
            if (!startInBounds)
            {
                errors.Add($"Start cell {board.StartCell} is out of bounds ({board.Width}x{board.Height}).");
            }

            bool gateInBounds = board.Width > 0 && board.Height > 0 &&
                                board.GateCell.x >= 0 && board.GateCell.x < board.Width &&
                                board.GateCell.y >= 0 && board.GateCell.y < board.Height;
            if (!gateInBounds)
            {
                errors.Add($"Gate cell {board.GateCell} is out of bounds ({board.Width}x{board.Height}).");
            }

            if (startInBounds && gateInBounds && board.StartCell == board.GateCell)
            {
                errors.Add($"Start cell and Gate cell cannot be at the same coordinate ({board.StartCell}).");
            }

            if (startInBounds)
            {
                var startTile = board.GetTileAt(board.StartCell);
                if (startTile != null && startTile.TileKind == TileKind.Gap)
                {
                    errors.Add($"Start cell {board.StartCell} is a Gap tile.");
                }
                else if (startTile != null && startTile.TileKind == TileKind.Trap)
                {
                    errors.Add($"Start cell {board.StartCell} is a Trap tile.");
                }
            }

            if (gateInBounds)
            {
                var gateTile = board.GetTileAt(board.GateCell);
                if (gateTile != null && gateTile.TileKind == TileKind.Gap)
                {
                    errors.Add($"Gate cell {board.GateCell} is a Gap tile.");
                }
                else if (gateTile != null && gateTile.TileKind == TileKind.Trap)
                {
                    errors.Add($"Gate cell {board.GateCell} is a Trap tile.");
                }
            }

            // 2. Validate cell entries in board.Cells (bounds, unassigned tiles, duplicate coordinates)
            var seenCoords = new HashSet<Vector2Int>();
            if (board.Cells != null)
            {
                for (int i = 0; i < board.Cells.Count; i++)
                {
                    var entry = board.Cells[i];
                    var cellCoord = entry.coord;

                    if (board.Width > 0 && board.Height > 0 &&
                        (cellCoord.x < 0 || cellCoord.x >= board.Width || cellCoord.y < 0 || cellCoord.y >= board.Height))
                    {
                        errors.Add($"Cell entry at {cellCoord} is out of bounds ({board.Width}x{board.Height}).");
                    }

                    if (entry.tile == null)
                    {
                        errors.Add($"Cell entry at {cellCoord} has unassigned TileDefinitionSO.");
                    }

                    if (!seenCoords.Add(cellCoord))
                    {
                        errors.Add($"Duplicate cell coordinate found at {cellCoord}.");
                    }
                }
            }

            // 3. Star count == starsRequired & locate points of interest
            int starCount = 0;
            var starCoords = new List<Vector2Int>();
            var charmCoords = new List<Vector2Int>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var coord = new Vector2Int(x, y);
                    var tile = board.GetTileAt(coord);
                    if (tile != null)
                    {
                        if (tile.TileKind == TileKind.Star)
                        {
                            starCount++;
                            starCoords.Add(coord);
                        }
                        else if (tile.TileKind == TileKind.Charm)
                        {
                            charmCoords.Add(coord);
                        }
                    }
                }
            }

            if (starCount != config.StarsRequired)
            {
                errors.Add($"Star count mismatch: found {starCount} star tiles, expected {config.StarsRequired}.");
            }

            // Orthogonal neighbour vectors
            Vector2Int[] orthogonalDirs = {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            // 4. Reachability from Start via BFS through non-gap cells
            if (startInBounds)
            {
                var reachable = new HashSet<Vector2Int>();
                var queue = new Queue<Vector2Int>();

                var startTile = board.GetTileAt(board.StartCell);
                if (startTile == null || startTile.TileKind != TileKind.Gap)
                {
                    reachable.Add(board.StartCell);
                    queue.Enqueue(board.StartCell);
                }

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var dir in orthogonalDirs)
                    {
                        var next = current + dir;
                        if (next.x < 0 || next.x >= board.Width || next.y < 0 || next.y >= board.Height)
                        {
                            continue;
                        }

                        if (reachable.Contains(next))
                        {
                            continue;
                        }

                        var nextTile = board.GetTileAt(next);
                        if (nextTile != null && nextTile.TileKind == TileKind.Gap)
                        {
                            continue;
                        }

                        // Cell is enterable (including Gate)
                        reachable.Add(next);
                        queue.Enqueue(next);
                    }
                }

                if (gateInBounds && !reachable.Contains(board.GateCell))
                {
                    errors.Add($"Gate cell {board.GateCell} is not reachable from start {board.StartCell}.");
                }

                foreach (var star in starCoords)
                {
                    if (!reachable.Contains(star))
                    {
                        errors.Add($"Star tile at {star} is not reachable from start {board.StartCell}.");
                    }
                }

                foreach (var charm in charmCoords)
                {
                    if (!reachable.Contains(charm))
                    {
                        errors.Add($"Charm tile at {charm} is not reachable from start {board.StartCell}.");
                    }
                }
            }

            // 5. No trap orthogonally adjacent to start
            if (startInBounds)
            {
                foreach (var dir in orthogonalDirs)
                {
                    var neighbor = board.StartCell + dir;
                    if (neighbor.x >= 0 && neighbor.x < board.Width && neighbor.y >= 0 && neighbor.y < board.Height)
                    {
                        var neighborTile = board.GetTileAt(neighbor);
                        if (neighborTile != null && neighborTile.TileKind == TileKind.Trap)
                        {
                            errors.Add($"Trap tile at {neighbor} is orthogonally adjacent to start {board.StartCell}.");
                        }
                    }
                }
            }

            // 6. Gate has at least one non-trap, non-gap neighbour
            if (gateInBounds)
            {
                bool hasSafeNeighbour = false;
                foreach (var dir in orthogonalDirs)
                {
                    var neighbor = board.GateCell + dir;
                    if (neighbor.x >= 0 && neighbor.x < board.Width && neighbor.y >= 0 && neighbor.y < board.Height)
                    {
                        var neighborTile = board.GetTileAt(neighbor);
                        if (neighborTile != null && neighborTile.TileKind != TileKind.Trap && neighborTile.TileKind != TileKind.Gap)
                        {
                            hasSafeNeighbour = true;
                            break;
                        }
                    }
                }

                if (!hasSafeNeighbour)
                {
                    errors.Add($"Gate at {board.GateCell} has no non-trap, non-gap orthogonal neighbours.");
                }
            }

            return errors;
        }
    }
}
