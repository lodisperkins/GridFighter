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
        [Tooltip("The maximum amount of bounciness objects bouncing off of this panel will have")]
        [SerializeField]
        private float _maxBounceForce = 3.0f;
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
            else if (knockbackScript.IsInvincible || knockbackScript.InFreeFall)
                return;

            if (defenseScript)
            {
                if (defenseScript.IsBraced)
                {
                    knockbackScript.SetInvincibilityByTimer(defenseScript.RecoverInvincibilityLength);
                    Vector3 normal = other.transform.position - transform.position;
                    defenseScript.onFallBroken?.Invoke(normal.normalized);
                    Debug.Log("teched floor");
                    return;
                }
            }

            //Don't add a force if the object is traveling at a low speed
            if (knockbackScript.LastVelocity.magnitude <= 0.1f || knockbackScript.Bounciness <= 0 || !knockbackScript.PanelBounceEnabled)
                return;

            float upMagnitude = 0;
            upMagnitude = knockbackScript.LastVelocity.magnitude;

            if (_bounceDampening > knockbackScript.Bounciness )
                //Calculate and apply friction force
                upMagnitude /= _bounceDampening - knockbackScript.Bounciness;

            knockbackScript.ApplyImpulseForce(Vector3.up * upMagnitude);
        }

        private void OnTriggerStay(Collider other)
        {
            //Get knock back script to apply force
            Movement.KnockbackBehaviour knockbackScript = other.transform.root.GetComponent<Movement.KnockbackBehaviour>();

            //Return if the object doesn't have one or is invincible
            if (!knockbackScript)
                return;
            else if (knockbackScript.IsInvincible || knockbackScript.InFreeFall)
                return;

            //Don't add a force if the object is traveling at a low speed
            if (knockbackScript.LastVelocity.magnitude <= 0.5f)
                return;

            //Calculate and apply friction force
            Vector3 frictionForce = new Vector3(knockbackScript.Mass * knockbackScript.LastVelocity.x, 0, 0).normalized * _friction;
            knockbackScript.ApplyForce(-frictionForce);
        }
    }
}


