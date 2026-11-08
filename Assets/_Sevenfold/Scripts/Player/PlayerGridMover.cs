using System;
using System.Collections;
using Sevenfold.Board;
using Sevenfold.Core;
using UnityEngine;

namespace Sevenfold.Player
{
    /// <summary>
    /// Handles grid-based hop movement and visual animations (hop arc, landing squash, idle bob) for the player pawn.
    /// Validates moves via MoveValidator before spending points or beginning movement.
    /// </summary>
    public class PlayerGridMover : MonoBehaviour
    {
        [Header("Configuration & Presentation")]
        [SerializeField] private PlayerPresentationSO _presentation;
        [SerializeField] private Transform _visualChild;

        private GameSessionState _sessionState;
        private BoardGrid _boardGrid;
        private Coroutine _moveCoroutine;
        private bool _isMoving;

        public event Action<Vector2Int, Vector2Int> OnMoveStarted;
        public event Action<Vector2Int> OnCellEntered;
        public event Action OnMoveCompleted;
        public event Action<MoveRejectReason> OnMoveRejected;

        public bool IsMoving => _isMoving;

        public PlayerPresentationSO Presentation
        {
            get => _presentation;
            set => _presentation = value;
        }

        public Transform VisualChild
        {
            get => _visualChild;
            set => _visualChild = value;
        }

        public GameSessionState SessionState
        {
            get => _sessionState;
            set => _sessionState = value;
        }

        public BoardGrid BoardGrid
        {
            get => _boardGrid;
            set => _boardGrid = value;
        }

        private void Awake()
        {
            if (_presentation == null)
            {
                Debug.LogError($"[PlayerGridMover] Missing PlayerPresentationSO reference on {gameObject.name}.");
            }
            if (_visualChild == null)
            {
                Debug.LogError($"[PlayerGridMover] Missing VisualChild Transform reference on {gameObject.name}.");
            }
        }

        /// <summary>
        /// Injects the active session state and board grid.
        /// </summary>
        public void Initialize(GameSessionState sessionState, BoardGrid boardGrid)
        {
            _sessionState = sessionState;
            _boardGrid = boardGrid;
        }

        private float _idleBobTimer;

        private void OnDisable()
        {
            StopMovementImmediate();
        }

        /// <summary>
        /// Snaps the mover directly to the specified board cell, cancelling any active movement.
        /// </summary>
        public void SnapToCell(Vector2Int cell)
        {
            StopMovementImmediate();

            if (_boardGrid != null)
            {
                transform.localPosition = _boardGrid.CellToLocal(cell);
            }
            if (_visualChild != null)
            {
                _visualChild.localPosition = Vector3.zero;
                _visualChild.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            // Idle bob on the visual child only when stationary (smoothly accumulates from 0)
            if (!_isMoving && _visualChild != null && _presentation != null)
            {
                _idleBobTimer += Time.deltaTime;
                float bobY = Mathf.Sin(_idleBobTimer * _presentation.IdleBobSpeed) * _presentation.IdleBobAmount;
                _visualChild.localPosition = new Vector3(0f, bobY, 0f);
            }
        }

        /// <summary>
        /// Attempts a one-cell move in the given direction.
        /// Returns true if the move is legal and started; false if rejected or already moving.
        /// </summary>
        public bool TryMove(Direction direction)
        {
            if (_isMoving)
            {
                return false;
            }

            if (!MoveValidator.CanMove(_sessionState, _boardGrid, direction, out var target, out var reason))
            {
                OnMoveRejected?.Invoke(reason);
                return false;
            }

            // Move is valid: spend 1 movement point, update state coordinate, and hop
            _sessionState.RemainingMovementPoints -= 1;
            Vector2Int fromCell = _sessionState.PlayerCell;
            _sessionState.PlayerCell = target;

            if (Application.isPlaying)
            {
                if (_moveCoroutine != null)
                {
                    StopCoroutine(_moveCoroutine);
                }
                _moveCoroutine = StartCoroutine(HopRoutine(fromCell, target));
            }
            else
            {
                // In EditMode test execution, coroutines cannot run: snap position and invoke events synchronously
                SnapToCell(target);
                OnMoveStarted?.Invoke(fromCell, target);
                OnCellEntered?.Invoke(target);
                OnMoveCompleted?.Invoke();
            }

            return true;
        }

        public void StopMovementImmediate()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            _isMoving = false;
            _idleBobTimer = 0f;

            if (_boardGrid != null && _sessionState != null)
            {
                transform.localPosition = _boardGrid.CellToLocal(_sessionState.PlayerCell);
            }

            if (_visualChild != null)
            {
                _visualChild.localPosition = Vector3.zero;
                _visualChild.localScale = Vector3.one;
            }
        }

        private IEnumerator HopRoutine(Vector2Int from, Vector2Int to)
        {
            _isMoving = true;
            OnMoveStarted?.Invoke(from, to);

            Vector3 startPos = _boardGrid != null ? _boardGrid.CellToLocal(from) : transform.localPosition;
            Vector3 endPos = _boardGrid != null ? _boardGrid.CellToLocal(to) : transform.localPosition;

            if (_visualChild != null)
            {
                _visualChild.localPosition = Vector3.zero;
                _visualChild.localScale = Vector3.one;
            }

            float duration = _presentation != null ? _presentation.MoveDuration : 0.22f;
            float hopHeight = _presentation != null ? _presentation.HopHeight : 0.12f;

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    // Parabolic arc: 4 * h * t * (1 - t) reaches peak h at t = 0.5
                    float arcY = 4f * hopHeight * t * (1f - t);
                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += arcY;
                    transform.localPosition = currentPos;

                    yield return null;
                }
            }

            transform.localPosition = endPos;
            OnCellEntered?.Invoke(to);

            // Landing squash on VISUAL CHILD only
            float landingDuration = _presentation != null ? _presentation.LandingDuration : 0.1f;
            float landingSquash = _presentation != null ? _presentation.LandingSquash : 0.1f;

            if (landingDuration > 0f && _visualChild != null)
            {
                float elapsedSquash = 0f;
                while (elapsedSquash < landingDuration)
                {
                    elapsedSquash += Time.deltaTime;
                    float tSquash = Mathf.Clamp01(elapsedSquash / landingDuration);

                    // Sine squash: 0 -> peak squash at t=0.5 -> 0
                    float squashFactor = Mathf.Sin(tSquash * Mathf.PI) * landingSquash;
                    _visualChild.localScale = new Vector3(
                        1f + squashFactor * 0.5f,
                        1f - squashFactor,
                        1f + squashFactor * 0.5f
                    );

                    yield return null;
                }

                _visualChild.localScale = Vector3.one;
                _visualChild.localPosition = Vector3.zero;
            }

            _idleBobTimer = 0f;
            _isMoving = false;
            _moveCoroutine = null;
            OnMoveCompleted?.Invoke();
        }
    }
}
