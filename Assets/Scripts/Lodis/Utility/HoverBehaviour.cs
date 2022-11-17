using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Lodis.Utility
{
    public class HoverBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _speed;
        [SerializeField]
        private Vector3 _positionOffset;
        private Vector3 _startPosition;
        private Vector3 _target;
        private float _time;

        // Start is called before the first frame update
        void Start()
        {
            _startPosition = transform.localPosition;
            _target = transform.localPosition + _positionOffset;
        }

        void Update()
        {
            transform.localPosition = Vector3.LerpUnclamped(_startPosition, _target, Mathf.Cos(_time += Time.deltaTime) * _speed);
        }

    }
}