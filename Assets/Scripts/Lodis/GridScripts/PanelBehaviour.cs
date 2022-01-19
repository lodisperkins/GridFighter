using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class PanelBehaviour : MonoBehaviour
    {
        private Vector2 _position;
        [SerializeField]
        private bool _occupied;
        private GridAlignment _alignment;
        [Tooltip("The material to give this panel if it is not aligned with either side of the grid.")]
        [SerializeField]
        private Material _neutralMat;
        [Tooltip("The material to give this panel if it is aligned with the left side of the grid.")]
        [SerializeField]
        private Material _leftSideMat;
        [Tooltip("The material to give this panel if it is aligned with the right side of the grid.")]
        [SerializeField]
        private Material _rightSideMat;
        [Tooltip("How quick objects bouncing on this panel stop boucnign")]
        [SerializeField]
        private float _bounceDampening = 3.0f;
        [Tooltip("The amount of resistance this panel has against objects sliding over it")]
        [SerializeField]
        private float _friction = 3.0f;
        private MeshRenderer _mesh;
        private void Awake()
        {
            _mesh = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// The side of the grid this panel this panel belongs to
        /// </summary>
        public GridAlignment Alignment
        {
            get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;
                UpdateMaterial();
            }
        }

        /// <summary>
        /// Changes the material of the panel based on the side of the
        /// grid that its on.
        /// </summary>
        private void UpdateMaterial()
        {
            if (_mesh == null)
                _mesh = GetComponent<MeshRenderer>();

            switch (_alignment)
            {
                case GridAlignment.LEFT:
                    _mesh.material = _leftSideMat;
                    break;
                case GridAlignment.RIGHT:
                    _mesh.material = _rightSideMat;
                    break;
                case GridAlignment.ANY:
                    _mesh.material = _neutralMat;
                    break;
            }
        }

        /// <summary>
        /// The position of this panel on the grid.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// Returns if there is anything preventing an object from moving on to this panel.
        /// </summary>
        public bool Occupied
        {
            get
            {
                return _occupied;
            }
            set
            {
                _occupied = value;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Movement.KnockbackBehaviour knockbackScript = other.transform.root.GetComponent<Movement.KnockbackBehaviour>();
            Gameplay.CharacterDefenseBehaviour defenseScript = other.transform.root.GetComponent<Gameplay.CharacterDefenseBehaviour>();

            if (!knockbackScript)
                return;
            else if (knockbackScript.IsInvincible || knockbackScript.Landing)
                return;

            //Don't add a force if the object is traveling at a low speed
            if (knockbackScript.Physics.LastVelocity.magnitude <= 0.1f || knockbackScript.Physics.Bounciness <= 0 || !knockbackScript.Physics.PanelBounceEnabled)
                return;

            float upMagnitude = knockbackScript.Physics.LastVelocity.magnitude * knockbackScript.Physics.Bounciness;

            //Calculate and apply friction force
            upMagnitude /= _bounceDampening;

            knockbackScript.Physics.ApplyImpulseForce(Vector3.up * upMagnitude);
            knockbackScript.CancelHitStun();
        }

        private void OnTriggerStay(Collider other)
        {
            //Get knock back script to apply force
            Movement.GridPhysicsBehaviour physics = other.transform.root.GetComponent<Movement.GridPhysicsBehaviour>();

            //Return if the object doesn't have one or is invincible
            if (!physics)
                return;

            //Don't add a force if the object is traveling at a low speed
            float dotProduct = Vector3.Dot(physics.LastVelocity, Vector3.up);
            if (dotProduct >= 0 || dotProduct == -1)
                return;

            //Calculate and apply friction force
            Vector3 frictionForce = new Vector3(physics.Mass * physics.LastVelocity.x, 0, 0).normalized * _friction;
            physics.ApplyForce(-frictionForce);
        }
    }
}


