using UnityEngine;

namespace Sevenfold.Core
{
    [CreateAssetMenu(fileName = "Tile_", menuName = "Sevenfold/Tiles/Tile Definition")]
    public class TileDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _stableId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private TileKind _tileKind = TileKind.Normal;
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField] private int _effectAmount = 0;
        [SerializeField] private bool _isOneShot = false;
        [SerializeField] private bool _hiddenUntilRevealed = false;
        [SerializeField] private string _feedbackEventId = string.Empty;

        public string StableId { get => _stableId; set => _stableId = value; }
        public string DisplayName { get => _displayName; set => _displayName = value; }
        public TileKind TileKind { get => _tileKind; set => _tileKind = value; }
        public string Description { get => _description; set => _description = value; }
        public Sprite Icon { get => _icon; set => _icon = value; }
        public Color BaseColor { get => _baseColor; set => _baseColor = value; }
        public int EffectAmount { get => _effectAmount; set => _effectAmount = value; }
        public bool IsOneShot { get => _isOneShot; set => _isOneShot = value; }
        public bool HiddenUntilRevealed { get => _hiddenUntilRevealed; set => _hiddenUntilRevealed = value; }
        public string FeedbackEventId { get => _feedbackEventId; set => _feedbackEventId = value; }
    }
}
