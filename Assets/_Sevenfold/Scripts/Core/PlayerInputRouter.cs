using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sevenfold.Core
{
    /// <summary>
    /// Translates raw InputSystem actions created at runtime from InputConfigSO into domain events.
    /// Contains NO game rules.
    /// </summary>
    public class PlayerInputRouter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InputConfigSO _config;
        [SerializeField] private bool _inputEnabled = true;

        private readonly List<InputAction> _actions = new List<InputAction>();

        public event Action RollPressed;
        public event Action<Direction> MovePressed;
        public event Action EndTurnPressed;
        public event Action UseCharmPressed;
        public event Action PausePressed;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        public InputConfigSO Config
        {
            get => _config;
            set
            {
                _config = value;
                if (isActiveAndEnabled)
                {
                    RebuildActions();
                }
            }
        }

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"[PlayerInputRouter] Missing InputConfigSO reference on {gameObject.name}.");
            }
        }

        private void OnEnable()
        {
            SetupActions();
        }

        public void RebuildActions()
        {
            TeardownActions();
            SetupActions();
        }

        private void SetupActions()
        {
            if (_config == null)
            {
                return;
            }

            CreateAction("Roll", _config.RollBindings, () =>
            {
                if (_inputEnabled) RollPressed?.Invoke();
            });

            CreateAction("Up", _config.UpBindings, () =>
            {
                if (_inputEnabled) MovePressed?.Invoke(Direction.Up);
            });

            CreateAction("Down", _config.DownBindings, () =>
            {
                if (_inputEnabled) MovePressed?.Invoke(Direction.Down);
            });

            CreateAction("Left", _config.LeftBindings, () =>
            {
                if (_inputEnabled) MovePressed?.Invoke(Direction.Left);
            });

            CreateAction("Right", _config.RightBindings, () =>
            {
                if (_inputEnabled) MovePressed?.Invoke(Direction.Right);
            });

            CreateAction("EndTurn", _config.EndTurnBindings, () =>
            {
                if (_inputEnabled) EndTurnPressed?.Invoke();
            });

            CreateAction("UseCharm", _config.UseCharmBindings, () =>
            {
                if (_inputEnabled) UseCharmPressed?.Invoke();
            });

            CreateAction("Pause", _config.PauseBindings, () =>
            {
                PausePressed?.Invoke();
            });
        }

        private void OnDisable()
        {
            TeardownActions();
        }

        private void TeardownActions()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action != null)
                {
                    action.Disable();
                    action.Dispose();
                }
            }
            _actions.Clear();
        }

        private void CreateAction(string name, string[] bindings, Action callback)
        {
            if (bindings == null || bindings.Length == 0)
            {
                return;
            }

            var action = new InputAction(name, InputActionType.Button);
            for (int i = 0; i < bindings.Length; i++)
            {
                string path = bindings[i];
                if (!string.IsNullOrEmpty(path))
                {
                    action.AddBinding(path);
                }
            }

            if (action.bindings.Count == 0)
            {
                action.Dispose();
                return;
            }

            action.performed += _ => callback();
            action.Enable();
            _actions.Add(action);
        }
    }
}
