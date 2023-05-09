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
        [Tooltip("If true, this ability can be canceled if a special move is used")]
        public bool CanCancelIntoSpecial = false;
        [Tooltip("If true, this ability can be canceled if a normal move is used")]
        public bool CanCancelIntoNormal = false;
        [Tooltip("If true, this ability can be ")]
        public bool CanOnlyCancelOnOpponentHit = true;
        [Tooltip("If true, this ability will be canceled when the user is hit")]
        public bool cancelOnHit = false;
        [Tooltip("If true, this ability will be canceled when the user is flinching")]
        public bool cancelOnFlinch = false;
        [Tooltip("If true, this ability will be canceled when the user is in knockback")]
        public bool cancelOnKnockback = true;

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
