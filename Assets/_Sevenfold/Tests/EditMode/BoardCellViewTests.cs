using NUnit.Framework;
using Sevenfold.Board;
using Sevenfold.Core;
using TMPro;
using UnityEngine;

namespace Sevenfold.Tests
{
    [TestFixture]
    public class BoardCellViewTests
    {
        private GameObject _cellGo;
        private BoardCellView _cellView;
        private TileDefinitionSO _tileNormal;
        private TileDefinitionSO _tileTrap;
        private TileDefinitionSO _tileStar;
        private TileDefinitionSO _tileGate;
        private TileDefinitionSO _tileGap;

        private MeshRenderer _tileRenderer;
        private TextMeshPro _numberText;
        private TextMeshPro _symbolText;
        private TextMeshPro _badgeText;
        private GameObject _gateArchRoot;

        [SetUp]
        public void SetUp()
        {
            _tileNormal = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileNormal.TileKind = TileKind.Normal;
            _tileNormal.BaseColor = Color.gray;
            _tileNormal.HiddenUntilRevealed = false;

            _tileTrap = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileTrap.TileKind = TileKind.Trap;
            _tileTrap.BaseColor = Color.magenta;
            _tileTrap.HiddenUntilRevealed = true;

            _tileStar = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileStar.TileKind = TileKind.Star;
            _tileStar.BaseColor = Color.yellow;
            _tileStar.HiddenUntilRevealed = false;

            _tileGate = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileGate.TileKind = TileKind.Gate;
            _tileGate.BaseColor = new Color(0.3f, 0.1f, 0.4f);
            _tileGate.HiddenUntilRevealed = false;

            _tileGap = ScriptableObject.CreateInstance<TileDefinitionSO>();
            _tileGap.TileKind = TileKind.Gap;
            _tileGap.HiddenUntilRevealed = false;

            _cellGo = new GameObject("TestCell");
            _cellView = _cellGo.AddComponent<BoardCellView>();

            var meshGo = new GameObject("TileMesh");
            meshGo.transform.SetParent(_cellGo.transform);
            _tileRenderer = meshGo.AddComponent<MeshRenderer>();

            var numGo = new GameObject("NumberLabel");
            numGo.transform.SetParent(_cellGo.transform);
            _numberText = numGo.AddComponent<TextMeshPro>();

            var symGo = new GameObject("SymbolLabel");
            symGo.transform.SetParent(_cellGo.transform);
            _symbolText = symGo.AddComponent<TextMeshPro>();

            var badgeGo = new GameObject("BadgeLabel");
            badgeGo.transform.SetParent(_cellGo.transform);
            _badgeText = badgeGo.AddComponent<TextMeshPro>();

            _gateArchRoot = new GameObject("GateArch");
            _gateArchRoot.transform.SetParent(_cellGo.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_cellGo != null) Object.DestroyImmediate(_cellGo);
            if (_tileNormal != null) Object.DestroyImmediate(_tileNormal);
            if (_tileTrap != null) Object.DestroyImmediate(_tileTrap);
            if (_tileStar != null) Object.DestroyImmediate(_tileStar);
            if (_tileGate != null) Object.DestroyImmediate(_tileGate);
            if (_tileGap != null) Object.DestroyImmediate(_tileGap);
        }

