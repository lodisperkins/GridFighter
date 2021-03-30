using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;

namespace Lodis.Movement
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridMovementBehaviour))]
    public class KnockbackBehaviour : MonoBehaviour, IDamagable
    {
        private Rigidbody _rigidbody;
        [Tooltip("How heavy the game object is. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField]
        private float _weight;
        [Tooltip("The total amount of damage taken. The higher this value is, the farther it travels when knocked back.")]
        [SerializeField]
        private float _damageAccumulated;
        [SerializeField]
        private VariableScripts.FloatVariable _maxMagnitude;
        private GridMovementBehaviour _movementBehaviour;
        private Condition _onRigidbodyInactive;
        private Vector2 _newPanelPosition;
        private float _currentKnockBackScale;

        /// <summary>
        /// Returns if the object is in knockback
        /// </summary>
        public bool InHitStun
        {
            get
            {
                return !RigidbodyInactive(null);
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _onRigidbodyInactive = RigidbodyInactive;
            _movementBehaviour.AddOnMoveAction(() => { _rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveAction(UpdatePanelPosition);
        }

        private bool RigidbodyInactive(object[] args)
        {
            return _rigidbody.IsSleeping();
        }

        /// <summary>
        /// Updates the panel position to the position the object will land.
        /// New position found after calculating the knockback force
        /// </summary>
        private void UpdatePanelPosition()
        {
            _movementBehaviour.MoveToPanel(_newPanelPosition, false, GridScripts.GridAlignment.ANY);
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels backwards
        /// </summary>
        /// <param name="knockbackScale">How many panels backwards will the object move assuming its weight is 0</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public Vector3 CalculateKnockbackForce(float knockbackScale, float hitAngle)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Grid.PanelSpacing;
            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = (knockbackScale + (knockbackScale * (_damageAccumulated /100))) - _weight;

            //hitAngle = Mathf.Clamp(hitAngle, 0.6f, 2.6f);

            //If the knockback was too weak return an empty vector
            if (totalKnockback <= 0)
                return new Vector3();

            //Find the new panel's grid position based on the knock back
            _newPanelPosition = _movementBehaviour.Position + (new Vector2(Mathf.Cos(hitAngle), 0)).normalized * totalKnockback;

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * totalKnockback) + (panelSpacing * (totalKnockback - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * Physics.gravity.magnitude;
            float val2 = Mathf.Sin(2 * hitAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
                return new Vector3();

            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Val);

            //Return the knockback force
            return new Vector3(Mathf.Cos(hitAngle), Mathf.Sin(hitAngle)) * magnitude;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.GetComponent<GridMovementBehaviour>() || !InHitStun)
                return;

            IDamagable damageScript = collision.gameObject.GetComponent<IDamagable>();

            if (damageScript == null)
                return;

            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, direction);
            float hitAngle = Mathf.Acos(dotProduct);

            damageScript.TakeDamage(_currentKnockBackScale * 2, _currentKnockBackScale / 2, hitAngle);
            TakeDamage(_currentKnockBackScale * 2, _currentKnockBackScale / 2, hitAngle);
        }

        /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// Index 0: The damage this object will take
        /// Index 1: How many panels backwards will the object move assuming its weight is 0
        /// Index 2: The angle to launch the object
        /// </summary>
        public float TakeDamage(params object[] args)
        {

            if (!_movementBehaviour || !_rigidbody)
                return 0;

            float damage = (float)args[0];
            float knockbackScale = (float)args[1];
            float hitAngle = (float)args[2];
            _currentKnockBackScale = knockbackScale;

            //Adds damage to the total damage
            _damageAccumulated += damage;

            //Disables object movement on the grid
            _movementBehaviour.DisableMovement(_onRigidbodyInactive);

            //Calculates force and applies it to the rigidbody
            _rigidbody.isKinematic = false;
            _rigidbody.AddForce(CalculateKnockbackForce(knockbackScale, hitAngle), ForceMode.Impulse);

            return damage;
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

            if (GUILayout.Button("Test Attack"))
            {
                _owner.TakeDamage(_damage, _knockbackScale, _hitAngle);
            }
        }
    }

#endif
}


