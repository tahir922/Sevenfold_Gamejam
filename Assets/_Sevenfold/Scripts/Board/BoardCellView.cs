using Sevenfold.Core;
using TMPro;
using UnityEngine;

namespace Sevenfold.Board
{
    public enum CellVisualState
    {
        Hidden,
        Revealed,
        Consumed,
        Highlighted,
        Occupied
    }

    /// <summary>
    /// Presentation component for an individual board cell.
    /// Manages graybox visuals, engraved numbers, placeholder symbols, text badges,
    /// gate archway, and visual states using MaterialPropertyBlock for URP Lit materials.
    /// </summary>
    [SelectionBase]
    [ExecuteAlways]
    public class BoardCellView : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [Header("Data")]
        [SerializeField] private Vector2Int _coord;
        [SerializeField] private TileDefinitionSO _tileDefinition;
        [SerializeField] private TileDefinitionSO _defaultNormalTile;
        [SerializeField] private CellVisualState _visualState = CellVisualState.Hidden;
        [SerializeField] private bool _isGateOpen = false;

        [Header("Internal State Tracking")]
        [SerializeField] private bool _isRevealed = true;
        [SerializeField] private bool _isConsumed = false;
        [SerializeField] private bool _isHighlighted = false;
        [SerializeField] private bool _isOccupied = false;

        [Header("Visual Components")]
        [SerializeField] private MeshRenderer _tileRenderer;
        [SerializeField] private TextMeshPro _numberText;
        [SerializeField] private TextMeshPro _symbolText;
        [SerializeField] private TextMeshPro _badgeText;
        [SerializeField] private GameObject _gateArchRoot;
        [SerializeField] private MeshRenderer[] _gateArchRenderers;

        private MaterialPropertyBlock _propBlock;

        public Vector2Int Coord => _coord;
        public TileDefinitionSO TileDefinition => _tileDefinition;
        public CellVisualState VisualState
        {
            get
            {
                if (_isOccupied) return CellVisualState.Occupied;
                if (_isHighlighted) return CellVisualState.Highlighted;
                if (_isConsumed) return CellVisualState.Consumed;
                if (!_isRevealed && _tileDefinition != null && _tileDefinition.HiddenUntilRevealed)
                    return CellVisualState.Hidden;
                return CellVisualState.Revealed;
            }
        }

        public bool GateOpen
        {
            get => _isGateOpen;
            set => SetGateOpen(value);
        }

        public bool IsGateOpen => _isGateOpen;

        public MeshRenderer TileRenderer => _tileRenderer;
        public TextMeshPro NumberText => _numberText;
        public TextMeshPro SymbolText => _symbolText;
        public TextMeshPro BadgeText => _badgeText;

        private void OnEnable()
        {
            RefreshVisuals();
        }

        public void Initialize(
            Vector2Int coord,
            TileDefinitionSO tileDef,
            TileDefinitionSO defaultNormalTile,
            int boardWidth,
            MeshRenderer tileRenderer,
            TextMeshPro numberText,
            TextMeshPro symbolText,
            TextMeshPro badgeText,
            GameObject gateArchRoot,
            MeshRenderer[] gateArchRenderers)
        {
            _coord = coord;
            _tileDefinition = tileDef;
            _defaultNormalTile = defaultNormalTile;
            _tileRenderer = tileRenderer;
            _numberText = numberText;
            _symbolText = symbolText;
            _badgeText = badgeText;
            _gateArchRoot = gateArchRoot;
            _gateArchRenderers = gateArchRenderers;

            // Compute standard engraved number (y * boardWidth + x + 1)
            int engravedNumber = coord.y * boardWidth + coord.x + 1;
            if (_numberText != null)
            {
                _numberText.text = engravedNumber.ToString();
            }

            // Traps and Charms start Hidden by rule; all other tiles start Revealed
            bool startsHidden = tileDef != null && tileDef.HiddenUntilRevealed;
            _isRevealed = !startsHidden;
            _isConsumed = false;
            _isHighlighted = false;
            _isOccupied = false;
            _isGateOpen = false;

            RefreshVisuals();
        }

        public void SetVisualState(CellVisualState state)
        {
            switch (state)
            {
                case CellVisualState.Hidden:
                    _isRevealed = false;
                    _isConsumed = false;
                    _isHighlighted = false;
                    _isOccupied = false;
                    break;
                case CellVisualState.Revealed:
                    _isRevealed = true;
                    _isConsumed = false;
                    _isHighlighted = false;
                    _isOccupied = false;
                    break;
                case CellVisualState.Consumed:
                    _isConsumed = true;
                    _isHighlighted = false;
                    _isOccupied = false;
                    break;
                case CellVisualState.Highlighted:
                    _isHighlighted = true;
                    break;
                case CellVisualState.Occupied:
                    _isOccupied = true;
                    break;
            }
            RefreshVisuals();
        }

        public void SetRevealed(bool revealed)
        {
            _isRevealed = revealed;
            RefreshVisuals();
        }

