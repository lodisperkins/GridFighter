using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class GridPhysicsBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _gravity = 9.81f;
        private Vector3 _velocity;
        private Vector3 _force;
        [SerializeField]
        private float _maxForceMagnitude;
        [SerializeField]
        private float _maxVelocityMagnitude;
        private Rigidbody _rigidbody;
        private bool _isGrounded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
        }

        public bool IsOnGrid()
        {
            LayerMask layerMask = LayerMask.GetMask("Panel");

            if (Physics.Raycast(transform.position, -transform.up, 10, layerMask))
                return true;
    
            return false;
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsOnGrid())
                _force += new Vector3(0, -_gravity, 0);
            else
                _force = new Vector3(_force.x, 0, _force.y);

            _force = Vector3.ClampMagnitude(_force, _maxForceMagnitude);

            _velocity += _force;

            _velocity = Vector3.ClampMagnitude(_velocity, _maxVelocityMagnitude);

            transform.Translate(_velocity * Time.deltaTime);
        }
    }
}


