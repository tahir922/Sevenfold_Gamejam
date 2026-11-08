using Sevenfold.Board;
using Sevenfold.Player;
using UnityEngine;

namespace Sevenfold.Core
{
    /// <summary>
    /// Temporary debug harness for movement testing.
    /// Creates a GameSessionState, sets 99 movement points, positions the player,
    /// and routes input from PlayerInputRouter to PlayerGridMover with detailed logging.
    /// Will be replaced in Step 4 by the full GameFlowController.
    /// </summary>
    public class DebugMovementHarness : MonoBehaviour
    {
        [Header("Configuration & Dependencies")]
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private BoardDefinitionSO _board;
        [SerializeField] private BoardBuilder _boardBuilder;
        [SerializeField] private PlayerInputRouter _inputRouter;
        [SerializeField] private PlayerGridMover _playerMover;

        private GameSessionState _sessionState;

        public GameSessionState SessionState => _sessionState;

        public GameConfigSO Config
        {
            get => _config;
            set => _config = value;
        }

        public BoardDefinitionSO BoardDefinition
        {
            get => _board;
            set => _board = value;
        }

        public BoardBuilder BoardBuilder
        {
            get => _boardBuilder;
            set => _boardBuilder = value;
        }

        public PlayerInputRouter InputRouter
        {
            get => _inputRouter;
            set => _inputRouter = value;
        }

        public PlayerGridMover PlayerMover
        {
            get => _playerMover;
            set => _playerMover = value;
        }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"[DebugMovementHarness] Missing GameConfigSO on {gameObject.name}.");
            }
            if (_board == null)
            {
                Debug.LogError($"[DebugMovementHarness] Missing BoardDefinitionSO on {gameObject.name}.");
            }
            if (_boardBuilder == null)
            {
                Debug.LogError($"[DebugMovementHarness] Missing BoardBuilder on {gameObject.name}.");
            }
            if (_inputRouter == null)
            {
                Debug.LogError($"[DebugMovementHarness] Missing PlayerInputRouter on {gameObject.name}.");
            }
            if (_playerMover == null)
            {
                Debug.LogError($"[DebugMovementHarness] Missing PlayerGridMover on {gameObject.name}.");
            }
        }

        private void Start()
        {
            _sessionState = GameSessionState.CreateNew(_config, _board);
            _sessionState.RemainingMovementPoints = 99;

            BoardGrid grid = _boardBuilder != null && _boardBuilder.Grid != null
                ? _boardBuilder.Grid
                : new BoardGrid(_board);

            if (_playerMover != null)
            {
                _playerMover.Initialize(_sessionState, grid);
                _playerMover.SnapToCell(_sessionState.PlayerCell);
            }

            Debug.Log($"[DebugMovementHarness] Initialized debug session at {_sessionState.PlayerCell} with {_sessionState.RemainingMovementPoints} movement points.");
        }

        private void OnEnable()
        {
            if (_inputRouter != null)
            {
                _inputRouter.MovePressed += HandleMovePressed;
                _inputRouter.RollPressed += HandleRollPressed;
                _inputRouter.EndTurnPressed += HandleEndTurnPressed;
                _inputRouter.UseCharmPressed += HandleUseCharmPressed;
                _inputRouter.PausePressed += HandlePausePressed;
            }

            if (_playerMover != null)
            {
                _playerMover.OnMoveStarted += HandleMoveStarted;
                _playerMover.OnCellEntered += HandleCellEntered;
                _playerMover.OnMoveCompleted += HandleMoveCompleted;
                _playerMover.OnMoveRejected += HandleMoveRejected;
            }
        }

        private void OnDisable()
        {
            if (_inputRouter != null)
            {
                _inputRouter.MovePressed -= HandleMovePressed;
                _inputRouter.RollPressed -= HandleRollPressed;
                _inputRouter.EndTurnPressed -= HandleEndTurnPressed;
                _inputRouter.UseCharmPressed -= HandleUseCharmPressed;
                _inputRouter.PausePressed -= HandlePausePressed;
            }

            if (_playerMover != null)
            {
                _playerMover.OnMoveStarted -= HandleMoveStarted;
                _playerMover.OnCellEntered -= HandleCellEntered;
                _playerMover.OnMoveCompleted -= HandleMoveCompleted;
                _playerMover.OnMoveRejected -= HandleMoveRejected;
            }
        }

        private void HandleMovePressed(Direction direction)
        {
            if (_playerMover == null) return;
            _playerMover.TryMove(direction);
        }

        private void HandleRollPressed()
        {
            Debug.Log("[DebugMovementHarness] Roll pressed (no-op in movement debug harness).");
        }

        private void HandleEndTurnPressed()
        {
            Debug.Log("[DebugMovementHarness] EndTurn pressed (no-op in movement debug harness).");
        }

        private void HandleUseCharmPressed()
        {
            Debug.Log("[DebugMovementHarness] UseCharm pressed (no-op in movement debug harness).");
        }

        private void HandlePausePressed()
        {
            Debug.Log("[DebugMovementHarness] Pause pressed (no-op in movement debug harness).");
        }

        private void HandleMoveStarted(Vector2Int from, Vector2Int to)
        {
            int remaining = _sessionState != null ? _sessionState.RemainingMovementPoints : -1;
            Debug.Log($"[DebugMovementHarness] Move started: {from} -> {to}. Remaining points: {remaining}");
        }

        private void HandleCellEntered(Vector2Int cell)
        {
            Debug.Log($"[DebugMovementHarness] Entered cell: {cell}");
        }

        private void HandleMoveCompleted()
        {
            int remaining = _sessionState != null ? _sessionState.RemainingMovementPoints : -1;
            Vector2Int current = _sessionState != null ? _sessionState.PlayerCell : Vector2Int.zero;
            Debug.Log($"[DebugMovementHarness] Move completed at {current}. Remaining points: {remaining}");
        }

        private void HandleMoveRejected(MoveRejectReason reason)
        {
            int remaining = _sessionState != null ? _sessionState.RemainingMovementPoints : -1;
            Vector2Int current = _sessionState != null ? _sessionState.PlayerCell : Vector2Int.zero;
            Debug.LogWarning($"[DebugMovementHarness] Move rejected! Reason: {reason}. Current cell: {current}, Remaining points: {remaining}");
        }
    }
}
