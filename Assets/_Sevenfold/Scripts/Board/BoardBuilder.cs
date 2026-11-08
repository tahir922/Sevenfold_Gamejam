using System.Collections.Generic;
using Sevenfold.Core;
using TMPro;
using UnityEngine;

namespace Sevenfold.Board
{
    /// <summary>
    /// Runtime and editor builder for the 7x7 graybox board.
    /// Reads BoardDefinitionSO and instantiates the cell views, meshes, labels, and archway.
    /// Exposes BoardGrid and coordinate-to-view lookup.
    /// </summary>
    [SelectionBase]
    public class BoardBuilder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardDefinitionSO _board;
        [SerializeField] private Transform _cellsRoot;
        [SerializeField] private Material _tileMaterial;

        private readonly Dictionary<Vector2Int, BoardCellView> _cellViews = new Dictionary<Vector2Int, BoardCellView>();

        public BoardDefinitionSO BoardDefinition
        {
            get => _board;
            set => _board = value;
        }

        public Transform CellsRoot
        {
            get => _cellsRoot;
            set => _cellsRoot = value;
        }

        public Material TileMaterial
        {
            get => _tileMaterial;
            set => _tileMaterial = value;
        }

        public BoardGrid Grid { get; private set; }

        public IReadOnlyDictionary<Vector2Int, BoardCellView> CellViews => _cellViews;

        private void Awake()
        {
            Build();
        }

