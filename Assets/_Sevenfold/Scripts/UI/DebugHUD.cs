using Sevenfold.Core;
using Sevenfold.Dice;
using TMPro;
using UnityEngine;

namespace Sevenfold.UI
{
    /// <summary>
    /// Temporary uGUI HUD displaying turn state, dice results, resources, and progress.
    /// Purely event-driven; performs zero per-frame polling.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Event Sources")]
        [SerializeField] private GameFlowController _flowController;
        [SerializeField] private TurnController _turnController;

        public TextMeshProUGUI StatusText
        {
            get => _statusText;
            set => _statusText = value;
        }

        public GameFlowController FlowController
        {
            get => _flowController;
            set => _flowController = value;
        }

        public TurnController TurnController
        {
            get => _turnController;
            set => _turnController = value;
        }

        private void Awake()
        {
            if (_statusText == null)
            {
                Debug.LogError($"[DebugHUD] Missing TextMeshProUGUI reference on {gameObject.name}.");
            }
        }

        private string _transientMessage;
        private float _transientMessageTimer;

        private void OnEnable()
        {
            if (_flowController != null)
            {
                _flowController.OnStateChanged += HandleStateChanged;
            }

            if (_turnController != null)
            {
                _turnController.OnTurnStarted += HandleTurnStarted;
                _turnController.OnDiceRolled += HandleDiceRolled;
                _turnController.OnMovementPointsChanged += HandleMovementPointsChanged;
                _turnController.OnTurnEnded += HandleTurnEnded;
                _turnController.OnDefeat += HandleDefeat;
                _turnController.OnVictory += HandleVictory;
                _turnController.OnTileResolved += HandleTileResolved;
                _turnController.OnCharmUsed += HandleCharmUsed;
                _turnController.OnMoveRejected += HandleMoveRejected;
            }

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (_flowController != null)
            {
                _flowController.OnStateChanged -= HandleStateChanged;
            }

            if (_turnController != null)
            {
                _turnController.OnTurnStarted -= HandleTurnStarted;
                _turnController.OnDiceRolled -= HandleDiceRolled;
                _turnController.OnMovementPointsChanged -= HandleMovementPointsChanged;
                _turnController.OnTurnEnded -= HandleTurnEnded;
                _turnController.OnDefeat -= HandleDefeat;
                _turnController.OnVictory -= HandleVictory;
                _turnController.OnTileResolved -= HandleTileResolved;
                _turnController.OnCharmUsed -= HandleCharmUsed;
                _turnController.OnMoveRejected -= HandleMoveRejected;
            }
        }

        private void Update()
        {
            if (_transientMessageTimer > 0f)
            {
                _transientMessageTimer -= Time.deltaTime;
                if (_transientMessageTimer <= 0f)
                {
                    _transientMessage = null;
                    RefreshDisplay();
                }
            }
        }

        private void HandleStateChanged(GameState from, GameState to) => RefreshDisplay();
        private void HandleTurnStarted(int turn) => RefreshDisplay();
        private void HandleDiceRolled(DiceResult result) => RefreshDisplay();
        private void HandleMovementPointsChanged(int remaining, int budget) => RefreshDisplay();
        private void HandleTurnEnded(int turn) => RefreshDisplay();
        private void HandleDefeat(DefeatReason reason) => RefreshDisplay();
        private void HandleVictory() => RefreshDisplay();
        private void HandleTileResolved(Vector2Int cell, TileDefinitionSO tile) => RefreshDisplay();
        private void HandleCharmUsed() => RefreshDisplay();

        private void HandleMoveRejected(MoveRejectReason reason)
        {
            if (reason == MoveRejectReason.GateLocked)
            {
                var session = _turnController != null ? _turnController.SessionState : null;
                var config = _turnController != null ? _turnController.Config : null;
                int stars = session != null ? session.StarsCollected : 0;
                int req = config != null ? config.StarsRequired : 7;
                _transientMessage = $"Gate locked: {stars}/{req} stars";
                _transientMessageTimer = 2.0f;
            }
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_statusText == null) return;

            string stateStr = _flowController != null ? _flowController.CurrentState.ToString() : "N/A";
            var session = _turnController != null ? _turnController.SessionState : null;
            var config = _turnController != null ? _turnController.Config : null;

            int turnNumber = session != null ? session.TurnNumber : 0;
            int maxTurns = config != null ? config.MaxTurns : 7;
            int dieA = session != null ? session.LastDieA : 0;
            int dieB = session != null ? session.LastDieB : 0;
            int rollTotal = session != null ? session.LastRollTotal : 0;
            int movesLeft = session != null ? session.RemainingMovementPoints : 0;
            int hearts = session != null ? session.Hearts : (config != null ? config.StartingHearts : 3);
            int maxHearts = config != null ? config.StartingHearts : 3;
            int stars = session != null ? session.StarsCollected : 0;
            int maxStars = config != null ? config.StarsRequired : 7;
            int charms = session != null ? session.Charms : (config != null ? config.StartingCharms : 1);
            int maxCharms = config != null ? config.MaxCharms : 3;
            int ward = session != null ? session.WardCharges : 0;

            string diceStr = rollTotal > 0 ? $"{dieA} + {dieB} = {rollTotal}" : "-";

            string status = $"<b>State:</b> {stateStr}\n" +
                            $"<b>Turn:</b> {turnNumber} / {maxTurns}\n" +
                            $"<b>Dice:</b> {diceStr}\n" +
                            $"<b>Moves left:</b> {movesLeft}\n" +
                            $"<b>Hearts:</b> {hearts} / {maxHearts}\n" +
                            $"<b>Stars:</b> {stars} / {maxStars}\n" +
                            $"<b>Charms:</b> {charms} / {maxCharms}\n" +
                            $"<b>Ward:</b> {ward}";

            if (_flowController != null)
            {
                if (_flowController.CurrentState == GameState.Victory)
                {
                    status += "\n\n<color=#FFD700><b>*** VICTORY! ***</b>\nFortune Gate Entered!</color>";
                }
                else if (_flowController.CurrentState == GameState.Defeat)
                {
                    status += "\n\n<color=#FF4444><b>*** DEFEAT! ***</b></color>";
                }
                else if (_flowController.CurrentState == GameState.Paused)
                {
                    status += "\n\n<color=#FFAA00><b>[PAUSED - Press Esc]</b></color>";
                }
            }

            if (!string.IsNullOrEmpty(_transientMessage))
            {
                status += $"\n\n<color=#FFAA00><b>{_transientMessage}</b></color>";
            }

            status += "\n\n<size=75%><color=#334155>Controls: [Space] Roll | [WASD] Move\n[Enter] End Turn | [C] Charm | [Esc] Pause</color></size>";

            _statusText.text = status;
        }
    }
}
