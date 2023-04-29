using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    public enum BodyPartSlot
    {
        ROOT,
        BRIEFS,
        CHEST,
        CHESTGUARD,
        L_ARM,
        L_KNEEGUARD,
        L_LEG,
        L_SHOULDER,
        L_EARPIECE,
        WAISTGUARD,
        R_ARM,
        R_KNEEGUARD,
        R_LEG,
        R_SHOULDER,
        R_EARPIECE,
        MASK,
        HAIR
    }

    public enum BodySection
    {
        HEAD,
        FACE,
        CHEST,
        ARMS,
        WAIST,
        LEGS
    }

    [System.Serializable]
    public class ArmorPiece
    {
        public Mesh ArmorMesh;
        public BodyPartSlot BodyPart;
    }

    [CreateAssetMenu(menuName = "Armor Data")]
    public class ArmorData : ScriptableObject
    {
        [Header("Armor Details")]
        [SerializeField]
        private ArmorPiece[] _armorPieces;
        [SerializeField]
        private Material _armorMaterial;
        [SerializeField]
        private BodySection _bodySection;

        [Header("Display Settings")]
        [SerializeField]
        private string _armorSetName;
        [SerializeField]
        private Sprite _displayIcon;

        public Material ArmorMaterial { get => _armorMaterial; set => _armorMaterial = value; }
        public Sprite DisplayIcon { get => _displayIcon; set => _displayIcon = value; }
        public ArmorPiece[] ArmorPieces { get => _armorPieces; set => _armorPieces = value; }
        public BodySection BodySection { get => _bodySection; }
        public string ArmorSetName { get => _armorSetName; }
    }
}