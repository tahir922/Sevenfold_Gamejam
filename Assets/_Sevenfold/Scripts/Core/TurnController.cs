using System;
using System.Collections;
using Sevenfold.Board;
using Sevenfold.Dice;
using Sevenfold.Player;
using UnityEngine;

namespace Sevenfold.Core
{
    /// <summary>
    /// Coordinates the full turn loop from roll to tile resolution and turn completion.
    /// Drives the game state machine and enforces the rules in AGENTS.md section 5.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [Header("Configuration & Data")]
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private BoardDefinitionSO _board;

        [Header("Scene References")]
        [SerializeField] private BoardBuilder _boardBuilder;
        [SerializeField] private PlayerGridMover _playerMover;
        [SerializeField] private GameFlowController _flowController;
        [SerializeField] private DiceView _diceViewA;
        [SerializeField] private DiceView _diceViewB;
        [SerializeField] private PlayerInputRouter _inputRouter;

        private GameSessionState _sessionState;
        private DiceService _diceService;
        private BoardGrid _grid;
        private bool _hasRolledThisTurn;
        private Coroutine _rollCoroutine;
        private Coroutine _autoEndTurnCoroutine;

        public event Action<int> OnTurnStarted;
        public event Action<DiceResult> OnDiceRolled;
        public event Action<int, int> OnMovementPointsChanged;
        public event Action<int> OnTurnEnded;
        public event Action<DefeatReason> OnDefeat;
        public event Action OnVictory;
        public event Action<Vector2Int, TileDefinitionSO> OnTileResolved;
        public event Action OnCharmUsed;
        public event Action<MoveRejectReason> OnMoveRejected;

        public GameSessionState SessionState => _sessionState;
        public DiceService DiceService { get => _diceService; set => _diceService = value; }
        public BoardGrid Grid => _grid;
        public GameConfigSO Config { get => _config; set => _config = value; }
        public BoardDefinitionSO BoardDefinition { get => _board; set => _board = value; }
        public BoardBuilder BoardBuilder { get => _boardBuilder; set => _boardBuilder = value; }
        public PlayerGridMover PlayerMover
        {
            get => _playerMover;
            set
            {
                if (_playerMover == value) return;
                UnsubscribeMover();
                _playerMover = value;
                if (enabled)
                {
                    SubscribeMover();
                }
            }
        }
        public GameFlowController FlowController { get => _flowController; set => _flowController = value; }
        public DiceView DiceViewA { get => _diceViewA; set => _diceViewA = value; }
        public DiceView DiceViewB { get => _diceViewB; set => _diceViewB = value; }
        public PlayerInputRouter InputRouter
        {
            get => _inputRouter;
            set
            {
                if (_inputRouter == value) return;
                UnsubscribeInput();
                _inputRouter = value;
                if (enabled)
                {
                    SubscribeInput();
                }
            }
        }

        private void Awake()
        {
            if (_config == null) Debug.LogError($"[TurnController] Missing GameConfigSO on {gameObject.name}.");
            if (_board == null) Debug.LogError($"[TurnController] Missing BoardDefinitionSO on {gameObject.name}.");
            if (_boardBuilder == null) Debug.LogError($"[TurnController] Missing BoardBuilder on {gameObject.name}.");
            if (_playerMover == null) Debug.LogError($"[TurnController] Missing PlayerGridMover on {gameObject.name}.");
            if (_flowController == null) Debug.LogError($"[TurnController] Missing GameFlowController on {gameObject.name}.");
            if (_diceViewA == null) Debug.LogError($"[TurnController] Missing DiceView A on {gameObject.name}.");
            if (_diceViewB == null) Debug.LogError($"[TurnController] Missing DiceView B on {gameObject.name}.");
            if (_inputRouter == null) Debug.LogError($"[TurnController] Missing PlayerInputRouter on {gameObject.name}.");

            if (_sessionState == null && _config != null && _board != null)
            {
                _sessionState = GameSessionState.CreateNew(_config, _board);
            }
        }

