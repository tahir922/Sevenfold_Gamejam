using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sevenfold.Core
{
    [Serializable]
    public struct CellEntry
    {
        public Vector2Int coord;
        public TileDefinitionSO tile;
        public float heightOffset;

        public CellEntry(Vector2Int coord, TileDefinitionSO tile, float heightOffset = 0f)
        {
            this.coord = coord;
            this.tile = tile;
            this.heightOffset = heightOffset;
        }
    }

    [CreateAssetMenu(fileName = "Board_", menuName = "Sevenfold/Board/Board Definition")]
    public class BoardDefinitionSO : ScriptableObject
    {
        [SerializeField] private int _width = 7;
        [SerializeField] private int _height = 7;
        [SerializeField] private float _cellSpacing = 1.25f;
        [SerializeField] private float _tileTopSize = 1.12f;
        [SerializeField] private Vector2Int _startCell = new Vector2Int(0, 0);
        [SerializeField] private Vector2Int _gateCell = new Vector2Int(6, 6);
        [SerializeField] private TileDefinitionSO _defaultNormalTile;
        [SerializeField] private List<CellEntry> _cells = new List<CellEntry>();

        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        public float CellSpacing { get => _cellSpacing; set => _cellSpacing = value; }
        public float TileTopSize { get => _tileTopSize; set => _tileTopSize = value; }
        public Vector2Int StartCell { get => _startCell; set => _startCell = value; }
        public Vector2Int GateCell { get => _gateCell; set => _gateCell = value; }
        public TileDefinitionSO DefaultNormalTile { get => _defaultNormalTile; set => _defaultNormalTile = value; }
        public List<CellEntry> Cells => _cells;

        public TileDefinitionSO GetTileAt(Vector2Int coord)
        {
            if (coord.x < 0 || coord.x >= _width || coord.y < 0 || coord.y >= _height)
            {
                return null;
            }

            if (_cells != null)
            {
                for (int i = 0; i < _cells.Count; i++)
                {
                    if (_cells[i].coord == coord)
                    {
                        return _cells[i].tile != null ? _cells[i].tile : _defaultNormalTile;
                    }
                }
            }

            return _defaultNormalTile;
        }

        public bool TryGetCellEntry(Vector2Int coord, out CellEntry entry)
        {
            if (coord.x < 0 || coord.x >= _width || coord.y < 0 || coord.y >= _height)
            {
                entry = default;
                return false;
            }

            if (_cells != null)
            {
                for (int i = 0; i < _cells.Count; i++)
                {
                    if (_cells[i].coord == coord)
                    {
                        entry = _cells[i];
                        return true;
                    }
                }
            }

            entry = new CellEntry(coord, _defaultNormalTile, 0f);
            return false;
        }
    }
}
