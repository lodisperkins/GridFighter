using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class PanelBehaviour : MonoBehaviour
    {
        private Vector2 _position;
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
        [SerializeField]
        private float _maxBounceForce = 3.0f;
        [SerializeField]
        private float _bounceDampening = 3.0f;
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
            Movement.KnockbackBehaviour knockbackScript = other.GetComponent<Movement.KnockbackBehaviour>();

            if (!knockbackScript)
                return;

            if (Vector3.Dot(Vector3.down, knockbackScript.LastVelocity) <= 0 || !knockbackScript.InHitStun)
                return;

            float upMagnitude = Mathf.Clamp(knockbackScript.LastVelocity.magnitude / _bounceDampening, 0, _maxBounceForce);


            knockbackScript.StopVelocity();
            knockbackScript.ApplyImpulseForce(Vector3.up * upMagnitude);

            //Vector3 perpVelocity = new Vector3(-knockbackScript.LastVelocity.y, knockbackScript.LastVelocity.x, 0);
            //float hitAngle = Vector3.Angle(perpVelocity, Vector3.right) * Mathf.Deg2Rad;

            //float velocityMagnitude = knockbackScript.LastVelocity.magnitude;
            //float knockbackScale = knockbackScript.CurrentKnockBackScale * (velocityMagnitude / knockbackScript.LaunchVelocity.magnitude);

            ////Apply ricochet force and damage
            //knockbackScript.TakeDamage(0.0f, knockbackScale, hitAngle);
        }
    }
}


