using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;

namespace Lodis.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridMovementBehaviour))]
    public class KnockbackBehaviour : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        [SerializeField]
        private float _weight;
        [SerializeField]
        private float _damageAccumulated;
        private GridMovementBehaviour _movementBehaviour;

        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        public Vector3 CalculateKnockbackForce(float knockbackScale, float hitAngle)
        {
            float panelSize = BlackBoardBehaviour.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Grid.PanelSpacing;

            float displacement = (panelSize * knockbackScale) + (panelSpacing * (knockbackScale - 1));
            float magnitude = Mathf.Sqrt((displacement * Physics.gravity.magnitude) / Mathf.Sin(2 * hitAngle));

            return new Vector3(Mathf.Cos(hitAngle), Mathf.Sin(hitAngle)) * magnitude;
        }

        public void TakeDamage(float damage, float knockbackScale, float hitAngle)
        {
            _damageAccumulated += damage;
            _rigidbody.AddForce(CalculateKnockbackForce(knockbackScale, hitAngle), ForceMode.Impulse);
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(KnockbackBehaviour))]
    class KnockbackEditor : Editor
    {
        private KnockbackBehaviour _owner;
        private float _damage;
        private float _knockbackScale;
        private float _hitAngle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _owner = (KnockbackBehaviour)target;

            _damage = EditorGUILayout.FloatField("Damage", _damage);
            _knockbackScale = EditorGUILayout.FloatField("Knockback Scale", _knockbackScale);
            _hitAngle = EditorGUILayout.FloatField("Hit Angle", _hitAngle);

            if (GUILayout.Button("Apply Knockback"))
            {
                _owner.TakeDamage(_damage, _knockbackScale, _hitAngle);
            }
        }
    }

#endif
}


