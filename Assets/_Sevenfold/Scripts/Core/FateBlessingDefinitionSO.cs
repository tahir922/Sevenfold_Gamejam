using UnityEngine;

namespace Sevenfold.Core
{
    [CreateAssetMenu(fileName = "Blessing_", menuName = "Sevenfold/Luck/Blessing Definition")]
    public class FateBlessingDefinitionSO : ScriptableObject
    {
        [SerializeField] private BlessingKind _blessingKind = BlessingKind.FatesSight;
        [SerializeField] private string _title = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _revealRadius = 0;
        [SerializeField] private int _wardAmount = 0;
        [SerializeField] private int _rerollCount = 0;

        public BlessingKind BlessingKind { get => _blessingKind; set => _blessingKind = value; }
        public string Title { get => _title; set => _title = value; }
        public string Description { get => _description; set => _description = value; }
        public Sprite Icon { get => _icon; set => _icon = value; }
        public int RevealRadius { get => _revealRadius; set => _revealRadius = value; }
        public int WardAmount { get => _wardAmount; set => _wardAmount = value; }
        public int RerollCount { get => _rerollCount; set => _rerollCount = value; }
    }
}
