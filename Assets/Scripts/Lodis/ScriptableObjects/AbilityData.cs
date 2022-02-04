﻿using Lodis.Gameplay;
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
        [Tooltip("The type of damage this attack will deal to other objects")]
        public DamageType damageType = DamageType.DEFAULT;
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
        [Tooltip("If false, the user of this ability can't move while it's active")]
        public bool CanInputMovementWhileActive = true;
        [Tooltip("If true, this ability will be canceled when the user is hit")]
        public bool cancelOnHit = false;
        [Tooltip("If true, this ability will be canceled when the user is in knockback")]
        public bool cancelOnKnockback = true;
        [Tooltip("If false, this ability's hit colliders can collide with others. Meaning that this ability's colliders can be destroyed if the other collider has higher priority.")]
        public bool IgnoreColliders = true;
        [Tooltip("The priority level of the collider. Colliders with higher levels destroys colliders with lower levels.")]
        public float ColliderPriority = 0.0f;
        [Tooltip("If true, the animation will change speed according to the start, active, and recover times")]
        public bool useAbilityTimingForAnimation;
        [Tooltip("If true, the animation will only play when specified in the ability script")]
        public bool playAnimationManually = false;
        [Tooltip("The prefab that holds the visual this ability will be using.")]
        public GameObject visualPrefab;

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
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(AbilityData))]
    [CanEditMultipleObjects]
    public class AbilityDataEditor : Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _customAnimation;
        private SerializedProperty _abilityType;
        private SerializedProperty _damageType;
        private SerializedProperty _timeActive;
        private SerializedProperty _recoverTime;
        private SerializedProperty _startUpTime;
        private SerializedProperty _canCancelStartUp;
        private SerializedProperty _canCancelActive;
        private SerializedProperty _canCancelRecover;
        private SerializedProperty _animationType;
        private SerializedProperty _useAbilityTiming;

        private void OnEnable()
        {
            _name = serializedObject.FindProperty("name");
            _customAnimation = serializedObject.FindProperty("_customAnimation");
            _abilityType = serializedObject.FindProperty("abilityType");
            _damageType = serializedObject.FindProperty("damageType");
            _timeActive = serializedObject.FindProperty("timeActive");
            _recoverTime = serializedObject.FindProperty("recoverTime");
            _startUpTime = serializedObject.FindProperty("startUpTime");
            _canCancelStartUp = serializedObject.FindProperty("canCancelStartUp");
            _canCancelActive = serializedObject.FindProperty("canCancelActive");
            _canCancelRecover = serializedObject.FindProperty("canCancelRecover");
            _animationType = serializedObject.FindProperty("animationType");
            _useAbilityTiming = serializedObject.FindProperty("useAbilityTimingForAnimation");
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
