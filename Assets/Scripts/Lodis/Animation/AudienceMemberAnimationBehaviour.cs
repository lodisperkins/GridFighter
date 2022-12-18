using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis
{
    public class AudienceMemberAnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If true, the random scale will always be the same in all axis.")]
        private bool _uniformScale;
        [SerializeField]
        [Tooltip("The smallest scale this can be in any axis.")]
        private float _minScale;
        [SerializeField]
        [Tooltip("The largest scale this can be in any axis.")]
        private float _maxScale;
        [SerializeField]
        [Tooltip("The slowest the audience animation is allowed to play.")]
        private float _minSpeed;
        [SerializeField]
        [Tooltip("The fastest the audience animation is allowed to play.")]
        private float _maxSpeed;
        private Animator _animator;
        
        // Start is called before the first frame update
        void Awake()
        {
            _animator = GetComponent<Animator>();

            //Assign random values.

            _animator.speed = Random.Range(_minSpeed, _maxSpeed);

            //Scale on all axis evenly if it's been enabled.
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
