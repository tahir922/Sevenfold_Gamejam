using System;
using System.Collections.Generic;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Board
{
    /// <summary>
    /// Pure C# grid representation constructed from a BoardDefinitionSO.
    /// Provides spatial queries, bounds checking, gap evaluation, and coordinate transformations.
    /// </summary>
    public class BoardGrid
    {
        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        private readonly BoardDefinitionSO _boardDef;

        public BoardDefinitionSO Definition => _boardDef;
        public int Width => _boardDef != null ? _boardDef.Width : 0;
        public int Height => _boardDef != null ? _boardDef.Height : 0;
        public float CellSpacing => _boardDef != null ? _boardDef.CellSpacing : 1.25f;
        public float TileTopSize => _boardDef != null ? _boardDef.TileTopSize : 1.12f;
        public Vector2Int StartCell => _boardDef != null ? _boardDef.StartCell : Vector2Int.zero;
        public Vector2Int GateCell => _boardDef != null ? _boardDef.GateCell : Vector2Int.zero;

        public BoardGrid(BoardDefinitionSO boardDef)
        {
            _boardDef = boardDef;
        }

        /// <summary>
        /// Checks if coordinate c falls within the board boundaries [0, Width) x [0, Height).
        /// </summary>
        public bool InBounds(Vector2Int c)
        {
            return c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;
        }

        /// <summary>
        /// Checks if coordinate c corresponds to an impassable Gap tile.
        /// Returns false if out of bounds.
        /// </summary>
        public bool IsGap(Vector2Int c)
        {
            if (!InBounds(c) || _boardDef == null)
            {
                return false;
            }

            var tile = GetTile(c);
            return tile != null && tile.TileKind == TileKind.Gap;
        }

        /// <summary>
        /// Gets the tile definition assigned at coordinate c.
        /// Returns null if out of bounds.
        /// </summary>
        public TileDefinitionSO GetTile(Vector2Int c)
        {
            if (!InBounds(c) || _boardDef == null)
            {
                return null;
            }

            return _boardDef.GetTileAt(c);
        }

        /// <summary>
        /// Gets the height offset assigned at coordinate c.
        /// </summary>
        public float GetHeightOffset(Vector2Int c)
        {
            if (!InBounds(c) || _boardDef == null)
            {
                return 0f;
            }

            return _boardDef.TryGetCellEntry(c, out var entry) ? entry.heightOffset : 0f;
        }

        /// <summary>
        /// Returns valid 4-directional adjacent coordinates that are within board bounds and not Gaps.
        /// </summary>
        public List<Vector2Int> GetNeighbours(Vector2Int c)
        {
            var neighbours = new List<Vector2Int>(4);
            if (!InBounds(c))
            {
                return neighbours;
            }

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                var target = c + NeighborOffsets[i];
                if (InBounds(target) && !IsGap(target))
                {
                    neighbours.Add(target);
                }
            }

            return neighbours;
        }

        /// <summary>
        /// Converts a board coordinate into local 3D position using the centered formula:
        /// localX = (x - (w-1)/2f) * spacing
        /// localZ = (y - (h-1)/2f) * spacing
        /// localY = heightOffset
        /// </summary>
        public Vector3 CellToLocal(Vector2Int c)
        {
            float localX = (c.x - (Width - 1) / 2f) * CellSpacing;
            float localZ = (c.y - (Height - 1) / 2f) * CellSpacing;
            float localY = GetHeightOffset(c);
            return new Vector3(localX, localY, localZ);
        }
    }
}
