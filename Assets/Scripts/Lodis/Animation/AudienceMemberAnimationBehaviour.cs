using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis
{
    public class AudienceMemberAnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        private bool _uniformScale;
        [SerializeField]
        private float _minScale;
        [SerializeField]
        private float _maxScale;
        [SerializeField]
        private float _minSpeed;
        [SerializeField]
        private float _maxSpeed;
        private Animator _animator;
        
        // Start is called before the first frame update
        void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.speed = Random.Range(_minSpeed, _maxSpeed);

            if (_uniformScale)
            {
                transform.localScale = new Vector3(Random.Range(_minScale, _maxScale), Random.Range(_minScale, _maxScale), Random.Range(_minScale, _maxScale));
                return;
            }

            float scale = Random.Range(_minScale, _maxScale);

            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
