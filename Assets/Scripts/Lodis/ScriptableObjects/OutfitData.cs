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
        LEGS,
        FEET
    }

    [System.Serializable]
    public class ArmorPiece
    {
        public Mesh ArmorMesh;
        public BodyPartSlot BodyPart;
    }

    [CreateAssetMenu(menuName = "Armor Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("Armor Details")]
        [SerializeField]
        private string[] _IDs;
        [SerializeField]
        private Color _hairColor;
        [SerializeField]
        private Color _bodyColor;

        [SerializeField]
        private string _outfitName;

        public string OutfitName { get => _outfitName; }
        public string[] IDs { get => _IDs; set => _IDs = value; }
    }
}