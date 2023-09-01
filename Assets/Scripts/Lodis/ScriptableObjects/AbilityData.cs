using System;
using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.Video;
using Lodis.Utility;

namespace Lodis.ScriptableObjects
{
    public enum AnimationType
    {
        CAST,
        SUMMON,
        MELEE,
        CUSTOM
    }


    [CreateAssetMenu(menuName = "AbilityData/Default")]
    public class AbilityData : ScriptableObject
    {
        
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

        public string abilityName = "Unassigned";
        [TextArea]
        public string abilityDescription = "None";
        public VideoClip exampleClip;
        [Tooltip("The type describes the strength and input value for the ability")]
        public AbilityType AbilityType;

        [Header("Usage Timing")]
        [Tooltip("How long the ability should be active for")]
        public float timeActive = 0;
        [Tooltip("How long does the object that used the ability needs before returning to idle")]
        public float recoverTime = 0;
        [Tooltip("How long does the object that used the ability must wait before the ability activates")]
        public float startUpTime = 0;

        [Header("Activation")]
        [Tooltip("The amount of time this ability must be in an active slot before it can be used. Can be ignored if this ability is a normal type.")]
        public float chargeTime = 0;
        [Tooltip("The amount of time this ability can be used before it is removed from the active slot. Can be ignored if this ability is a normal type.")]
        public int maxActivationAmount = 1;
        [Tooltip("The amount energy it costs to activate this ability.")]
        public float EnergyCost = 0;

        [Header("Cancellation Rules")]
        public CancellationRule[] CancellationRules;
        [Tooltip("If true, this ability will be canceled when the user is takes damage in any ability phase.")]
        public bool CancelAllOnHit;
        [Tooltip("If true, this ability will be canceled when the user is flinching in any ability phase.")]
        public bool CancelAllOnFlinch = true;
        [Tooltip("If true, this ability will be canceled when the user is in knockback in any phase.")]
        public bool CancelAllOnKnockback = true;

        [Header("Movement Rules")]
        [Tooltip("If false, the user of this ability can't move while it's winding up")]
        public bool CanInputMovementDuringStartUp;
        [Tooltip("If false, the user of this ability can't move while it's active")]
        public bool CanInputMovementWhileActive;
        [Tooltip("If false, the user of this ability can't move while their recovering")]
        public bool CanInputMovementWhileRecovering;

        [Header("Sound and Appearance")]
        [Tooltip("If true, the animation will change speed according to the start, active, and recover times")]
        public bool useAbilityTimingForAnimation;
        [Tooltip("If true, the animation will only play when specified in the ability script")]
        public bool playAnimationManually = false;
        [Tooltip("The prefab that holds the visual this ability will be using.")]
        public GameObject visualPrefab;
        [Tooltip("The data for the accessory that this ability needs to reference.")]
        public AccessoryData Accessory;
        [Tooltip("Additional effects for this ability to play.")]
        public GameObject[] Effects;
        [Tooltip("Additional sounds for this ability to play.")]
        public AudioClip[] Sounds;
        [Tooltip("The icon thatv will display when this ability is on screen")]
        public Sprite DisplayIcon;
        public AudioClip ActivateSound;
        public AudioClip ActiveSound;
        public AudioClip DeactivateSound;

        [Header("Usage Stats")]
        [Tooltip("Information for all colliders this ability will use")]
        [SerializeField]
        protected HitColliderData[] ColliderData;
        [Tooltip("Any additional stats this ability needs to keep track of")]
        [SerializeField]
        protected Stat[] _customStats;

        [Header("Animation Options")]
        [Tooltip("The type of animation that will play. If custom is selected the animation in the custom slot will be used")]
        public AnimationType animationType;

        [Tooltip("A unique animation that will be used for the attack instead of one of the defaults")]
        [SerializeField]
        private AnimationClip _customAnimation;

        [Tooltip("Whether or not this animation should be mirrored when on the right side.")]
        [SerializeField]
        private bool _shouldMirror = true;

        [Tooltip("Any animations that will be used in the ability script.")]
        [SerializeField]
        private AnimationClip[] _additionalAnimations;

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
        /// Gets the animation stored in the additional animations array. Excludes the custom or default animation set.
        /// </summary>
        /// <param name="clip">The reference to initialize</param>
        /// <returns>Whether or not the index was in bounds.</returns>
        public bool GetAdditionalAnimation(int index, out AnimationClip clip)
        {
            clip = null;

            if (index < 0 || _additionalAnimations == null || index >= _additionalAnimations.Length)
                return false;

            clip = _additionalAnimations[index];

            return true;
        }

        /// <summary>
        /// Searches for a stat value that matches the name and returns it if found
        /// </summary>
        /// <param name="statName">The name of the stat value</param>
        /// <returns>The value of the stat. Return NaN if the stat couldn't be found</returns>
        public float GetCustomStatValue(string statName)
        {
            foreach (Stat stat in _customStats)
            {
                if (stat.name == statName)
                    return stat.value;
            }

            throw new Exception(
                "Couldn't find stat. Either the stat doesn't exist or the name is misspelled. Attempted stat name was " +
                statName);
        }

        public bool HasCustomStatValue(string statName)
        {
            return _customStats.Contains(statName);
        }

        public HitColliderData GetColliderInfo(int index)
        {
            if (index < 0 || index >= ColliderData.Length)
            {
                return new HitColliderData();
            }
            ColliderData[index].AbilityType = AbilityType;
            return ColliderData[index];
        }

        public int ColliderInfoCount
        {
            get
            {
                return ColliderData.Length;
            }
        }

        public bool ShouldMirror { get => _shouldMirror; set => _shouldMirror = value; }

        /// <summary>
        /// Gets the first cancellation rule that matches the given phase.
        /// </summary>
        /// <param name="phase">The phase that will be compared to each cancellation rule phase.</param>
        /// <returns>The cancellation rule or null if none matched the phase.</returns>
        public CancellationRule GetCancellationRule(AbilityPhase phase)
        {
            if (CancellationRules == null)
                return null;

            foreach (CancellationRule rule in CancellationRules)
            {
                if (rule.ComparePhase(phase))
                    return rule;
            }

            return null;
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
