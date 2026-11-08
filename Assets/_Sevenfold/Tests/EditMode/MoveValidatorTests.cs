using System;
using NUnit.Framework;
using Sevenfold.Board;
using Sevenfold.Core;
using Sevenfold.Player;
using UnityEngine;

namespace Sevenfold.Tests
{
    [TestFixture]
    public class MoveValidatorTests
    {
        private BoardDefinitionSO _boardDef;
        private BoardGrid _boardGrid;
        private TileDefinitionSO _normalTile;
        private TileDefinitionSO _gapTile;
        private TileDefinitionSO _gateTile;

        [SetUp]
        public void SetUp()
        {
            _normalTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _normalTile.TileKind = TileKind.Normal;

            _gapTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _gapTile.TileKind = TileKind.Gap;

            _gateTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _gateTile.TileKind = TileKind.Gate;

            _boardDef = ScriptableObject.CreateInstance<BoardDefinitionSO>();
            _boardDef.Width = 7;
            _boardDef.Height = 7;
            _boardDef.StartCell = new Vector2Int(0, 0);
            _boardDef.GateCell = new Vector2Int(6, 6);
            _boardDef.DefaultNormalTile = _normalTile;

            // Add standard test tiles:
            // Gate at (6,6)
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(6, 6), _gateTile));
            // Gap at (3, 1) and (2, 5)
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(3, 1), _gapTile));
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(2, 5), _gapTile));

            _boardGrid = new BoardGrid(_boardDef);
        }

        [TearDown]
        public void TearDown()
        {
            if (_normalTile != null) UnityEngine.Object.DestroyImmediate(_normalTile);
            if (_gapTile != null) UnityEngine.Object.DestroyImmediate(_gapTile);
            if (_gateTile != null) UnityEngine.Object.DestroyImmediate(_gateTile);
            if (_boardDef != null) UnityEngine.Object.DestroyImmediate(_boardDef);
        }

        [Test]
        public void CanMove_ValidMove_ReturnsTrueAndNoneReason()
        {
            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(0, 0),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Up, out var target, out var reason);

            Assert.IsTrue(canMove, "Valid move should return true.");
            Assert.AreEqual(new Vector2Int(0, 1), target, "Target coordinate must match Direction.Up.");
            Assert.AreEqual(MoveRejectReason.None, reason, "Reason should be None on valid move.");
        }

        [Test]
        public void CanMove_OffTheBoard_ReturnsFalseAndOutOfBounds()
        {
            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(0, 0),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            // Move Down from (0,0) -> target (0, -1)
            bool canMoveDown = MoveValidator.CanMove(state, _boardGrid, Direction.Down, out var targetDown, out var reasonDown);
            Assert.IsFalse(canMoveDown, "Moving off bottom edge must fail.");
            Assert.AreEqual(new Vector2Int(0, -1), targetDown);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonDown);

            // Move Left from (0,0) -> target (-1, 0)
            bool canMoveLeft = MoveValidator.CanMove(state, _boardGrid, Direction.Left, out var targetLeft, out var reasonLeft);
            Assert.IsFalse(canMoveLeft, "Moving off left edge must fail.");
            Assert.AreEqual(new Vector2Int(-1, 0), targetLeft);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonLeft);

            // Move Up/Right from (6,6)
            state.PlayerCell = new Vector2Int(6, 6);
            bool canMoveUp = MoveValidator.CanMove(state, _boardGrid, Direction.Up, out var targetUp, out var reasonUp);
            Assert.IsFalse(canMoveUp, "Moving off top edge must fail.");
            Assert.AreEqual(new Vector2Int(6, 7), targetUp);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonUp);

            bool canMoveRight = MoveValidator.CanMove(state, _boardGrid, Direction.Right, out var targetRight, out var reasonRight);
            Assert.IsFalse(canMoveRight, "Moving off right edge must fail.");
            Assert.AreEqual(new Vector2Int(7, 6), targetRight);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonRight);
        }

        [Test]
        public void CanMove_IntoGap_ReturnsFalseAndGap()
        {
            var state = new GameSessionState
            {
                // (3,1) is a Gap; player sits at (3,0)
                PlayerCell = new Vector2Int(3, 0),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Up, out var target, out var reason);

            Assert.IsFalse(canMove, "Moving into a Gap tile must be rejected.");
            Assert.AreEqual(new Vector2Int(3, 1), target);
            Assert.AreEqual(MoveRejectReason.Gap, reason);
        }

        [Test]
        public void CanMove_IntoLockedGate_ReturnsFalseAndGateLocked()
        {
            var state = new GameSessionState
            {
                // Gate is at (6,6); player sits at (5,6)
                PlayerCell = new Vector2Int(5, 6),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Right, out var target, out var reason);

            Assert.IsFalse(canMove, "Moving into a locked gate must be rejected.");
            Assert.AreEqual(new Vector2Int(6, 6), target);
            Assert.AreEqual(MoveRejectReason.GateLocked, reason);
        }

        [Test]
        public void CanMove_IntoUnlockedGate_ReturnsTrueAndNone()
        {
            var state = new GameSessionState
            {
                // Gate is at (6,6); player sits at (5,6)
                PlayerCell = new Vector2Int(5, 6),
                RemainingMovementPoints = 5,
                GateOpen = true
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Right, out var target, out var reason);

            Assert.IsTrue(canMove, "Moving into an opened gate must succeed.");
            Assert.AreEqual(new Vector2Int(6, 6), target);
            Assert.AreEqual(MoveRejectReason.None, reason);
        }

        [Test]
        public void CanMove_WithZeroPoints_ReturnsFalseAndNoPoints()
        {
            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(0, 0),
                RemainingMovementPoints = 0,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Up, out var target, out var reason);

            Assert.IsFalse(canMove, "Moving with 0 movement points must be rejected.");
            Assert.AreEqual(new Vector2Int(0, 1), target);
            Assert.AreEqual(MoveRejectReason.NoPoints, reason);
        }

        [Test]
        public void CanMove_WithNegativePoints_ReturnsFalseAndNoPoints()
        {
            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(2, 2),
                RemainingMovementPoints = -1,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, Direction.Right, out var target, out var reason);

            Assert.IsFalse(canMove, "Moving with negative movement points must be rejected.");
            Assert.AreEqual(MoveRejectReason.NoPoints, reason);
        }

        /// <summary>
        /// Diagonal moves are syntactically impossible because Direction only has 4 values (Up, Down, Left, Right).
        /// Direction.ToVector() only emits cardinal unit vectors (0,1), (0,-1), (-1,0), (1,0).
        /// There is no diagonal enum value in the type system, ensuring absolute safety against diagonal movement exploits.
        /// </summary>
        [Test]
        public void DirectionEnum_OnlyHasFourCardinals_DiagonalMovesAreSyntacticallyImpossible()
        {
            var enumValues = Enum.GetValues(typeof(Direction));
            Assert.AreEqual(4, enumValues.Length, "Direction enum must contain exactly 4 cardinal directions.");

            foreach (Direction dir in enumValues)
            {
                Vector2Int v = dir.ToVector();
                int manhattanDist = Math.Abs(v.x) + Math.Abs(v.y);
                Assert.AreEqual(1, manhattanDist, $"Direction {dir} vector {v} must have Manhattan distance of 1 (cardinal only).");
                Assert.IsTrue(v.x == 0 || v.y == 0, $"Direction {dir} vector {v} cannot have non-zero x and y simultaneously (no diagonal).");
            }
        }

        [Test]
        public void CanMove_NullStateOrGrid_ReturnsFalseDefensively()
        {
            bool canMoveNullState = MoveValidator.CanMove(null, _boardGrid, Direction.Up, out _, out var reasonNullState);
            Assert.IsFalse(canMoveNullState);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonNullState);

            var state = new GameSessionState { PlayerCell = Vector2Int.zero, RemainingMovementPoints = 1 };
            bool canMoveNullGrid = MoveValidator.CanMove(state, null, Direction.Up, out _, out var reasonNullGrid);
            Assert.IsFalse(canMoveNullGrid);
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reasonNullGrid);
        }

        [Test]
        public void PlayerPresentationSO_DefaultValues_MatchGameJamContract()
        {
            var presentation = ScriptableObject.CreateInstance<PlayerPresentationSO>();
            Assert.AreEqual(0.22f, presentation.MoveDuration, 0.001f);
            Assert.AreEqual(0.12f, presentation.HopHeight, 0.001f);
            Assert.AreEqual(0.1f, presentation.LandingSquash, 0.001f);
            Assert.AreEqual(0.1f, presentation.LandingDuration, 0.001f);
            Assert.AreEqual(0.03f, presentation.IdleBobAmount, 0.001f);
            Assert.AreEqual(1.5f, presentation.IdleBobSpeed, 0.001f);
            UnityEngine.Object.DestroyImmediate(presentation);
        }

        [Test]
        public void InputConfigSO_DefaultBindings_AreConfigured()
        {
            var inputConfig = ScriptableObject.CreateInstance<InputConfigSO>();
            Assert.IsNotNull(inputConfig.RollBindings);
            Assert.Contains("<Keyboard>/space", inputConfig.RollBindings);

            Assert.IsNotNull(inputConfig.UpBindings);
            Assert.Contains("<Keyboard>/w", inputConfig.UpBindings);
            Assert.Contains("<Keyboard>/upArrow", inputConfig.UpBindings);

            Assert.IsNotNull(inputConfig.DownBindings);
            Assert.Contains("<Keyboard>/s", inputConfig.DownBindings);

            Assert.IsNotNull(inputConfig.LeftBindings);
            Assert.Contains("<Keyboard>/a", inputConfig.LeftBindings);

            Assert.IsNotNull(inputConfig.RightBindings);
            Assert.Contains("<Keyboard>/d", inputConfig.RightBindings);

            Assert.IsNotNull(inputConfig.EndTurnBindings);
            Assert.Contains("<Keyboard>/enter", inputConfig.EndTurnBindings);

            Assert.IsNotNull(inputConfig.UseCharmBindings);
            Assert.Contains("<Keyboard>/c", inputConfig.UseCharmBindings);

            Assert.IsNotNull(inputConfig.PauseBindings);
            Assert.Contains("<Keyboard>/escape", inputConfig.PauseBindings);
            UnityEngine.Object.DestroyImmediate(inputConfig);
        }

        [Test]
        public void CanMove_UndefinedDirectionEnum_ReturnsFalseAndOutOfBounds()
        {
            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(2, 2),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            bool canMove = MoveValidator.CanMove(state, _boardGrid, (Direction)99, out var target, out var reason);

            Assert.IsFalse(canMove, "Undefined enum value must be rejected.");
            Assert.AreEqual(state.PlayerCell, target, "Target should stay at current cell on undefined direction.");
            Assert.AreEqual(MoveRejectReason.OutOfBounds, reason, "Reason should be OutOfBounds for undefined direction.");
        }

        [Test]
        public void PlayerGridMover_ValidMove_SpendsPointUpdatesCellAndFiresEvents()
        {
            var go = new GameObject("TestPlayer");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            var pres = ScriptableObject.CreateInstance<PlayerPresentationSO>();
            var mover = go.AddComponent<PlayerGridMover>();
            mover.Presentation = pres;
            mover.VisualChild = visual.transform;

            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(0, 0),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            mover.Initialize(state, _boardGrid);

            Vector2Int startedFrom = Vector2Int.zero;
            Vector2Int startedTo = Vector2Int.zero;
            Vector2Int enteredCell = Vector2Int.zero;
            bool moveCompletedFired = false;

            mover.OnMoveStarted += (from, to) => { startedFrom = from; startedTo = to; };
            mover.OnCellEntered += cell => { enteredCell = cell; };
            mover.OnMoveCompleted += () => { moveCompletedFired = true; };

            bool moved = mover.TryMove(Direction.Up);

            Assert.IsTrue(moved, "Valid move should succeed.");
            Assert.AreEqual(4, state.RemainingMovementPoints, "Points should decrement by 1.");
            Assert.AreEqual(new Vector2Int(0, 1), state.PlayerCell, "PlayerCell should update to (0, 1).");
            Assert.AreEqual(new Vector2Int(0, 0), startedFrom);
            Assert.AreEqual(new Vector2Int(0, 1), startedTo);
            Assert.AreEqual(new Vector2Int(0, 1), enteredCell);
            Assert.IsTrue(moveCompletedFired);

            UnityEngine.Object.DestroyImmediate(pres);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerGridMover_InvalidMove_SpendsNoPointsAndRaisesMoveRejected()
        {
            var go = new GameObject("TestPlayer");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            var pres = ScriptableObject.CreateInstance<PlayerPresentationSO>();
            var mover = go.AddComponent<PlayerGridMover>();
            mover.Presentation = pres;
            mover.VisualChild = visual.transform;

            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(0, 0),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            mover.Initialize(state, _boardGrid);

            MoveRejectReason rejectedReason = MoveRejectReason.None;
            mover.OnMoveRejected += r => { rejectedReason = r; };

            bool moved = mover.TryMove(Direction.Down); // Down from row 0 is OutOfBounds

            Assert.IsFalse(moved, "Moving down from row 0 should fail.");
            Assert.AreEqual(5, state.RemainingMovementPoints, "Movement points must NOT be spent on rejected move.");
            Assert.AreEqual(new Vector2Int(0, 0), state.PlayerCell, "PlayerCell must remain unchanged.");
            Assert.AreEqual(MoveRejectReason.OutOfBounds, rejectedReason, "OnMoveRejected should be raised with OutOfBounds.");

            UnityEngine.Object.DestroyImmediate(pres);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerGridMover_SnapToCellAndStopMovementImmediate_CancelsMovementCleanly()
        {
            var go = new GameObject("TestPlayer");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            var pres = ScriptableObject.CreateInstance<PlayerPresentationSO>();
            var mover = go.AddComponent<PlayerGridMover>();
            mover.Presentation = pres;
            mover.VisualChild = visual.transform;

            var state = new GameSessionState
            {
                PlayerCell = new Vector2Int(1, 1),
                RemainingMovementPoints = 5,
                GateOpen = false
            };

            mover.Initialize(state, _boardGrid);
            mover.SnapToCell(new Vector2Int(2, 3));

            Assert.IsFalse(mover.IsMoving);
            Assert.AreEqual(visual.transform.localPosition, Vector3.zero);
            Assert.AreEqual(visual.transform.localScale, Vector3.one);

            mover.StopMovementImmediate();
            Assert.IsFalse(mover.IsMoving);

            UnityEngine.Object.DestroyImmediate(pres);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerInputRouter_InputEnabledProperty_CanBeToggled()
        {
            var go = new GameObject("TestRouter");
            var inputConfig = ScriptableObject.CreateInstance<InputConfigSO>();
            var router = go.AddComponent<PlayerInputRouter>();
            router.Config = inputConfig;

            Assert.IsTrue(router.InputEnabled, "Default InputEnabled must be true.");
            router.InputEnabled = false;
            Assert.IsFalse(router.InputEnabled);

            // Reassign config dynamically
            var newConfig = ScriptableObject.CreateInstance<InputConfigSO>();
            router.Config = newConfig;

            UnityEngine.Object.DestroyImmediate(inputConfig);
            UnityEngine.Object.DestroyImmediate(newConfig);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
