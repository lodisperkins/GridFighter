using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Lodis.ScriptableObjects
{
    public enum AnimationType
    {
        CAST,
        SUMMON,
        MELEE,
        CUSTOM
    }

    [System.Serializable]
    public class Stat
    {
        public Stat(string newName, float newValue)
        {
            name = newName;
            value = newValue;
        }
        public string name;
        public float value;
    }

    [CreateAssetMenu(menuName = "AbilityData/Default")]
    public class AbilityData : ScriptableObject
    {
        
        public string abilityName = "Unassigned";
        [Tooltip("The type describes the strength and input value for the ability")]
        public AbilityType AbilityType;
        [Tooltip("How long the ability should be active for")]
        public float timeActive = 0;
        [Tooltip("How long does the object that used the ability needs before returning to idle")]
        public float recoverTime = 0;
        [Tooltip("How long does the object that used the ability must wait before the ability activates")]
        public float startUpTime = 0;
        [Tooltip("The amount of time this ability must be in an active slot before it can be used. Can be ignored if this ability is a normal type.")]
        public float chargeTime = 0;
        [Tooltip("The amount of time this ability can be used before it is removed from the active slot. Can be ignored if this ability is a normal type.")]
        public int maxActivationAmount = 0;
        [Tooltip("If true, this ability can be canceled into others in the start up phase")]
        public bool canCancelStartUp = false;
        [Tooltip("If true, this ability can be canceled into others in the active phase")]
        public bool canCancelActive = false;
        [Tooltip("If true, this ability can be canceled into others in the recover phase")]
        public bool canCancelRecover = false;
        [Tooltip("If true, this ability can be canceled when the player inputs movement")]
        public bool CanCancelOnMove = false;
        [Tooltip("If true, this ability can be canceled if it is used again")]
        public bool CanCancelIntoSelf = false;
        [Tooltip("If false, the user of this ability can't move while it's active")]
        public bool CanInputMovementWhileActive = true;
        [Tooltip("If true, this ability will be canceled when the user is hit")]
        public bool cancelOnHit = false;
        [Tooltip("If true, this ability will be canceled when the user is flinching")]
        public bool cancelOnFlinch = false;
        [Tooltip("If true, this ability will be canceled when the user is in knockback")]
        public bool cancelOnKnockback = true;
        [Tooltip("If true, the animation will change speed according to the start, active, and recover times")]
        public bool useAbilityTimingForAnimation;
        [Tooltip("If true, the animation will only play when specified in the ability script")]
        public bool playAnimationManually = false;
        [Tooltip("The prefab that holds the visual this ability will be using.")]
        public GameObject visualPrefab;
        [Tooltip("Information for all colliders this ability will use")]
        [SerializeField]
        protected HitColliderInfo[] ColliderData;
        [Tooltip("Any additional stats this ability needs to keep track of")]
        [SerializeField]
        protected Stat[] _customStats;

        [Tooltip("The type of animation that will play. If custom is selected the animation in the custom slot will be used")]
        public AnimationType animationType;

        [Tooltip("A unique animation that will be used for the attack instead of one of the defaults")]
        [SerializeField]
        private AnimationClip _customAnimation;
        /// <summary>
        /// Gets the custom animation attached this data
        /// </summary>
        /// <param name="customAnimation">The reference to initialize</param>
        /// <returns></returns>
        public bool GetCustomAnimation(out AnimationClip customAnimation)
        {
            customAnimation = null;

            if (animationType != AnimationType.CUSTOM || !_customAnimation)
                return false;

            customAnimation = _customAnimation;
            return true;
        }

        /// <summary>
        /// Searches for a stat value that matches the name and returns it if found
        /// </summary>
        /// <param name="name">The name of the stat value</param>
        /// <returns>The value of the stat. Return NaN if the stat couldn't be found</returns>
        public float GetCustomStatValue(string name)
        {
            foreach (Stat stat in _customStats)
            {
                if (stat.name == name)
                    return stat.value;
            }

            Debug.LogError("Couldn't find stat. Either the stat doesn't exist or the name is mispelled. Attempted stat name was " + name);
            return float.NaN;
        }

        public HitColliderInfo GetColliderInfo(int index)
        {
            if (index < 0 || index >= ColliderData.Length)
            {
                Debug.LogWarning("GetColliderInfo() was called with an invalid index passed as a parameter");
                return new HitColliderInfo();
            }

            return ColliderData[index];
        }

        public int ColliderInfoCount
        {
            get
            {
                return ColliderData.Length;
            }
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(AbilityData))]
    [CanEditMultipleObjects]
    public class AbilityDataEditor : Editor
    {
        private SerializedProperty _ColliderInfo;

        private void OnEnable()
        {
            _ColliderInfo = serializedObject.FindProperty("ColliderInfo");
        }

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();

            base.CreateInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