        public void SetConsumed(bool consumed)
        {
            _isConsumed = consumed;
            RefreshVisuals();
        }

        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            RefreshVisuals();
        }

        public void SetOccupied(bool occupied)
        {
            _isOccupied = occupied;
            RefreshVisuals();
        }

        public void SetGateOpen(bool open)
        {
            _isGateOpen = open;
            RefreshGateVisuals();
        }

        public void RefreshVisuals()
        {
            if (_tileDefinition == null) return;

            _visualState = VisualState;

            // Gap tiles are not drawn as physical platforms
            if (_tileDefinition.TileKind == TileKind.Gap)
            {
                if (_tileRenderer != null) _tileRenderer.enabled = false;
                if (_numberText != null) _numberText.gameObject.SetActive(false);
                if (_symbolText != null) _symbolText.gameObject.SetActive(false);
                if (_badgeText != null) _badgeText.gameObject.SetActive(false);
                if (_gateArchRoot != null) _gateArchRoot.SetActive(false);
                return;
            }

            if (_tileRenderer != null) _tileRenderer.enabled = true;

            // Determine underlying disguise/revelation state
            bool isHidden = !_isRevealed && _tileDefinition.HiddenUntilRevealed;
            Color baseColor;

            if (isHidden)
            {
                // Disguised identically to normal numbered tile
                baseColor = _defaultNormalTile != null ? _defaultNormalTile.BaseColor : new Color(0.66f, 0.835f, 0.82f);
                ConfigureLabelsForNormal();
            }
            else if (_isConsumed)
            {
                // Dimmed consumed state
                baseColor = _tileDefinition.BaseColor * 0.45f;
                baseColor.a = 1f;
                ConfigureLabelsForConsumed();
            }
            else
            {
                // Revealed tile
                baseColor = _tileDefinition.BaseColor;
                if (_tileDefinition.TileKind == TileKind.Normal)
                {
                    ConfigureLabelsForNormal();
                }
                else
                {
                    ConfigureLabelsForSpecial();
                }
            }

            // Apply highlight or occupied overlays
            if (_isOccupied)
            {
                Color occupiedColor = Color.Lerp(baseColor, Color.cyan, 0.35f);
                SetTileColor(occupiedColor);
            }
            else if (_isHighlighted)
            {
                Color highlightColor = Color.Lerp(baseColor, Color.yellow, 0.45f) + new Color(0.15f, 0.15f, 0.15f, 0f);
                SetTileColor(highlightColor);
            }
            else
            {
                SetTileColor(baseColor);
            }

            // Gate archway presence and color
            bool isGate = _tileDefinition.TileKind == TileKind.Gate;
            if (_gateArchRoot != null)
            {
                _gateArchRoot.SetActive(isGate);
            }
            if (isGate)
            {
                RefreshGateVisuals();
            }
        }

        private void ConfigureLabelsForNormal()
        {
            if (_numberText != null)
            {
                _numberText.gameObject.SetActive(true);
                _numberText.color = new Color(0.18f, 0.22f, 0.25f, 1f);
            }
            if (_symbolText != null) _symbolText.gameObject.SetActive(false);
            if (_badgeText != null) _badgeText.gameObject.SetActive(false);
        }

        private void ConfigureLabelsForSpecial()
        {
            if (_numberText != null) _numberText.gameObject.SetActive(false);

            if (_symbolText != null)
            {
                _symbolText.gameObject.SetActive(true);
                _symbolText.text = GetSymbolGlyph(_tileDefinition.TileKind, _symbolText.font);
                _symbolText.color = Color.white;
            }

            if (_badgeText != null)
            {
                _badgeText.gameObject.SetActive(true);
                _badgeText.text = GetBadgeLabel(_tileDefinition.TileKind);
                _badgeText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            }
        }

        private void ConfigureLabelsForConsumed()
        {
            if (_numberText != null) _numberText.gameObject.SetActive(false);

            if (_symbolText != null)
            {
                _symbolText.gameObject.SetActive(true);
                _symbolText.text = _tileDefinition.TileKind == TileKind.Star ? "o" : "-";
                _symbolText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }

            if (_badgeText != null)
            {
                _badgeText.gameObject.SetActive(true);
                _badgeText.text = _tileDefinition.TileKind switch
                {
                    TileKind.Star => "EMPTY",
                    TileKind.Trap => "SPENT",
                    TileKind.Charm => "USED",
                    _ => "DONE"
                };
                _badgeText.color = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            }
        }

        private void RefreshGateVisuals()
        {
            if (_gateArchRenderers == null || _gateArchRenderers.Length == 0) return;

            // Closed = dark purple, Open = glowing gold
            Color archColor = _isGateOpen
                ? new Color(1.0f, 0.84f, 0.0f, 1f)  // Glowing Gold (#FFD700)
                : new Color(0.24f, 0.12f, 0.43f, 1f); // Dark Purple (#3D1E6D)

            for (int i = 0; i < _gateArchRenderers.Length; i++)
            {
                var r = _gateArchRenderers[i];
                if (r != null)
                {
                    ApplyRendererColor(r, archColor);
                }
            }
        }

        private void SetTileColor(Color color)
        {
            if (_tileRenderer != null)
            {
                ApplyRendererColor(_tileRenderer, color);
            }
        }

        private void ApplyRendererColor(Renderer targetRenderer, Color color)
        {
            if (targetRenderer == null) return;
            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorProperty, color);
            targetRenderer.SetPropertyBlock(_propBlock);
        }

        private static string GetSymbolGlyph(TileKind kind, TMP_FontAsset font)
        {
            bool hasChar(char c) => font != null && font.HasCharacter(c);

            return kind switch
            {
                TileKind.Star => hasChar('★') ? "★" : "*",
                TileKind.Trap => "!",
                TileKind.Charm => hasChar('♣') ? "♣" : "+",
                TileKind.Gate => hasChar('⛩') ? "⛩" : "G",
                TileKind.Start => "S",
                _ => string.Empty
            };
        }

        private static string GetBadgeLabel(TileKind kind)
        {
            return kind switch
            {
                TileKind.Star => "STAR",
                TileKind.Trap => "TRAP",
                TileKind.Charm => "CHARM",
                TileKind.Gate => "GATE",
                TileKind.Start => "START",
                _ => string.Empty
            };
        }
    }
}
