using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Utility
{
    public class SizeLerpBehaviour : MonoBehaviour
    {
        private Vector3 _startScale;
        [SerializeField]
        private Vector3 _minScale;
        [SerializeField]
        private Vector3 _maxScale;
        [SerializeField]
        private bool _loopOnEnabled;
        [SerializeField]
        private bool _loop;
        [SerializeField]
        private float _scaleSpeed;
        private bool _started;
        [SerializeField]
        private UnityEvent _onEnable;
        public UnityAction onResized;

        // Start is called before the first frame update
        void Start()
        {
            _startScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_loopOnEnabled)
                GrowAndShrink();

            _onEnable?.Invoke();
        }

        private IEnumerator ShrinkRoutine()
        {
            _started = true;
            float lerpVal = 0;

            while (transform.localScale.magnitude > _minScale.magnitude)
            {
                transform.localScale = Vector3.Lerp(_startScale, _minScale, lerpVal);
                lerpVal += Time.deltaTime * _scaleSpeed;
                yield return new WaitForEndOfFrame();
            }

            _started = false;
            onResized?.Invoke();

        }

        private IEnumerator GrowRoutine()
        {
            _started = true;
            float lerpVal = 0;

            while (transform.localScale.magnitude < _maxScale.magnitude)
            {
                transform.localScale = Vector3.Lerp(_startScale, _maxScale, lerpVal);
                lerpVal += Time.deltaTime * _scaleSpeed;
                yield return new WaitForEndOfFrame();
            }

            _started = false;
            onResized?.Invoke();
            if (_loop)
                GrowAndShrink();
        }

        private IEnumerator GrowAndShrinkRoutine()
        {
            _started = true;
            float lerpVal = 0;

            while (transform.localScale.magnitude < _maxScale.magnitude)
            {
                transform.localScale = Vector3.Lerp(_startScale, _maxScale, lerpVal);
                lerpVal += Time.deltaTime * _scaleSpeed;
                yield return new WaitForEndOfFrame();
            }

            lerpVal = 0;
            Vector3 tempStartScale = transform.localScale;

            while (transform.localScale.magnitude > _minScale.magnitude)
            {
                transform.localScale = Vector3.Lerp(tempStartScale, _minScale, lerpVal);
                lerpVal += Time.deltaTime * _scaleSpeed;
                yield return new WaitForEndOfFrame();
            }

            _started = false;
            onResized?.Invoke();

            if (_loop)
                Grow();
        }

        public void Shrink()
        {
            if (!_started)
                StartCoroutine(ShrinkRoutine());
        }

        public void Grow()
        {
            if (!_started)
                StartCoroutine(GrowRoutine());
        }

        public void GrowAndShrink()
        {
            if (!_started)
                StartCoroutine(GrowAndShrinkRoutine());
        }
    }
}