        /// <summary>
        /// Builds the visible board cells. Cleans up any existing children of cellsRoot first.
        /// Can be safely invoked in Awake at runtime or by scene builders in the editor.
        /// </summary>
        public void Build()
        {
            if (_board == null)
            {
                Debug.LogError($"[BoardBuilder] Missing BoardDefinitionSO reference on {gameObject.name}. Cannot build board.");
                return;
            }

            Grid = new BoardGrid(_board);
            _cellViews.Clear();

            if (_cellsRoot == null)
            {
                _cellsRoot = transform;
            }

            // Clean up existing cell children (idempotent rebuild)
            var existingChildren = new List<GameObject>();
            foreach (Transform child in _cellsRoot)
            {
                existingChildren.Add(child.gameObject);
            }

            for (int i = existingChildren.Count - 1; i >= 0; i--)
            {
                var childGo = existingChildren[i];
                if (childGo != null)
                {
                    childGo.transform.SetParent(null);
                    if (Application.isPlaying)
                    {
                        Destroy(childGo);
                    }
                    else
                    {
                        DestroyImmediate(childGo);
                    }
                }
            }

            // Ensure tile material fallback if unassigned
            if (_tileMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                if (shader != null)
                {
                    _tileMaterial = new Material(shader);
                    _tileMaterial.name = "M_Tile_RuntimeDefault";
                }
            }

            // Build all cells from data
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    var coord = new Vector2Int(x, y);
                    var cellView = CreateCell(coord);
                    if (cellView != null)
                    {
                        _cellViews[coord] = cellView;
                    }
                }
            }

            Debug.Log($"[BoardBuilder] Successfully built {_cellViews.Count} board cells for {_board.name} ({_board.Width}x{_board.Height}).");
        }

        /// <summary>
        /// Retrieves the view corresponding to coordinate c, or null if out of bounds.
        /// </summary>
        public BoardCellView GetView(Vector2Int coord)
        {
            if (_cellViews.TryGetValue(coord, out var view))
            {
                return view;
            }

            // Fallback for editor mode or after domain reload before Awake runs
            if (_cellsRoot != null)
            {
                var child = _cellsRoot.Find($"Cell_{coord.x}_{coord.y}");
                if (child != null && child.TryGetComponent<BoardCellView>(out view))
                {
                    _cellViews[coord] = view;
                    return view;
                }
            }

            return null;
        }

        private BoardCellView CreateCell(Vector2Int coord)
        {
            var tileDef = _board.GetTileAt(coord);

            var cellGo = new GameObject($"Cell_{coord.x}_{coord.y}");
            cellGo.transform.SetParent(_cellsRoot, false);
            cellGo.transform.localPosition = Grid.CellToLocal(coord);
            cellGo.transform.localRotation = Quaternion.identity;
            cellGo.transform.localScale = Vector3.one;

            // 1. Platform cube mesh (scaled tileTopSize, 0.3, tileTopSize)
            var cubeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeGo.name = "TileMesh";
            cubeGo.transform.SetParent(cellGo.transform, false);
            // Center is at y = -0.15f so top surface is at local y = 0f
            cubeGo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            cubeGo.transform.localScale = new Vector3(_board.TileTopSize, 0.3f, _board.TileTopSize);

            // Gaps have no physical platform or collision
            var boxCol = cubeGo.GetComponent<Collider>();
            if (tileDef != null && tileDef.TileKind == TileKind.Gap && boxCol != null)
            {
                boxCol.enabled = false;
            }

            var tileRenderer = cubeGo.GetComponent<MeshRenderer>();
            if (_tileMaterial != null)
            {
                tileRenderer.sharedMaterial = _tileMaterial;
            }

            // 2. Labels lying flat on top surface (y = 0.01f to avoid z-fighting)
            // Rotated (90, 0, -45) so they lie flat on the tile top and read perfectly horizontal from the 45-degree isometric camera
            var numberText = CreateTMPLabel(cellGo.transform, "NumberLabel", new Vector3(0f, 0.01f, 0f), 4.5f);
            var symbolText = CreateTMPLabel(cellGo.transform, "SymbolLabel", new Vector3(0.12f, 0.012f, 0.12f), 6.5f);
            var badgeText = CreateTMPLabel(cellGo.transform, "BadgeText", new Vector3(-0.13f, 0.012f, -0.13f), 2.4f);

            // 3. Gate Archway (only on Gate cell)
            GameObject archRoot = null;
            MeshRenderer[] archRenderers = null;

            if (tileDef != null && tileDef.TileKind == TileKind.Gate)
            {
                archRoot = new GameObject("GateArch");
                archRoot.transform.SetParent(cellGo.transform, false);
                archRoot.transform.localPosition = Vector3.zero;
                // Rotate arch by 45 degrees so its wide opening faces the isometric camera directly
                archRoot.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

                // Two tall cylinders and a top cross bar
                var leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leftPillar.name = "LeftPillar";
                leftPillar.transform.SetParent(archRoot.transform, false);
                leftPillar.transform.localPosition = new Vector3(-0.38f, 0.6f, 0f);
                leftPillar.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);

                var rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rightPillar.name = "RightPillar";
                rightPillar.transform.SetParent(archRoot.transform, false);
                rightPillar.transform.localPosition = new Vector3(0.38f, 0.6f, 0f);
                rightPillar.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);

                var topLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topLintel.name = "TopLintel";
                topLintel.transform.SetParent(archRoot.transform, false);
                topLintel.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                topLintel.transform.localScale = new Vector3(0.95f, 0.14f, 0.18f);

                archRenderers = new[]
                {
                    leftPillar.GetComponent<MeshRenderer>(),
                    rightPillar.GetComponent<MeshRenderer>(),
                    topLintel.GetComponent<MeshRenderer>()
                };

                for (int i = 0; i < archRenderers.Length; i++)
                {
                    if (_tileMaterial != null)
                    {
                        archRenderers[i].sharedMaterial = _tileMaterial;
                    }
                }
            }

            var cellView = cellGo.AddComponent<BoardCellView>();
            cellView.Initialize(
                coord,
                tileDef,
                _board.DefaultNormalTile,
                _board.Width,
                tileRenderer,
                numberText,
                symbolText,
                badgeText,
                archRoot,
                archRenderers);

            return cellView;
        }

        private static TextMeshPro CreateTMPLabel(Transform parent, string name, Vector3 localPos, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, -45f);
            go.transform.localScale = Vector3.one;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }
    }
}
