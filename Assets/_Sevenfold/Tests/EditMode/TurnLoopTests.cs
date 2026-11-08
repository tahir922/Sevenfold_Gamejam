using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sevenfold.Board;
using Sevenfold.Core;
using Sevenfold.Dice;
using Sevenfold.Player;
using Sevenfold.UI;
using TMPro;
using UnityEngine;

namespace Sevenfold.Tests
{
    [TestFixture]
    public class TurnLoopTests
    {
        private GameConfigSO _config;
        private BoardDefinitionSO _boardDef;
        private TileDefinitionSO _normalTile;
        private TileDefinitionSO _starTile;
        private TileDefinitionSO _trapTile;
        private TileDefinitionSO _charmTile;
        private TileDefinitionSO _gateTile;

        private GameObject _flowGo;
        private GameFlowController _flowController;

        private GameObject _turnGo;
        private TurnController _turnController;

        private GameObject _playerGo;
        private PlayerGridMover _playerMover;
        private PlayerPresentationSO _presentation;

        private GameObject _diceGoA;
        private DiceView _diceViewA;
        private GameObject _diceGoB;
        private DiceView _diceViewB;

        [SetUp]
        public void SetUp()
        {
            _normalTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _normalTile.TileKind = TileKind.Normal;
            _normalTile.BaseColor = Color.gray;

            _starTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _starTile.TileKind = TileKind.Star;
            _starTile.BaseColor = Color.yellow;

            _trapTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _trapTile.TileKind = TileKind.Trap;
            _trapTile.BaseColor = Color.red;

            _charmTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _charmTile.TileKind = TileKind.Charm;
            _charmTile.BaseColor = Color.green;

            _gateTile = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _gateTile.TileKind = TileKind.Gate;
            _gateTile.BaseColor = Color.magenta;

            _config = ScriptableObject.CreateInstance<GameConfigSO>();
            _config.DiceSides = 6;
            _config.LuckyNumber = 7;
            _config.StarsRequired = 1; // 1 star needed for testing unlock
            _config.StartingHearts = 3;
            _config.StartingCharms = 1;
            _config.MaxCharms = 3;
            _config.MaxTurns = 3; // 3 turns for turn limit testing
            _config.AutoEndTurn = true;
            _config.AutoEndTurnDelay = 0.0f; // 0s in tests

            _boardDef = ScriptableObject.CreateInstance<BoardDefinitionSO>();
            _boardDef.Width = 7;
            _boardDef.Height = 7;
            _boardDef.StartCell = new Vector2Int(0, 0);
            _boardDef.GateCell = new Vector2Int(6, 6);
            _boardDef.DefaultNormalTile = _normalTile;

            // Place test special tiles
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(0, 1), _starTile)); // Star 1 step Up
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(1, 0), _trapTile)); // Trap 1 step Right
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(0, 2), _charmTile)); // Charm 2 steps Up
            _boardDef.Cells.Add(new CellEntry(new Vector2Int(6, 6), _gateTile)); // Gate at (6,6)

            _flowGo = new GameObject("Test_FlowController");
            _flowController = _flowGo.AddComponent<GameFlowController>();

            _presentation = ScriptableObject.CreateInstance<PlayerPresentationSO>();
            _presentation.MoveDuration = 0f;

            _playerGo = new GameObject("Test_Player");
            _playerMover = _playerGo.AddComponent<PlayerGridMover>();
            _playerMover.Presentation = _presentation;
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(_playerGo.transform, false);
            _playerMover.VisualChild = visualGo.transform;

            _diceGoA = new GameObject("Test_DieA");
            _diceViewA = _diceGoA.AddComponent<DiceView>();
            _diceGoB = new GameObject("Test_DieB");
            _diceViewB = _diceGoB.AddComponent<DiceView>();

            _turnGo = new GameObject("Test_TurnController");
            _turnController = _turnGo.AddComponent<TurnController>();
            _turnController.Config = _config;
            _turnController.BoardDefinition = _boardDef;
            _turnController.FlowController = _flowController;
            _turnController.PlayerMover = _playerMover;
            _turnController.DiceViewA = _diceViewA;
            _turnController.DiceViewB = _diceViewB;
        }

        [TearDown]
        public void TearDown()
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f;

            if (_turnGo != null) UnityEngine.Object.DestroyImmediate(_turnGo);
            if (_playerGo != null) UnityEngine.Object.DestroyImmediate(_playerGo);
            if (_diceGoA != null) UnityEngine.Object.DestroyImmediate(_diceGoA);
            if (_diceGoB != null) UnityEngine.Object.DestroyImmediate(_diceGoB);
            if (_flowGo != null) UnityEngine.Object.DestroyImmediate(_flowGo);

            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
            if (_boardDef != null) UnityEngine.Object.DestroyImmediate(_boardDef);
            if (_presentation != null) UnityEngine.Object.DestroyImmediate(_presentation);
            if (_normalTile != null) UnityEngine.Object.DestroyImmediate(_normalTile);
            if (_starTile != null) UnityEngine.Object.DestroyImmediate(_starTile);
            if (_trapTile != null) UnityEngine.Object.DestroyImmediate(_trapTile);
            if (_charmTile != null) UnityEngine.Object.DestroyImmediate(_charmTile);
            if (_gateTile != null) UnityEngine.Object.DestroyImmediate(_gateTile);
        }

        // ==========================================
        // 1. DiceService Tests
        // ==========================================

        [Test]
        public void DiceService_Rolls_ValuesBetween1AndSides()
        {
            var dice = new DiceService(_config);
            for (int i = 0; i < 50; i++)
            {
                var result = dice.Roll();
                Assert.That(result.A, Is.InRange(1, 6));
                Assert.That(result.B, Is.InRange(1, 6));
                Assert.That(result.Total, Is.EqualTo(result.A + result.B));
            }
        }

        [Test]
        public void DiceService_ForcedRolls_DequeuedInOrderFirst()
        {
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(2, 5);
            dice.EnqueueForcedRoll(6, 1);

            var first = dice.Roll();
            Assert.That(first.A, Is.EqualTo(2));
            Assert.That(first.B, Is.EqualTo(5));
            Assert.That(first.Total, Is.EqualTo(7));

            var second = dice.Roll();
            Assert.That(second.A, Is.EqualTo(6));
            Assert.That(second.B, Is.EqualTo(1));
            Assert.That(second.Total, Is.EqualTo(7));

            Assert.That(dice.ForcedRollCount, Is.EqualTo(0));
        }

        [Test]
        public void DiceService_FixedSeed_ProducesDeterministicResults()
        {
            _config.UseFixedSeed = true;
            _config.FixedSeed = 12345;
            var dice1 = new DiceService(_config);
            var dice2 = new DiceService(_config);

            for (int i = 0; i < 10; i++)
            {
                var r1 = dice1.Roll();
                var r2 = dice2.Roll();
                Assert.That(r1, Is.EqualTo(r2));
            }
        }

        // ==========================================
        // 2. DiceView Static Table Tests
        // ==========================================

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void DiceView_StaticTable_RotatesFaceNormalToVector3Up(int faceValue)
        {
            Vector3 normal = DiceView.GetFaceNormal(faceValue);
            Quaternion rot = DiceView.GetFaceUpRotation(faceValue);
            Vector3 worldNormal = rot * normal;

            float dot = Vector3.Dot(worldNormal, Vector3.up);
            Assert.That(dot, Is.GreaterThan(0.999f), $"Face {faceValue} normal rotated to {worldNormal}, expected Vector3.up (dot was {dot})");
        }

        [Test]
        public void DiceView_SetFaceImmediate_SnapsRotationAndClampsValue()
        {
            _diceViewA.SetFaceImmediate(3);
            Assert.That(_diceViewA.CurrentFace, Is.EqualTo(3));
            Assert.That(Quaternion.Angle(_diceViewA.transform.localRotation, DiceView.GetFaceUpRotation(3)), Is.LessThan(0.01f));

            // Clamping invalid value
            _diceViewA.SetFaceImmediate(99);
            Assert.That(_diceViewA.CurrentFace, Is.EqualTo(6));
        }

        // ==========================================
        // 3. GameFlowController Tests
        // ==========================================

        [Test]
        public void GameFlowController_SetState_ChangesStateAndFiresEvent()
        {
            GameState lastFrom = GameState.TurnStart;
            GameState lastTo = GameState.TurnStart;
            _flowController.OnStateChanged += (from, to) =>
            {
                lastFrom = from;
                lastTo = to;
            };

            bool changed = _flowController.SetState(GameState.AwaitRoll);
            Assert.That(changed, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.AwaitRoll));
            Assert.That(lastFrom, Is.EqualTo(GameState.TurnStart));
            Assert.That(lastTo, Is.EqualTo(GameState.AwaitRoll));
        }

        [Test]
        public void GameFlowController_VictoryAndDefeat_AreTerminalStates()
        {
            _flowController.SetState(GameState.Victory);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Victory));

            // Attempts to transition out must fail
            bool transitioned = _flowController.SetState(GameState.AwaitRoll);
            Assert.That(transitioned, Is.False);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Victory));
        }

        [Test]
        public void GameFlowController_PauseAndResume_ManagesTimeScale()
        {
            _flowController.SetState(GameState.PlayerMoving);

            bool paused = _flowController.Pause();
            Assert.That(paused, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            bool resumed = _flowController.Resume();
            Assert.That(resumed, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.PlayerMoving));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void GameFlowController_Pause_RejectedDuringRollingOrTerminal()
        {
            _flowController.SetState(GameState.Rolling);
            Assert.That(_flowController.Pause(), Is.False);

            _flowController.SetState(GameState.Victory);
            Assert.That(_flowController.Pause(), Is.False);
        }

        // ==========================================
        // 4. TurnController Flow Tests
        // ==========================================

        [Test]
        public void TurnController_BeginTurn_InitializesTurn1AndAwaitRoll()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            _turnController.Initialize(session, dice);

            int startedTurn = 0;
            _turnController.OnTurnStarted += turn => startedTurn = turn;

            _turnController.BeginTurn();

            Assert.That(session.TurnNumber, Is.EqualTo(1));
            Assert.That(startedTurn, Is.EqualTo(1));
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.AwaitRoll));
        }

        [Test]
        public void TurnController_RequestRoll_GeneratesBudgetAndTransitionsToPlayerMoving()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(3, 4); // Total 7

            _turnController.Initialize(session, dice);
            _turnController.BeginTurn();

            DiceResult rolledResult = default;
            _turnController.OnDiceRolled += res => rolledResult = res;

            _turnController.RequestRoll();

            Assert.That(rolledResult.Total, Is.EqualTo(7));
            Assert.That(session.RemainingMovementPoints, Is.EqualTo(7));
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.PlayerMoving));
        }

        [Test]
        public void TurnController_RequestRoll_IgnoredIfNotInAwaitRoll()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(3, 4);

            _turnController.Initialize(session, dice);
            _flowController.SetState(GameState.TurnStart); // Not AwaitRoll

            _turnController.RequestRoll();

            Assert.That(session.RemainingMovementPoints, Is.EqualTo(0));
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.TurnStart));
        }

        [Test]
        public void TurnController_RequestMove_ValidStep_DecrementsPointsAndResolvesTile()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(2, 2); // Budget 4

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 0));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll(); // State is now PlayerMoving, Points = 4

            // Step UP to (0, 1), which has a Star tile
            _turnController.RequestMove(Direction.Up);

            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(0, 1)));
            Assert.That(session.RemainingMovementPoints, Is.EqualTo(3));
            Assert.That(session.StarsCollected, Is.EqualTo(1));
            Assert.That(session.GateOpen, Is.True); // 1 required in test config
        }

        [Test]
        public void TurnController_TrapWithoutWard_ReducesHearts()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.WardCharges = 0;
            session.Hearts = 3;

            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(1, 2); // Budget 3

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 0));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll();

            // Step RIGHT to (1, 0), which has a Trap tile
            _turnController.RequestMove(Direction.Right);

            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(session.Hearts, Is.EqualTo(2), "Hearts should decrease by 1 when triggering trap without ward.");
        }

        [Test]
        public void TurnController_TrapWithWard_AbsorbsDamageAndProtectsHearts()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.WardCharges = 1;
            session.Hearts = 3;

            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(1, 2);

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 0));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll();

            // Step RIGHT to (1, 0) - Trap
            _turnController.RequestMove(Direction.Right);

            Assert.That(session.WardCharges, Is.EqualTo(0), "Ward charge should be consumed.");
            Assert.That(session.Hearts, Is.EqualTo(3), "Hearts must not decrease when shielded by Ward.");
        }

        [Test]
        public void TurnController_HeartsZero_TriggersDefeatHeartsDepleted()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.WardCharges = 0;
            session.Hearts = 1; // Only 1 heart left

            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(1, 2);

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 0));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll();

            DefeatReason reasonReceived = DefeatReason.TurnLimit;
            _turnController.OnDefeat += reason => reasonReceived = reason;

            // Step RIGHT into Trap
            _turnController.RequestMove(Direction.Right);

            Assert.That(session.Hearts, Is.EqualTo(0));
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Defeat));
            Assert.That(reasonReceived, Is.EqualTo(DefeatReason.HeartsDepleted));
        }

        [Test]
        public void TurnController_TurnLimitExceeded_TriggersDefeatTurnLimit()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.TurnNumber = 3; // Max turns is 3

            var dice = new DiceService(_config);
            _turnController.Initialize(session, dice);

            DefeatReason reasonReceived = DefeatReason.HeartsDepleted;
            _turnController.OnDefeat += reason => reasonReceived = reason;

            _turnController.BeginTurn(); // Attempts turn 4 > 3

            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Defeat));
            Assert.That(reasonReceived, Is.EqualTo(DefeatReason.TurnLimit));
        }

        [Test]
        public void TurnController_EnteringOpenGate_TriggersVictory()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.GateOpen = true; // Gate unlocked
            session.RemainingMovementPoints = 5;

            var dice = new DiceService(_config);
            var grid = new BoardGrid(_boardDef);

            // Position player adjacent to gate: (6, 5)
            session.PlayerCell = new Vector2Int(6, 5);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(6, 5));

            _turnController.Initialize(session, dice, grid);
            _flowController.SetState(GameState.PlayerMoving);

            bool victoryFired = false;
            _turnController.OnVictory += () => victoryFired = true;

            // Step UP to (6, 6) - Fortune Gate
            _turnController.RequestMove(Direction.Up);

            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(6, 6)));
            Assert.That(victoryFired, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Victory));
        }

        [Test]
        public void TurnController_UseCharm_ConvertsCharmToWard()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.Charms = 2;
            session.WardCharges = 0;

            _turnController.Initialize(session, new DiceService(_config));
            _flowController.SetState(GameState.AwaitRoll);

            bool used = _turnController.RequestUseCharm();
            Assert.That(used, Is.True);
            Assert.That(session.Charms, Is.EqualTo(1));
            Assert.That(session.WardCharges, Is.EqualTo(1));

            // Cannot use charm when ward is already active
            bool usedAgain = _turnController.RequestUseCharm();
            Assert.That(usedAgain, Is.False);
            Assert.That(session.Charms, Is.EqualTo(1));
        }

        [Test]
        public void TurnController_RequestEndTurn_DiscardsPointsAndAdvancesTurn()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(3, 3); // Budget 6

            _turnController.Initialize(session, dice);
            _turnController.BeginTurn(); // Turn 1
            _turnController.RequestRoll(); // Points = 6

            Assert.That(session.TurnNumber, Is.EqualTo(1));
            Assert.That(session.RemainingMovementPoints, Is.EqualTo(6));

            int endedTurn = 0;
            _turnController.OnTurnEnded += turn => endedTurn = turn;

            _turnController.RequestEndTurn();

            Assert.That(endedTurn, Is.EqualTo(1));
            Assert.That(session.TurnNumber, Is.EqualTo(2));
            Assert.That(session.RemainingMovementPoints, Is.EqualTo(0));
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.AwaitRoll));
        }

        [Test]
        public void TurnController_CharmPickup_WhenPouchNotFull_ConsumesAndAddsCharm()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.Charms = 1;
            session.PlayerCell = new Vector2Int(0, 1);

            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(1, 1); // Budget 2

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 1));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll();

            // Step UP to (0, 2) which has Charm tile
            _turnController.RequestMove(Direction.Up);

            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(0, 2)));
            Assert.That(session.Charms, Is.EqualTo(2), "Charms should increase by 1.");
            Assert.That(session.CollectedPickupIds.Contains("charm_0_2"), Is.True);
        }

        [Test]
        public void TurnController_CharmPickup_WhenPouchFull_DoesNotConsumeCharm()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.Charms = 3; // Max is 3
            session.PlayerCell = new Vector2Int(0, 1);

            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(1, 1);

            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(0, 1));

            _turnController.Initialize(session, dice, grid);
            _turnController.BeginTurn();
            _turnController.RequestRoll();

            // Step UP to (0, 2) Charm tile
            _turnController.RequestMove(Direction.Up);

            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(0, 2)));
            Assert.That(session.Charms, Is.EqualTo(3), "Charms should remain at 3 when pouch full.");
            Assert.That(session.CollectedPickupIds.Contains("charm_0_2"), Is.False, "Charm should not be marked collected.");
        }

        [Test]
        public void TurnController_LockedGate_MoveRejected_FiresEventAndSpendsNoPoints()
        {
            var session = GameSessionState.CreateNew(_config, _boardDef);
            session.GateOpen = false; // Gate is locked
            session.RemainingMovementPoints = 3;
            session.PlayerCell = new Vector2Int(6, 5);

            var dice = new DiceService(_config);
            var grid = new BoardGrid(_boardDef);
            _playerMover.Initialize(session, grid);
            _playerMover.SnapToCell(new Vector2Int(6, 5));

            _turnController.Initialize(session, dice, grid);
            _flowController.SetState(GameState.PlayerMoving);

            MoveRejectReason rejectedReason = MoveRejectReason.None;
            _turnController.OnMoveRejected += reason => rejectedReason = reason;

            // Step UP toward (6, 6) locked gate
            _turnController.RequestMove(Direction.Up);

            Assert.That(rejectedReason, Is.EqualTo(MoveRejectReason.GateLocked));
            Assert.That(session.PlayerCell, Is.EqualTo(new Vector2Int(6, 5)), "Player should not move into locked gate.");
            Assert.That(session.RemainingMovementPoints, Is.EqualTo(3), "Movement point should NOT be spent when move is rejected.");
        }

        [Test]
        public void GameFlowController_SetState_Paused_CorrectlySynchronizesTimescale()
        {
            _flowController.SetState(GameState.PlayerMoving);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            bool paused = _flowController.SetState(GameState.Paused);
            Assert.That(paused, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            bool unpaused = _flowController.SetState(GameState.PlayerMoving);
            Assert.That(unpaused, Is.True);
            Assert.That(_flowController.CurrentState, Is.EqualTo(GameState.PlayerMoving));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DebugHUD_ReflectsStateAndResourceChanges()
        {
            var hudGo = new GameObject("Test_HUD");
            var tmpGo = new GameObject("TMP");
            tmpGo.transform.SetParent(hudGo.transform, false);
            var tmp = tmpGo.AddComponent<TextMeshProUGUI>();

            var hud = hudGo.AddComponent<DebugHUD>();
            hud.StatusText = tmp;
            hud.FlowController = _flowController;
            hud.TurnController = _turnController;

            var session = GameSessionState.CreateNew(_config, _boardDef);
            var dice = new DiceService(_config);
            dice.EnqueueForcedRoll(2, 5); // Total 7

            _turnController.Initialize(session, dice);
            hud.RefreshDisplay();

            Assert.That(tmp.text, Does.Contain("State:"));
            Assert.That(tmp.text, Does.Contain("Hearts: 3 / 3"));

            _turnController.BeginTurn();
            _turnController.RequestRoll();

            Assert.That(tmp.text, Does.Contain("Dice: 2 + 5 = 7"));
            Assert.That(tmp.text, Does.Contain("Moves left: 7"));

            UnityEngine.Object.DestroyImmediate(hudGo);
        }
    }
}
