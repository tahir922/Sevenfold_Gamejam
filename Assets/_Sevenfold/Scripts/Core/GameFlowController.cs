using System;
using UnityEngine;

namespace Sevenfold.Core
{
    /// <summary>
    /// Master state machine for the game session. Owns GameState and provides the single
    /// authorized entry point for state transitions and timescale control.
    /// UI and controllers request state transitions; this class enforces valid flows.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("State Tracking")]
        [SerializeField] private GameState _currentState = GameState.TurnStart;
        [SerializeField] private GameState _stateBeforePause = GameState.TurnStart;

        public GameState CurrentState => _currentState;
        public GameState StateBeforePause => _stateBeforePause;

        public event Action<GameState, GameState> OnStateChanged;

        private void OnDisable()
        {
            // Ensure timescale is never left frozen when unloading or disabling
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Transitions to a new game state. Enforces terminal states (Victory/Defeat cannot be transitioned out of).
        /// Returns true if transition succeeded, false if rejected.
        /// </summary>
        public bool SetState(GameState newState)
        {
            if (_currentState == GameState.Victory || _currentState == GameState.Defeat)
            {
                Debug.LogWarning($"[GameFlowController] Cannot transition from terminal state {_currentState} to {newState}.");
                return false;
            }

            if (_currentState == newState)
            {
                return false;
            }

            if (newState == GameState.Paused)
            {
                return Pause();
            }

            if (_currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
            }

            GameState previous = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(previous, newState);
            return true;
        }

        /// <summary>
        /// Pauses the game if currently in a pausable gameplay state.
        /// Saves prior state and sets Time.timeScale to 0.
        /// </summary>
        public bool Pause()
        {
            if (_currentState == GameState.Paused ||
                _currentState == GameState.Victory ||
                _currentState == GameState.Defeat ||
                _currentState == GameState.Rolling)
            {
                return false;
            }

            _stateBeforePause = _currentState;
            Time.timeScale = 0f;
            GameState previous = _currentState;
            _currentState = GameState.Paused;
            OnStateChanged?.Invoke(previous, GameState.Paused);
            return true;
        }

        /// <summary>
        /// Resumes gameplay from pause, restoring prior state and Time.timeScale to 1.
        /// </summary>
        public bool Resume()
        {
            if (_currentState != GameState.Paused)
            {
                return false;
            }

            Time.timeScale = 1f;
            GameState previous = _currentState;
            _currentState = _stateBeforePause;
            OnStateChanged?.Invoke(previous, _currentState);
            return true;
        }

        /// <summary>
        /// Toggles between paused and active states.
        /// </summary>
        public bool TogglePause()
        {
            if (_currentState == GameState.Paused)
            {
                return Resume();
            }
            return Pause();
        }
    }
}
