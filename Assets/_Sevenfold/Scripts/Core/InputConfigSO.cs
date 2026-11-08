using UnityEngine;

namespace Sevenfold.Core
{
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Sevenfold/Config/Input Config")]
    public class InputConfigSO : ScriptableObject
    {
        [Header("Action Bindings")]
        [SerializeField] private string[] _rollBindings = { "<Keyboard>/space" };
        [SerializeField] private string[] _upBindings = { "<Keyboard>/w", "<Keyboard>/upArrow" };
        [SerializeField] private string[] _downBindings = { "<Keyboard>/s", "<Keyboard>/downArrow" };
        [SerializeField] private string[] _leftBindings = { "<Keyboard>/a", "<Keyboard>/leftArrow" };
        [SerializeField] private string[] _rightBindings = { "<Keyboard>/d", "<Keyboard>/rightArrow" };
        [SerializeField] private string[] _endTurnBindings = { "<Keyboard>/enter", "<Keyboard>/numpadEnter" };
        [SerializeField] private string[] _useCharmBindings = { "<Keyboard>/c" };
        [SerializeField] private string[] _pauseBindings = { "<Keyboard>/escape" };

        public string[] RollBindings
        {
            get => _rollBindings;
            set => _rollBindings = value;
        }

        public string[] UpBindings
        {
            get => _upBindings;
            set => _upBindings = value;
        }

        public string[] DownBindings
        {
            get => _downBindings;
            set => _downBindings = value;
        }

        public string[] LeftBindings
        {
            get => _leftBindings;
            set => _leftBindings = value;
        }

        public string[] RightBindings
        {
            get => _rightBindings;
            set => _rightBindings = value;
        }

        public string[] EndTurnBindings
        {
            get => _endTurnBindings;
            set => _endTurnBindings = value;
        }

        public string[] UseCharmBindings
        {
            get => _useCharmBindings;
            set => _useCharmBindings = value;
        }

        public string[] PauseBindings
        {
            get => _pauseBindings;
            set => _pauseBindings = value;
        }
    }
}
