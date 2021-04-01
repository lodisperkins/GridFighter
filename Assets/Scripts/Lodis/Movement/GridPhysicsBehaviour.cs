using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class GridPhysicsBehaviour : MonoBehaviour
    {
        private float _gravity = 9.81f;
        private Vector3 _velocity;
        private Vector3 _force;
        private float _maxForceMagnitude;
        private float _maxVelocityMagnitude;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}