        private void OnEnable()
        {
            SubscribeInput();
            SubscribeMover();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            UnsubscribeMover();

            if (_rollCoroutine != null)
            {
                StopCoroutine(_rollCoroutine);
                _rollCoroutine = null;
            }

            if (_autoEndTurnCoroutine != null)
            {
                StopCoroutine(_autoEndTurnCoroutine);
                _autoEndTurnCoroutine = null;
            }
        }

        private void SubscribeInput()
        {
            if (_inputRouter == null) return;
            UnsubscribeInput();
            _inputRouter.RollPressed += RequestRoll;
            _inputRouter.MovePressed += RequestMove;
            _inputRouter.EndTurnPressed += RequestEndTurn;
            _inputRouter.UseCharmPressed += HandleUseCharmPressed;
            _inputRouter.PausePressed += HandlePausePressed;
        }

        private void UnsubscribeInput()
        {
            if (_inputRouter == null) return;
            _inputRouter.RollPressed -= RequestRoll;
            _inputRouter.MovePressed -= RequestMove;
            _inputRouter.EndTurnPressed -= RequestEndTurn;
            _inputRouter.UseCharmPressed -= HandleUseCharmPressed;
            _inputRouter.PausePressed -= HandlePausePressed;
        }

        private void SubscribeMover()
        {
            if (_playerMover == null) return;
            UnsubscribeMover();
            _playerMover.OnCellEntered += HandleCellEntered;
            _playerMover.OnMoveCompleted += HandleMoveCompleted;
            _playerMover.OnMoveRejected += HandleMoveRejected;
        }

        private void UnsubscribeMover()
        {
            if (_playerMover == null) return;
            _playerMover.OnCellEntered -= HandleCellEntered;
            _playerMover.OnMoveCompleted -= HandleMoveCompleted;
            _playerMover.OnMoveRejected -= HandleMoveRejected;
        }

        private void HandleMoveRejected(MoveRejectReason reason)
        {
            if (_inputRouter != null)
            {
                _inputRouter.InputEnabled = true;
            }

            OnMoveRejected?.Invoke(reason);
            if (reason == MoveRejectReason.GateLocked)
            {
                int stars = _sessionState != null ? _sessionState.StarsCollected : 0;
                int req = _config != null ? _config.StarsRequired : 7;
                Debug.Log($"[TurnController] Gate locked: {stars}/{req} stars.");
            }
        }

        private void Start()
        {
            if (_sessionState == null)
            {
                _sessionState = GameSessionState.CreateNew(_config, _board);
            }

            if (_diceService == null)
            {
                _diceService = new DiceService(_config);
            }

            if (_grid == null)
            {
                _grid = (_boardBuilder != null && _boardBuilder.Grid != null)
                    ? _boardBuilder.Grid
                    : new BoardGrid(_board);
            }

            if (_playerMover != null)
            {
                _playerMover.Initialize(_sessionState, _grid);
                _playerMover.SnapToCell(_sessionState.PlayerCell);
            }

            RevealNeighbours(_sessionState.PlayerCell);

            _diceViewA?.SetFaceImmediate(1);
            _diceViewB?.SetFaceImmediate(1);

            BeginTurn();
        }

        /// <summary>
        /// Explicit initialization helper for tests or external bootstrapping.
        /// </summary>
        public void Initialize(GameSessionState sessionState, DiceService diceService, BoardGrid grid = null, PlayerGridMover playerMover = null)
        {
            _sessionState = sessionState;
            _diceService = diceService;
            _grid = grid ?? (_board != null ? new BoardGrid(_board) : null);
            if (playerMover != null)
            {
                PlayerMover = playerMover;
            }
            else if (_playerMover != null && enabled)
            {
                SubscribeMover();
            }
        }

