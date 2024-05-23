using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Lodis.Gameplay;
using UnityEngine.Events;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace Lodis.FX
{
    public class CharacterCameraBehaviour : MonoBehaviour
    {
        private Camera _attachedCamera;
        [SerializeField]
        private float _lerpDuration;
        [SerializeField]
        private Transform _lerpStart;
        [SerializeField]
        private Transform _lerpEnd;
        [SerializeField]
        private AnimationCurve _lerpCurve;
        private UnityEvent _onLerpComplete = new UnityEvent();

        public float LerpDuration { get => _lerpDuration; private set => _lerpDuration = value; }

        public int CullingMask { get => _attachedCamera.cullingMask; set => _attachedCamera.cullingMask = value; }
        // Start is called before the first frame update
        void Awake()
        {
            _attachedCamera = GetComponent<Camera>();

            _onLerpComplete.AddListener(() => SetCameraEnabled(false));
        }

        private void OnEnable()
        {
            var camData = Camera.main.GetUniversalAdditionalCameraData();
            camData?.cameraStack.Add(_attachedCamera);
        }

        private void OnDisable()
        {
            var camData = Camera.main?.GetUniversalAdditionalCameraData();

            if (camData == null)
                return;

            camData?.cameraStack.Remove(_attachedCamera);
        }

        public void FlipStartEndTransforms()
        {
            Vector3 temp = _lerpStart.position;
            _lerpStart.position = _lerpEnd.position;
            _lerpEnd.position = temp;
        }

        public void AddOnLerpCompleteAction(UnityAction action)
        {
            _onLerpComplete.AddListener(action);
        }

        public void LerpCamera(bool flipped = false)
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;

            var tween = transform.DOMove(_lerpEnd.position, _lerpDuration).SetUpdate(true).SetEase(_lerpCurve);
            tween.onComplete += () => _onLerpComplete?.Invoke();
            if (flipped)
                tween.Flip();
        }

        public void PunchCamera(Vector3 punch, float duration)
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;

            var tween = transform.DOPunchPosition(punch, duration,0,0).SetUpdate(true);
            tween.onComplete += () => _onLerpComplete?.Invoke();
        }

        public void PunchCamera(Vector3 punch, float duration, AnimationCurve curve)
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;

            var tween = transform.DOPunchPosition(punch, duration,0,0).SetUpdate(true).SetEase(curve);
            tween.onComplete += () => _onLerpComplete?.Invoke();
        }

        public void LerpCamera(float duration,AnimationCurve curve)
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;


            var tween = transform.DOMove(_lerpEnd.position, duration).SetUpdate(true).SetEase(curve);
            tween.onComplete += () => _onLerpComplete?.Invoke();
        }

        public void StopLerpCamera()
        {
            transform.DOKill();
        }

        public void SetCameraEnabled(bool enabled)
        {
            _attachedCamera.enabled = enabled;
        }
    }
}