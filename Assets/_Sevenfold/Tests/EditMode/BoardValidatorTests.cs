using System.Collections.Generic;
using NUnit.Framework;
using Sevenfold.Board;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Tests
{
    public class BoardValidatorTests
    {
        private GameConfigSO _config;
        private TileDefinitionSO _normalTile;
        private TileDefinitionSO _starTile;
        private TileDefinitionSO _trapTile;
        private TileDefinitionSO _charmTile;
        private TileDefinitionSO _gapTile;
        private TileDefinitionSO _gateTile;
        private TileDefinitionSO _startTile;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameConfigSO>();
            _config.BoardWidth = 7;
            _config.BoardHeight = 7;
            _config.StarsRequired = 7;
            _config.StartingHearts = 3;
            _config.StartingCharms = 1;
            _config.MaxCharms = 3;
            _config.MaxTurns = 7;
            _config.DiceCount = 2;
            _config.DiceSides = 6;

            _normalTile = CreateTile("normal", TileKind.Normal);
            _starTile = CreateTile("star", TileKind.Star);
            _trapTile = CreateTile("trap", TileKind.Trap);
            _charmTile = CreateTile("charm", TileKind.Charm);
            _gapTile = CreateTile("gap", TileKind.Gap);
            _gateTile = CreateTile("gate", TileKind.Gate);
            _startTile = CreateTile("start", TileKind.Start);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_normalTile);
            Object.DestroyImmediate(_starTile);
            Object.DestroyImmediate(_trapTile);
            Object.DestroyImmediate(_charmTile);
            Object.DestroyImmediate(_gapTile);
            Object.DestroyImmediate(_gateTile);
            Object.DestroyImmediate(_startTile);
        }

        private TileDefinitionSO CreateTile(string id, TileKind kind)
        {
            var tile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            tile.StableId = id;
            tile.DisplayName = id;
            tile.TileKind = kind;
            return tile;
        }

        private BoardDefinitionSO CreateDefaultBoard()
        {
            var board = ScriptableObject.CreateInstance<BoardDefinitionSO>();
            board.Width = 7;
            board.Height = 7;
            board.StartCell = new Vector2Int(0, 0);
            board.GateCell = new Vector2Int(6, 6);
            board.DefaultNormalTile = _normalTile;

            board.Cells.Add(new CellEntry(new Vector2Int(0, 0), _startTile));
            board.Cells.Add(new CellEntry(new Vector2Int(6, 6), _gateTile));

            // Stars (7): (1,6) (5,6) (3,5) (0,4) (1,2) (3,2) (2,0)
            Vector2Int[] stars = {
                new Vector2Int(1, 6),
                new Vector2Int(5, 6),
                new Vector2Int(3, 5),
                new Vector2Int(0, 4),
                new Vector2Int(1, 2),
                new Vector2Int(3, 2),
                new Vector2Int(2, 0)
            };
            foreach (var star in stars)
            {
                board.Cells.Add(new CellEntry(star, _starTile));
            }

            // Traps (4): (4,5) (2,4) (1,1) (4,1)
            Vector2Int[] traps = {
                new Vector2Int(4, 5),
                new Vector2Int(2, 4),
                new Vector2Int(1, 1),
                new Vector2Int(4, 1)
            };
            foreach (var trap in traps)
            {
                board.Cells.Add(new CellEntry(trap, _trapTile));
            }

            // Charms (2): (1,5) (6,1)
            Vector2Int[] charms = {
                new Vector2Int(1, 5),
                new Vector2Int(6, 1)
            };
            foreach (var charm in charms)
            {
                board.Cells.Add(new CellEntry(charm, _charmTile));
            }

            // Gaps (4): (2,5) (4,4) (5,3) (3,1)
            Vector2Int[] gaps = {
                new Vector2Int(2, 5),
                new Vector2Int(4, 4),
                new Vector2Int(5, 3),
                new Vector2Int(3, 1)
            };
            foreach (var gap in gaps)
            {
                board.Cells.Add(new CellEntry(gap, _gapTile));
            }

            return board;
        }

        [Test]
        public void DefaultLayout_PassesValidation()
        {
            var board = CreateDefaultBoard();
            var errors = BoardValidator.Validate(board, _config);

            Assert.AreEqual(0, errors.Count, "Default board should pass with 0 errors. Found: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void StarWalledInByGaps_FailsValidation()
        {
            var board = CreateDefaultBoard();

            // Star at (0, 4) walled in by gaps at (0, 3), (0, 5), and (1, 4)
            board.Cells.Add(new CellEntry(new Vector2Int(0, 3), _gapTile));
            board.Cells.Add(new CellEntry(new Vector2Int(0, 5), _gapTile));
            board.Cells.Add(new CellEntry(new Vector2Int(1, 4), _gapTile));

            var errors = BoardValidator.Validate(board, _config);

            Assert.IsTrue(errors.Exists(e => e.Contains("is not reachable from start")),
                "Expected reachability error for walled-in star. Errors: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void DuplicateCoordinate_FailsValidation()
        {
            var board = CreateDefaultBoard();
            // Add a duplicate coordinate entry
            board.Cells.Add(new CellEntry(new Vector2Int(1, 6), _starTile));

            var errors = BoardValidator.Validate(board, _config);

            Assert.IsTrue(errors.Exists(e => e.Contains("Duplicate cell coordinate")),
                "Expected duplicate coordinate error. Errors: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void TrapAdjacentToStart_FailsValidation()
        {
            var board = CreateDefaultBoard();
            // Place a trap at (0, 1), which is orthogonally adjacent to (0, 0)
            board.Cells.Add(new CellEntry(new Vector2Int(0, 1), _trapTile));

            var errors = BoardValidator.Validate(board, _config);

            Assert.IsTrue(errors.Exists(e => e.Contains("orthogonally adjacent to start")),
                "Expected adjacent trap error. Errors: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void GateWithOnlyTrapOrGapNeighbours_FailsValidation()
        {
            var board = CreateDefaultBoard();
            // Gate is at (6, 6). Orthogonal neighbours are (5, 6) and (6, 5).
            // (5, 6) is a star in default layout; turn it into a Gap and (6, 5) into a Trap.
            board.Cells.RemoveAll(c => c.coord == new Vector2Int(5, 6));
            board.Cells.Add(new CellEntry(new Vector2Int(5, 6), _gapTile));
            board.Cells.Add(new CellEntry(new Vector2Int(6, 5), _trapTile));

            // Move the star that was at (5, 6) to (5, 5) to keep star count valid
            board.Cells.Add(new CellEntry(new Vector2Int(5, 5), _starTile));

            var errors = BoardValidator.Validate(board, _config);

            Assert.IsTrue(errors.Exists(e => e.Contains("has no non-trap, non-gap orthogonal neighbours")),
                "Expected gate safe neighbour error. Errors: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void StarCountMismatch_FailsValidation()
        {
            var board = CreateDefaultBoard();
            // Remove one star so there are only 6
            board.Cells.RemoveAll(c => c.coord == new Vector2Int(1, 6));

            var errors = BoardValidator.Validate(board, _config);

            Assert.IsTrue(errors.Exists(e => e.Contains("Star count mismatch")),
                "Expected star count mismatch error. Errors: " + string.Join("; ", errors));
            Object.DestroyImmediate(board);
        }

        [Test]
        public void StartCell_OutOfBoundsOrGap_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.StartCell = new Vector2Int(-1, 0);
            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Start cell (-1, 0) is out of bounds")),
                "Expected start out of bounds error. Errors: " + string.Join("; ", errors));

            board.StartCell = new Vector2Int(0, 0);
            board.Cells.RemoveAll(c => c.coord == new Vector2Int(0, 0));
            board.Cells.Add(new CellEntry(new Vector2Int(0, 0), _gapTile));
            errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Start cell (0, 0) is a Gap tile")),
                "Expected start gap error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void Charm_WalledInByGaps_FailsValidation()
        {
            var board = CreateDefaultBoard();
            // Charm at (1, 5) surrounded by gaps at (1, 4), (0, 5), (2, 5 is already gap), (1, 6)
            board.Cells.Add(new CellEntry(new Vector2Int(1, 4), _gapTile));
            board.Cells.Add(new CellEntry(new Vector2Int(0, 5), _gapTile));
            board.Cells.RemoveAll(c => c.coord == new Vector2Int(1, 6));
            board.Cells.Add(new CellEntry(new Vector2Int(1, 6), _gapTile));
            // Move star that was at (1, 6) to (0, 6) so star count is still 7
            board.Cells.Add(new CellEntry(new Vector2Int(0, 6), _starTile));

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Charm tile at (1, 5) is not reachable from start")),
                "Expected unreachable charm error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void NullBoardOrConfig_ReturnsError()
        {
            var errorsBoardNull = BoardValidator.Validate(null, _config);
            Assert.AreEqual(1, errorsBoardNull.Count);
            Assert.IsTrue(errorsBoardNull[0].Contains("BoardDefinitionSO is null"));

            var board = CreateDefaultBoard();
            var errorsConfigNull = BoardValidator.Validate(board, null);
            Assert.AreEqual(1, errorsConfigNull.Count);
            Assert.IsTrue(errorsConfigNull[0].Contains("GameConfigSO is null"));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void GameSessionState_CreateNew_InitializesCorrectly()
        {
            var board = CreateDefaultBoard();
            var state = GameSessionState.CreateNew(_config, board);

            Assert.AreEqual(0, state.TurnNumber);
            Assert.AreEqual(3, state.Hearts);
            Assert.AreEqual(1, state.Charms);
            Assert.AreEqual(0, state.WardCharges);
            Assert.AreEqual(0, state.StarsCollected);
            Assert.AreEqual(new Vector2Int(0, 0), state.PlayerCell);
            Assert.IsFalse(state.GateOpen);

            // Start cell (0, 0) and orthogonal neighbours (1, 0) and (0, 1) should be revealed
            Assert.IsTrue(state.RevealedCells.Contains(new Vector2Int(0, 0)));
            Assert.IsTrue(state.RevealedCells.Contains(new Vector2Int(1, 0)));
            Assert.IsTrue(state.RevealedCells.Contains(new Vector2Int(0, 1)));
            Assert.IsFalse(state.RevealedCells.Contains(new Vector2Int(1, 1)), "Diagonal cell should not be in radius 1 Manhattan");

            Object.DestroyImmediate(board);
        }

        [Test]
        public void DirectionExt_ToVector_ReturnsCorrectVectors()
        {
            Assert.AreEqual(new Vector2Int(0, 1), Direction.Up.ToVector());
            Assert.AreEqual(new Vector2Int(0, -1), Direction.Down.ToVector());
            Assert.AreEqual(new Vector2Int(-1, 0), Direction.Left.ToVector());
            Assert.AreEqual(new Vector2Int(1, 0), Direction.Right.ToVector());
        }

        [Test]
        public void CellEntry_OutOfBounds_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.Cells.Add(new CellEntry(new Vector2Int(7, 7), _gapTile));

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Cell entry at (7, 7) is out of bounds")),
                "Expected out of bounds cell error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void CellEntry_UnassignedTile_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.Cells.Add(new CellEntry(new Vector2Int(2, 2), null));

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("has unassigned TileDefinitionSO")),
                "Expected unassigned tile error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void StartCell_IsTrap_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.Cells.RemoveAll(c => c.coord == board.StartCell);
            board.Cells.Add(new CellEntry(board.StartCell, _trapTile));

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Start cell (0, 0) is a Trap tile")),
                "Expected trap start error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void GateCell_IsTrap_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.Cells.RemoveAll(c => c.coord == board.GateCell);
            board.Cells.Add(new CellEntry(board.GateCell, _trapTile));

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Gate cell (6, 6) is a Trap tile")),
                "Expected trap gate error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void StartAndGate_SameCoordinate_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.GateCell = board.StartCell;

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("cannot be at the same coordinate")),
                "Expected same start/gate coordinate error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void DefaultNormalTile_InvalidKind_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.DefaultNormalTile = _trapTile;

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("defaultNormalTile must have TileKind.Normal")),
                "Expected invalid default normal tile kind error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void BoardDimensions_NonPositive_FailsValidation()
        {
            var board = CreateDefaultBoard();
            board.Width = 0;
            board.Height = -1;

            var errors = BoardValidator.Validate(board, _config);
            Assert.IsTrue(errors.Exists(e => e.Contains("Board dimensions must be positive")),
                "Expected non-positive board dimension error. Errors: " + string.Join("; ", errors));

            Object.DestroyImmediate(board);
        }

        [Test]
        public void BoardDefinitionSO_TryGetCellEntry_OutOfBounds_ReturnsFalse()
        {
            var board = CreateDefaultBoard();
            bool result = board.TryGetCellEntry(new Vector2Int(-1, 5), out var entry);

            Assert.IsFalse(result);
            Assert.IsNull(entry.tile);

            Object.DestroyImmediate(board);
        }
    }
}