        [Test]
        public void Initialize_TrapTile_StartsHiddenWithNumberActive()
        {
            _cellView.Initialize(new Vector2Int(4, 5), _tileTrap, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            Assert.AreEqual(CellVisualState.Hidden, _cellView.VisualState);
            Assert.IsTrue(_numberText.gameObject.activeSelf);
            Assert.IsFalse(_symbolText.gameObject.activeSelf);
            Assert.IsFalse(_badgeText.gameObject.activeSelf);
        }

        [Test]
        public void Initialize_StarTile_StartsRevealedWithSpecialLabels()
        {
            _cellView.Initialize(new Vector2Int(1, 6), _tileStar, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            Assert.AreEqual(CellVisualState.Revealed, _cellView.VisualState);
            Assert.IsFalse(_numberText.gameObject.activeSelf);
            Assert.IsTrue(_symbolText.gameObject.activeSelf);
            Assert.IsTrue(_badgeText.gameObject.activeSelf);
        }

        [Test]
        public void HiddenTrap_WhenHighlighted_PreservesDisguiseAndRestoresOnUnhighlight()
        {
            _cellView.Initialize(new Vector2Int(4, 5), _tileTrap, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            // Highlight reachable cell
            _cellView.SetHighlighted(true);
            Assert.AreEqual(CellVisualState.Highlighted, _cellView.VisualState);
            // Label disguise must still show number, NOT trap symbol
            Assert.IsTrue(_numberText.gameObject.activeSelf, "Hidden trap when highlighted must show engraved number.");
            Assert.IsFalse(_symbolText.gameObject.activeSelf, "Hidden trap when highlighted must not reveal trap symbol.");
            Assert.IsFalse(_badgeText.gameObject.activeSelf, "Hidden trap when highlighted must not reveal badge.");

            // Unhighlight
            _cellView.SetHighlighted(false);
            Assert.AreEqual(CellVisualState.Hidden, _cellView.VisualState, "Unhighlighting hidden trap must restore Hidden state.");
            Assert.IsTrue(_numberText.gameObject.activeSelf);
            Assert.IsFalse(_symbolText.gameObject.activeSelf);
        }

        [Test]
        public void RevealedTrap_WhenHighlightedAndUnhighlighted_StaysRevealed()
        {
            _cellView.Initialize(new Vector2Int(4, 5), _tileTrap, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            // Reveal trap via proximity
            _cellView.SetRevealed(true);
            Assert.AreEqual(CellVisualState.Revealed, _cellView.VisualState);

            // Highlight and then unhighlight
            _cellView.SetHighlighted(true);
            Assert.AreEqual(CellVisualState.Highlighted, _cellView.VisualState);

            _cellView.SetHighlighted(false);
            Assert.AreEqual(CellVisualState.Revealed, _cellView.VisualState,
                "Unhighlighting an already-revealed trap must NOT revert back to Hidden.");
        }

        [Test]
        public void ConsumedStar_WhenOccupiedAndUnoccupied_StaysConsumed()
        {
            _cellView.Initialize(new Vector2Int(1, 6), _tileStar, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            _cellView.SetConsumed(true);
            Assert.AreEqual(CellVisualState.Consumed, _cellView.VisualState);
            Assert.AreEqual("EMPTY", _badgeText.text);

            _cellView.SetOccupied(true);
            Assert.AreEqual(CellVisualState.Occupied, _cellView.VisualState);

            _cellView.SetOccupied(false);
            Assert.AreEqual(CellVisualState.Consumed, _cellView.VisualState,
                "Unoccupying a consumed star must stay in Consumed state.");
        }

        [Test]
        public void GateOpen_TogglesStateCorrectly()
        {
            _cellView.Initialize(new Vector2Int(6, 6), _tileGate, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            Assert.IsFalse(_cellView.GateOpen);
            Assert.IsFalse(_cellView.IsGateOpen);

            _cellView.GateOpen = true;
            Assert.IsTrue(_cellView.GateOpen);
            Assert.IsTrue(_cellView.IsGateOpen);

            _cellView.SetGateOpen(false);
            Assert.IsFalse(_cellView.GateOpen);
            Assert.IsFalse(_cellView.IsGateOpen);
        }

        [Test]
        public void GapTile_DisablesRendererAndLabels()
        {
            _cellView.Initialize(new Vector2Int(2, 5), _tileGap, _tileNormal, 7,
                _tileRenderer, _numberText, _symbolText, _badgeText, _gateArchRoot, null);

            Assert.IsFalse(_tileRenderer.enabled);
            Assert.IsFalse(_numberText.gameObject.activeSelf);
            Assert.IsFalse(_symbolText.gameObject.activeSelf);
            Assert.IsFalse(_badgeText.gameObject.activeSelf);
            Assert.IsFalse(_gateArchRoot.activeSelf);
        }
    }
}
