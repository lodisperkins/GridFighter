using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Lodis.Gameplay;
using UnityEngine.Events;

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

        // Start is called before the first frame update
        void Awake()
        {
            _attachedCamera = GetComponent<Camera>();

            _onLerpComplete.AddListener(() => SetCameraEnabled(false));
        }

        private void OnEnable()
        {
            var camData = Camera.main.GetUniversalAdditionalCameraData();
            camData.cameraStack.Add(_attachedCamera);
        }

        private void OnDisable()
        {
            var camData = Camera.main?.GetUniversalAdditionalCameraData();

            if (camData == null)
                return;

            camData.cameraStack.Remove(_attachedCamera);
        }

        public void AddOnLerpCompleteAction(UnityAction action)
        {
            _onLerpComplete.AddListener(action);
        }

        public void LerpCamera()
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;
            transform.DOMove(_lerpEnd.position, LerpDuration).SetUpdate(true).SetEase(_lerpCurve)
                .onComplete += () => _onLerpComplete?.Invoke();
        }

        public void LerpCamera(float duration)
        {
            SetCameraEnabled(true);
            transform.DOKill();

            transform.position = _lerpStart.position;
            transform.DOMove(_lerpEnd.position, duration).SetUpdate(true).SetEase(_lerpCurve)
                .onComplete += () => _onLerpComplete?.Invoke();
        }

        public void StopLerpCamera()
        {
            transform.DOKill();
        }

        public void SetCameraCullingMask(string name)
        {
            _attachedCamera.cullingMask = LayerMask.NameToLayer(name);
        }

        public void SetCameraEnabled(bool enabled)
        {
            _attachedCamera.enabled = enabled;
        }
    }
}