        /// <summary>
        /// Begins a new turn. Checks max turn limit, increments turn counter, resets budget,
        /// and transitions state from TurnStart to AwaitRoll.
        /// </summary>
        public void BeginTurn()
        {
            if (_sessionState == null)
            {
                _sessionState = GameSessionState.CreateNew(_config, _board);
            }

            int maxTurns = _config != null ? _config.MaxTurns : 7;
            if (_sessionState.TurnNumber + 1 > maxTurns)
            {
                _flowController?.SetState(GameState.Defeat);
                OnDefeat?.Invoke(DefeatReason.TurnLimit);
                Debug.Log($"[TurnController] Turn limit reached ({_sessionState.TurnNumber} >= {maxTurns}). Defeat!");
                return;
            }

            _sessionState.TurnNumber += 1;
            _sessionState.RemainingMovementPoints = 0;
            _hasRolledThisTurn = false;

            _flowController?.SetState(GameState.TurnStart);
            OnTurnStarted?.Invoke(_sessionState.TurnNumber);

            _flowController?.SetState(GameState.AwaitRoll);
        }

        /// <summary>
        /// Requests a 2d6 roll. Ignored if not in AwaitRoll state.
        /// Generates dice values first, then animates dice views.
        /// </summary>
        public void RequestRoll()
        {
            if (_flowController != null && _flowController.CurrentState != GameState.AwaitRoll)
            {
                return;
            }

            _flowController?.SetState(GameState.Rolling);

            if (Application.isPlaying)
            {
                if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
                _rollCoroutine = StartCoroutine(RollRoutine());
            }
            else
            {
                ExecuteRollSynchronously();
            }
        }

        private IEnumerator RollRoutine()
        {
            if (_diceService == null)
            {
                _diceService = new DiceService(_config);
            }

            var result = _diceService.Roll();
            _sessionState.LastDieA = result.A;
            _sessionState.LastDieB = result.B;
            _sessionState.LastRollTotal = result.Total;
            OnDiceRolled?.Invoke(result);

            float animDuration = _config != null ? _config.DiceAnimDuration : 0.7f;
            Coroutine animA = null;
            Coroutine animB = null;

            if (_diceViewA != null) animA = _diceViewA.AnimateToFace(result.A, animDuration);
            if (_diceViewB != null) animB = _diceViewB.AnimateToFace(result.B, animDuration);

            if (animA != null) yield return animA;
            if (animB != null) yield return animB;

            // Lucky Fate check (only first roll of turn can trigger)
            bool isFirstRoll = !_hasRolledThisTurn;
            _hasRolledThisTurn = true;

            int luckyNumber = _config != null ? _config.LuckyNumber : 7;
            if (isFirstRoll && result.Total == luckyNumber)
            {
                Debug.Log("LUCKY 7 (fate not implemented yet)");
            }

            _sessionState.RemainingMovementPoints = result.Total;
            OnMovementPointsChanged?.Invoke(_sessionState.RemainingMovementPoints, result.Total);

            _flowController?.SetState(GameState.PlayerMoving);
            _rollCoroutine = null;
        }

        private void ExecuteRollSynchronously()
        {
            if (_diceService == null)
            {
                _diceService = new DiceService(_config);
            }

            var result = _diceService.Roll();
            _sessionState.LastDieA = result.A;
            _sessionState.LastDieB = result.B;
            _sessionState.LastRollTotal = result.Total;
            OnDiceRolled?.Invoke(result);

            _diceViewA?.SetFaceImmediate(result.A);
            _diceViewB?.SetFaceImmediate(result.B);

            bool isFirstRoll = !_hasRolledThisTurn;
            _hasRolledThisTurn = true;

            int luckyNumber = _config != null ? _config.LuckyNumber : 7;
            if (isFirstRoll && result.Total == luckyNumber)
            {
                Debug.Log("LUCKY 7 (fate not implemented yet)");
            }

            _sessionState.RemainingMovementPoints = result.Total;
            OnMovementPointsChanged?.Invoke(_sessionState.RemainingMovementPoints, result.Total);

            _flowController?.SetState(GameState.PlayerMoving);
        }

        /// <summary>
        /// Requests a 1-step move in the specified direction. Ignored if not in PlayerMoving state.
        /// </summary>
        public void RequestMove(Direction direction)
        {
            if (_flowController != null && _flowController.CurrentState != GameState.PlayerMoving)
            {
                return;
            }

            if (_playerMover == null || _playerMover.IsMoving)
            {
                return;
            }

            if (_inputRouter != null)
            {
                _inputRouter.InputEnabled = false;
            }

            if (!_playerMover.TryMove(direction))
            {
                if (_inputRouter != null)
                {
                    _inputRouter.InputEnabled = true;
                }
                return;
            }

            OnMovementPointsChanged?.Invoke(_sessionState.RemainingMovementPoints, _sessionState.LastRollTotal);
        }

