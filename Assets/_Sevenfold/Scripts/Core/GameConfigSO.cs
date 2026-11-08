using System.Collections.Generic;
using UnityEngine;

namespace Sevenfold.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Sevenfold/Config/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Board Settings")]
        [SerializeField] private int _boardWidth = 7;
        [SerializeField] private int _boardHeight = 7;
        [SerializeField] private float _cellSpacing = 1.25f;
        [SerializeField] private float _tileTopSize = 1.12f;

        [Header("Rules & Progression")]
        [SerializeField] private int _diceCount = 2;
        [SerializeField] private int _diceSides = 6;
        [SerializeField] private int _luckyNumber = 7;
        [SerializeField] private int _starsRequired = 7;
        [SerializeField] private int _startingHearts = 3;
        [SerializeField] private int _startingCharms = 1;
        [SerializeField] private int _maxCharms = 3;
        [SerializeField] private int _maxTurns = 7;

        [Header("Turn Timing")]
        [SerializeField] private bool _autoEndTurn = true;
        [SerializeField] private float _autoEndTurnDelay = 0.3f;

        [Header("Movement & Animation")]
        [SerializeField] private float _playerMoveDuration = 0.22f;
        [SerializeField] private float _playerHopHeight = 0.12f;
        [SerializeField] private float _diceAnimDuration = 0.7f;
        [SerializeField] private int _fatesSightRadius = 3;

        [Header("Audio & Feel")]
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.75f;
        [SerializeField] private bool _cameraShakeEnabled = true;

        [Header("Development & Testing")]
        [SerializeField] private bool _useFixedSeed = false;
        [SerializeField] private int _fixedSeed = 0;
        [SerializeField] private List<Vector2Int> _forcedRolls = new List<Vector2Int>();

        public int BoardWidth { get => _boardWidth; set => _boardWidth = value; }
        public int BoardHeight { get => _boardHeight; set => _boardHeight = value; }
        public float CellSpacing { get => _cellSpacing; set => _cellSpacing = value; }
        public float TileTopSize { get => _tileTopSize; set => _tileTopSize = value; }
        public int DiceCount { get => _diceCount; set => _diceCount = value; }
        public int DiceSides { get => _diceSides; set => _diceSides = value; }
        public int LuckyNumber { get => _luckyNumber; set => _luckyNumber = value; }
        public int StarsRequired { get => _starsRequired; set => _starsRequired = value; }
        public int StartingHearts { get => _startingHearts; set => _startingHearts = value; }
        public int StartingCharms { get => _startingCharms; set => _startingCharms = value; }
        public int MaxCharms { get => _maxCharms; set => _maxCharms = value; }
        public int MaxTurns { get => _maxTurns; set => _maxTurns = value; }
        public bool AutoEndTurn { get => _autoEndTurn; set => _autoEndTurn = value; }
        public float AutoEndTurnDelay { get => _autoEndTurnDelay; set => _autoEndTurnDelay = value; }
        public float PlayerMoveDuration { get => _playerMoveDuration; set => _playerMoveDuration = value; }
        public float PlayerHopHeight { get => _playerHopHeight; set => _playerHopHeight = value; }
        public float DiceAnimDuration { get => _diceAnimDuration; set => _diceAnimDuration = value; }
        public int FatesSightRadius { get => _fatesSightRadius; set => _fatesSightRadius = value; }
        public float MusicVolume { get => _musicVolume; set => _musicVolume = value; }
        public float SfxVolume { get => _sfxVolume; set => _sfxVolume = value; }
        public bool CameraShakeEnabled { get => _cameraShakeEnabled; set => _cameraShakeEnabled = value; }
        public bool UseFixedSeed { get => _useFixedSeed; set => _useFixedSeed = value; }
        public int FixedSeed { get => _fixedSeed; set => _fixedSeed = value; }
        public List<Vector2Int> ForcedRolls { get => _forcedRolls; set => _forcedRolls = value; }
    }
}
