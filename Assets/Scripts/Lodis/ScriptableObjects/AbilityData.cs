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



    [CreateAssetMenu(menuName = "AbilityData")]
    public class AbilityData : ScriptableObject
    {
        [SerializeField]
        private AnimationClip _customAnimation;
        public string name = "Unassigned";
        //The type describes the strength and input value for the ability
        public BasicAbilityType abilityType;
        public DamageType damageType = DamageType.DEFAULT;
        //How long the ability should be active for
        public float timeActive = 0;
        //How long does the object that used the ability need before being able to recover
        public float recoverTime = 0;
        //How long does the object that used the ability must wait before the ability activates
        public float startUpTime = 0;
        //If true, this ability can be canceled into others in the start up phase
        public bool canCancelStartUp = false;
        //If true, this ability can be canceled into others in the active phase
        public bool canCancelActive = false;
        //If true, this ability can be canceled into others in the recover phase
        public bool canCancelRecover = false;
        public bool useAbilityTimingForAnimation;
        public AnimationType animationType;

        public bool GetCustomAnimation(out AnimationClip customAnimation)
        {
            customAnimation = null;

            if (animationType != AnimationType.CUSTOM || !_customAnimation)
                return false;

            customAnimation = _customAnimation;
            return true;
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
            EditorGUILayout.PropertyField(_name,
                new GUIContent("Name", "The name of the ability"));

            EditorGUILayout.PropertyField(_abilityType,
                new GUIContent("Ability Type", "The type describes the strength and input value for the ability"));

            EditorGUILayout.PropertyField(_damageType,
                new GUIContent("Damage Type", "The type of damage this ability deals to objects if any"));

            EditorGUILayout.PropertyField(_timeActive,
                new GUIContent("Time Active", "How long the ability should be active for"));

            EditorGUILayout.PropertyField(_recoverTime,
                new GUIContent("Recover Time", "How long does the object that used the ability need before being able to recover"));

            EditorGUILayout.PropertyField(_startUpTime,
                new GUIContent("Start Up Time", "How long should the object that used the ability wait before the ability activates"));

            EditorGUILayout.PropertyField(_canCancelStartUp, new GUIContent("Can Cancel Start Up Phase",
                "If true, this ability can be canceled into others"));

            EditorGUILayout.PropertyField(_canCancelActive, new GUIContent("Can Cancel Active Phase",
                "If true, this ability can be canceled into others"));

            EditorGUILayout.PropertyField(_canCancelRecover, new GUIContent("Can Cancel Recover Phase",
                "If true, this ability can be canceled into others"));

            EditorGUILayout.PropertyField(_useAbilityTiming, new GUIContent("Use Ability Timing For Animation",
                "If true, uses the animation events attached to the clip to speed up or slow down the animation" +
                " based on each ability phase duration."));

            EditorGUILayout.PropertyField(_animationType, new GUIContent("Animation Type",
                "The type of animation that will play when the ability is used"));

            base.CreateInspectorGUI();

            if (_animationType.enumValueIndex == (int)AnimationType.CUSTOM)
            {
                EditorGUILayout.PropertyField(_customAnimation,
                    new GUIContent("Custom Animation", "Reference to the animation clip that will play when the ability is used"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
