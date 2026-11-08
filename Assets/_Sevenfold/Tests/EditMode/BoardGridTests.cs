using System.Collections.Generic;
using NUnit.Framework;
using Sevenfold.Board;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Tests
{
    [TestFixture]
    public class BoardGridTests
    {
        private BoardDefinitionSO _boardDef;
        private TileDefinitionSO _tileNormal;
        private TileDefinitionSO _tileStart;
        private TileDefinitionSO _tileGate;
        private TileDefinitionSO _tileStar;
        private TileDefinitionSO _tileTrap;
        private TileDefinitionSO _tileCharm;
        private TileDefinitionSO _tileGap;
        private BoardGrid _grid;

        [SetUp]
        public void SetUp()
        {
            _tileNormal = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileNormal.TileKind = TileKind.Normal;
            _tileNormal.StableId = "tile_normal";

            _tileStart = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileStart.TileKind = TileKind.Start;
            _tileStart.StableId = "tile_start";

            _tileGate = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileGate.TileKind = TileKind.Gate;
            _tileGate.StableId = "tile_gate";

            _tileStar = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileStar.TileKind = TileKind.Star;
            _tileStar.StableId = "tile_star";

            _tileTrap = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileTrap.TileKind = TileKind.Trap;
            _tileTrap.StableId = "tile_trap";
            _tileTrap.HiddenUntilRevealed = true;

            _tileCharm = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileCharm.TileKind = TileKind.Charm;
            _tileCharm.StableId = "tile_charm";
            _tileCharm.HiddenUntilRevealed = true;

            _tileGap = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileGap.TileKind = TileKind.Gap;
            _tileGap.StableId = "tile_gap";

            _boardDef = ScriptableObject.CreateInstance<BoardDefinitionSO>();
            _boardDef.Width = 7;
            _boardDef.Height = 7;
            _boardDef.CellSpacing = 1.25f;
            _boardDef.TileTopSize = 1.12f;
            _boardDef.StartCell = new Vector2Int(0, 0);
            _boardDef.GateCell = new Vector2Int(6, 6);
            _boardDef.DefaultNormalTile = _tileNormal;

            _boardDef.Cells.Clear();
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(0, 0), _tileStart));
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(6, 6), _tileGate));

            // Stars (7)
            Vector2Int[] stars = {
                new Vector2Int(1, 6), new Vector2Int(5, 6), new Vector2Int(3, 5),
                new Vector2Int(0, 4), new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(2, 0)
            };
            foreach (var s in stars) _boardDef.Cells.Add(new CellEntry(s, _tileStar));

            // Traps (4)
            Vector2Int[] traps = {
                new Vector2Int(4, 5), new Vector2Int(2, 4), new Vector2Int(1, 1), new Vector2Int(4, 1)
            };
            foreach (var t in traps) _boardDef.Cells.Add(new CellEntry(t, _tileTrap));

            // Charms (2)
            Vector2Int[] charms = { new Vector2Int(1, 5), new Vector2Int(6, 1) };
            foreach (var c in charms) _boardDef.Cells.Add(new CellEntry(c, _tileCharm));

            // Gaps (4): (2,5), (4,4), (5,3), (3,1)
            Vector2Int[] gaps = {
                new Vector2Int(2, 5), new Vector2Int(4, 4), new Vector2Int(5, 3), new Vector2Int(3, 1)
            };
            foreach (var g in gaps) _boardDef.Cells.Add(new CellEntry(g, _tileGap));

            _grid = new BoardGrid(_boardDef);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_boardDef);
            Object.DestroyImmediate(_tileNormal);
            Object.DestroyImmediate(_tileStart);
            Object.DestroyImmediate(_tileGate);
            Object.DestroyImmediate(_tileStar);
            Object.DestroyImmediate(_tileTrap);
            Object.DestroyImmediate(_tileCharm);
            Object.DestroyImmediate(_tileGap);
        }

        [Test]
        public void InBounds_InsideGrid_ReturnsTrue()
        {
            Assert.IsTrue(_grid.InBounds(new Vector2Int(0, 0)));
            Assert.IsTrue(_grid.InBounds(new Vector2Int(6, 6)));
            Assert.IsTrue(_grid.InBounds(new Vector2Int(3, 3)));
            Assert.IsTrue(_grid.InBounds(new Vector2Int(0, 6)));
            Assert.IsTrue(_grid.InBounds(new Vector2Int(6, 0)));
        }

        [Test]
        public void InBounds_OutsideGrid_ReturnsFalse()
        {
            Assert.IsFalse(_grid.InBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(_grid.InBounds(new Vector2Int(0, -1)));
            Assert.IsFalse(_grid.InBounds(new Vector2Int(7, 0)));
            Assert.IsFalse(_grid.InBounds(new Vector2Int(0, 7)));
            Assert.IsFalse(_grid.InBounds(new Vector2Int(7, 7)));
            Assert.IsFalse(_grid.InBounds(new Vector2Int(100, 100)));
        }

        [Test]
        public void IsGap_GapTile_ReturnsTrue()
        {
            Assert.IsTrue(_grid.IsGap(new Vector2Int(2, 5)));
            Assert.IsTrue(_grid.IsGap(new Vector2Int(4, 4)));
            Assert.IsTrue(_grid.IsGap(new Vector2Int(5, 3)));
            Assert.IsTrue(_grid.IsGap(new Vector2Int(3, 1)));
        }

        [Test]
        public void IsGap_NonGapTile_ReturnsFalse()
        {
            Assert.IsFalse(_grid.IsGap(new Vector2Int(0, 0)));
            Assert.IsFalse(_grid.IsGap(new Vector2Int(6, 6)));
            Assert.IsFalse(_grid.IsGap(new Vector2Int(3, 3)));
            Assert.IsFalse(_grid.IsGap(new Vector2Int(1, 6)));
        }

        [Test]
        public void IsGap_OutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(_grid.IsGap(new Vector2Int(-1, 0)));
            Assert.IsFalse(_grid.IsGap(new Vector2Int(7, 7)));
        }

        [Test]
        public void GetNeighbours_OriginCell_ReturnsExactlyTwoCells()
        {
            // At (0,0), Left (-1,0) and Down (0,-1) are out of bounds.
            // Up (0,1) and Right (1,0) are valid non-gap cells.
            var neighbours = _grid.GetNeighbours(new Vector2Int(0, 0));

            Assert.AreEqual(2, neighbours.Count, "Origin (0,0) must have exactly 2 neighbours.");
            CollectionAssert.Contains(neighbours, new Vector2Int(0, 1));
            CollectionAssert.Contains(neighbours, new Vector2Int(1, 0));
        }

        [Test]
        public void GetNeighbours_NeighboursNeverContainGaps()
        {
            // (2,4) is adjacent to Gap at (2,5)
            var neighboursOf24 = _grid.GetNeighbours(new Vector2Int(2, 4));
            CollectionAssert.DoesNotContain(neighboursOf24, new Vector2Int(2, 5));

            // (3,4) is adjacent to Gap at (4,4)
            var neighboursOf34 = _grid.GetNeighbours(new Vector2Int(3, 4));
            CollectionAssert.DoesNotContain(neighboursOf34, new Vector2Int(4, 4));

            // (5,4) is adjacent to Gap at (4,4) and (5,3)
            var neighboursOf54 = _grid.GetNeighbours(new Vector2Int(5, 4));
            CollectionAssert.DoesNotContain(neighboursOf54, new Vector2Int(4, 4));
            CollectionAssert.DoesNotContain(neighboursOf54, new Vector2Int(5, 3));
        }

        [Test]
        public void CellToLocal_CentreCell_ReturnsZeroYZero()
        {
            // On a 7x7 board, centre cell is (3,3)
            // localX = (3 - (7-1)/2f) * 1.25 = (3 - 3) * 1.25 = 0
            // localZ = (3 - (7-1)/2f) * 1.25 = (3 - 3) * 1.25 = 0
            // localY = heightOffset = 0
            Vector3 centreLocal = _grid.CellToLocal(new Vector2Int(3, 3));

            Assert.AreEqual(0f, centreLocal.x, 0.0001f);
            Assert.AreEqual(0f, centreLocal.z, 0.0001f);
            Assert.AreEqual(0f, centreLocal.y, 0.0001f);
        }

        [Test]
        public void CellToLocal_Corners_AreSymmetricAroundOrigin()
        {
            Vector3 bottomLeft = _grid.CellToLocal(new Vector2Int(0, 0));
            Vector3 topRight = _grid.CellToLocal(new Vector2Int(6, 6));

            Assert.AreEqual(-topRight.x, bottomLeft.x, 0.0001f);
            Assert.AreEqual(-topRight.z, bottomLeft.z, 0.0001f);
        }

        [Test]
        public void CellToLocal_HeightOffset_IsAppliedToLocalY()
        {
            // Add custom entry with height offset
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(3, 3), _tileNormal, 0.75f));

            Vector3 elevated = _grid.CellToLocal(new Vector2Int(3, 3));
            Assert.AreEqual(0.75f, elevated.y, 0.0001f);
        }

        [Test]
        public void GetTile_ReturnsCorrectTileDefinition()
        {
            Assert.AreEqual(TileKind.Start, _grid.GetTile(new Vector2Int(0, 0)).TileKind);
            Assert.AreEqual(TileKind.Gate, _grid.GetTile(new Vector2Int(6, 6)).TileKind);
            Assert.AreEqual(TileKind.Star, _grid.GetTile(new Vector2Int(1, 6)).TileKind);
            Assert.AreEqual(TileKind.Trap, _grid.GetTile(new Vector2Int(4, 5)).TileKind);
            Assert.AreEqual(TileKind.Charm, _grid.GetTile(new Vector2Int(1, 5)).TileKind);
            Assert.AreEqual(TileKind.Gap, _grid.GetTile(new Vector2Int(2, 5)).TileKind);
            Assert.AreEqual(TileKind.Normal, _grid.GetTile(new Vector2Int(3, 3)).TileKind);
        }

        [Test]
        public void BoardGrid_NullBoardDefinition_HandledDefensively()
        {
            var emptyGrid = new BoardGrid(null);

            Assert.AreEqual(0, emptyGrid.Width);
            Assert.AreEqual(0, emptyGrid.Height);
            Assert.IsFalse(emptyGrid.InBounds(new Vector2Int(0, 0)));
            Assert.IsFalse(emptyGrid.IsGap(new Vector2Int(0, 0)));
            Assert.IsNull(emptyGrid.GetTile(new Vector2Int(0, 0)));
            Assert.AreEqual(0, emptyGrid.GetNeighbours(new Vector2Int(0, 0)).Count);
        }
    }
}