        private void HandleCellEntered(Vector2Int cell)
        {
            _flowController?.SetState(GameState.ResolvingTile);
            ResolveTile(cell);
        }

        private void ResolveTile(Vector2Int cell)
        {
            var tile = _grid != null ? _grid.GetTile(cell) : null;
            var view = _boardBuilder != null ? _boardBuilder.GetView(cell) : null;

            if (tile != null)
            {
                switch (tile.TileKind)
                {
                    case TileKind.Star:
                    {
                        string id = $"star_{cell.x}_{cell.y}";
                        if (!_sessionState.CollectedPickupIds.Contains(id))
                        {
                            _sessionState.CollectedPickupIds.Add(id);
                            _sessionState.StarsCollected += 1;
                            view?.SetConsumed(true);

                            int required = _config != null ? _config.StarsRequired : 7;
                            if (_sessionState.StarsCollected >= required)
                            {
                                _sessionState.GateOpen = true;
                                Vector2Int gateCoord = _board != null ? _board.GateCell : new Vector2Int(6, 6);
                                var gateView = _boardBuilder != null ? _boardBuilder.GetView(gateCoord) : null;
                                gateView?.SetGateOpen(true);
                            }
                        }
                        break;
                    }

                    case TileKind.Trap:
                    {
                        string id = $"trap_{cell.x}_{cell.y}";
                        if (!_sessionState.ConsumedTrapIds.Contains(id))
                        {
                            _sessionState.ConsumedTrapIds.Add(id);
                            view?.SetConsumed(true);

                            if (_sessionState.WardCharges > 0)
                            {
                                _sessionState.WardCharges -= 1;
                                Debug.Log($"[TurnController] Ward absorbed trap at {cell}! Remaining ward: {_sessionState.WardCharges}");
                            }
                            else
                            {
                                _sessionState.Hearts -= 1;
                                Debug.Log($"[TurnController] Trap triggered at {cell}! Remaining hearts: {_sessionState.Hearts}");
                            }
                        }
                        break;
                    }

                    case TileKind.Charm:
                    {
                        string id = $"charm_{cell.x}_{cell.y}";
                        if (!_sessionState.CollectedPickupIds.Contains(id))
                        {
                            int maxCharms = _config != null ? _config.MaxCharms : 3;
                            if (_sessionState.Charms < maxCharms)
                            {
                                _sessionState.CollectedPickupIds.Add(id);
                                _sessionState.Charms += 1;
                                view?.SetConsumed(true);
                                Debug.Log($"[TurnController] Collected charm at {cell}! Total charms: {_sessionState.Charms}");
                            }
                            else
                            {
                                Debug.Log("[TurnController] Pouch full: charms at maximum.");
                            }
                        }
                        break;
                    }

                    case TileKind.Gate:
                        break;
                }
            }

            RevealNeighbours(cell);
            OnTileResolved?.Invoke(cell, tile);

            // Win check (happens before turn-limit or other checks)
            Vector2Int gateTarget = _board != null ? _board.GateCell : new Vector2Int(6, 6);
            if (cell == gateTarget && _sessionState.GateOpen)
            {
                _flowController?.SetState(GameState.Victory);
                OnVictory?.Invoke();
                Debug.Log("<color=green>[TurnController] VICTORY! Stepped into Fortune Gate!</color>");
                return;
            }

            // Defeat check: hearts depleted
            if (_sessionState.Hearts <= 0)
            {
                _flowController?.SetState(GameState.Defeat);
                OnDefeat?.Invoke(DefeatReason.HeartsDepleted);
                Debug.Log("<color=red>[TurnController] DEFEAT! Hearts depleted.</color>");
                return;
            }
        }

