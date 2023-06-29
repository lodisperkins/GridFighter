using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using UnityEngine.Serialization;

namespace Lodis.Utility
{
    public class SizeLerpBehaviour : MonoBehaviour
    {
        private Vector3 _startScale;
        [SerializeField]
        private Vector3 _targetScale;
        [SerializeField]
        [FormerlySerializedAs("_loopOnEnabled")]
        private bool _resizeOnEnabled;
        [Tooltip("Makes the object constantly resize. Lerps size to target size and lerps back to original when the target size is reached.")]
        [SerializeField]
        private bool _loopYoYo;

        [Tooltip("Makes the object constantly resize. Lerps size to target size and snaps back to original when the target size is reached.")]
        [SerializeField]
        private bool _loopReset;
        [SerializeField]
        private float _scaleDuration;
        [SerializeField]
        private UnityEvent _onEnable;
        [SerializeField]
        private UnityEvent _onResizeComplete;

        // Start is called before the first frame update
        void Start()
        {
            _startScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_resizeOnEnabled)
                StartResize();

            _onEnable?.Invoke();
        }

        private void OnDisable()
        {
            transform.DOKill();
            ResetScale();
        }

        public void StartResize()
        {
            if (_loopYoYo)
                transform.DOScale(_targetScale, _scaleDuration).SetLoops(-1, LoopType.Yoyo).onComplete += () => _onResizeComplete?.Invoke();
            else if (_loopReset)
                transform.DOScale(_targetScale, _scaleDuration).SetLoops(-1, LoopType.Restart).onComplete += () => _onResizeComplete?.Invoke();
            else
                transform.DOScale(_targetScale, _scaleDuration).onComplete += () => _onResizeComplete?.Invoke();
        }

        public void ResetScale()
        {
            transform.localScale = _startScale;
        }
    }
}