        private void HandleMoveCompleted()
        {
            if (_flowController != null &&
                (_flowController.CurrentState == GameState.Victory || _flowController.CurrentState == GameState.Defeat))
            {
                return;
            }

            if (_sessionState.RemainingMovementPoints > 0)
            {
                _flowController?.SetState(GameState.PlayerMoving);
                if (_inputRouter != null)
                {
                    _inputRouter.InputEnabled = true;
                }
            }
            else
            {
                bool autoEnd = _config != null ? _config.AutoEndTurn : true;
                if (autoEnd)
                {
                    if (Application.isPlaying)
                    {
                        if (_autoEndTurnCoroutine != null) StopCoroutine(_autoEndTurnCoroutine);
                        _autoEndTurnCoroutine = StartCoroutine(AutoEndTurnRoutine());
                    }
                    else
                    {
                        EndTurn();
                    }
                }
                else
                {
                    _flowController?.SetState(GameState.PlayerMoving);
                    if (_inputRouter != null)
                    {
                        _inputRouter.InputEnabled = true;
                    }
                }
            }
        }

        private IEnumerator AutoEndTurnRoutine()
        {
            _flowController?.SetState(GameState.TurnEnding);

            float delay = _config != null ? _config.AutoEndTurnDelay : 0.3f;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            _autoEndTurnCoroutine = null;

            if (_flowController != null &&
                (_flowController.CurrentState == GameState.Victory || _flowController.CurrentState == GameState.Defeat))
            {
                yield break;
            }

            EndTurn();
        }

        /// <summary>
        /// Requests ending the turn early. Any remaining movement points are lost.
        /// </summary>
        public void RequestEndTurn()
        {
            if (_flowController != null &&
                _flowController.CurrentState != GameState.PlayerMoving &&
                _flowController.CurrentState != GameState.TurnEnding)
            {
                return;
            }

            if (_playerMover != null && _playerMover.IsMoving)
            {
                return;
            }

            if (_autoEndTurnCoroutine != null)
            {
                StopCoroutine(_autoEndTurnCoroutine);
                _autoEndTurnCoroutine = null;
            }

            EndTurn();
        }

        private void EndTurn()
        {
            if (_flowController != null &&
                (_flowController.CurrentState == GameState.Victory || _flowController.CurrentState == GameState.Defeat))
            {
                return;
            }

            _flowController?.SetState(GameState.TurnEnding);
            _sessionState.RemainingMovementPoints = 0;
            OnMovementPointsChanged?.Invoke(0, _sessionState.LastRollTotal);
            OnTurnEnded?.Invoke(_sessionState.TurnNumber);

            if (_inputRouter != null)
            {
                _inputRouter.InputEnabled = true;
            }

            BeginTurn();
        }

        /// <summary>
        /// Uses 1 charm from pouch to gain 1 Ward charge, if allowed by rules.
        /// </summary>
        public bool RequestUseCharm()
        {
            if (_flowController != null &&
                _flowController.CurrentState != GameState.AwaitRoll &&
                _flowController.CurrentState != GameState.PlayerMoving)
            {
                return false;
            }

            if (_playerMover != null && _playerMover.IsMoving)
            {
                return false;
            }

            if (_sessionState != null && _sessionState.Charms > 0 && _sessionState.WardCharges == 0)
            {
                _sessionState.Charms -= 1;
                _sessionState.WardCharges = 1;
                OnCharmUsed?.Invoke();
                Debug.Log($"[TurnController] Charm used! Ward active. Remaining charms: {_sessionState.Charms}");
                return true;
            }

            return false;
        }

        private void HandleUseCharmPressed()
        {
            RequestUseCharm();
        }

        private void HandlePausePressed()
        {
            _flowController?.TogglePause();
        }

        private void RevealNeighbours(Vector2Int cell)
        {
            if (_sessionState == null) return;

            int width = _board != null ? _board.Width : 7;
            int height = _board != null ? _board.Height : 7;
            GameSessionState.RevealAround(_sessionState.RevealedCells, cell, 1, width, height);

            if (_boardBuilder != null)
            {
                foreach (var coord in _sessionState.RevealedCells)
                {
                    var cellView = _boardBuilder.GetView(coord);
                    if (cellView != null)
                    {
                        cellView.SetRevealed(true);
                    }
                }
            }
        }
    }
}
