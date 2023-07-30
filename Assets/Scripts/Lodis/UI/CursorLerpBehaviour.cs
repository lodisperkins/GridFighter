using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Lodis.Gameplay;

namespace Lodis.UI
{
    public class CursorLerpBehaviour : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _cursor;
        [SerializeField]
        private float _lerpDuration;
        private TweenerCore<Vector3, Vector3, VectorOptions> _moveTween;
        [SerializeField]
        private UnityEngine.EventSystems.EventSystem _eventSystem;
        private GameObject _lastSelectedGameObject;

        public EventSystem EventSystem { get => _eventSystem; set => _eventSystem = value; }

        public void LerpToTransform(Transform rect)
        {
            _moveTween.Kill();
            _moveTween = _cursor.DOMove(rect.transform.position, _lerpDuration).SetUpdate(true);
        }

        void Update()
        {
            if (!EventSystem)
                return;

            _lastSelectedGameObject = EventSystem.currentSelectedGameObject;

            if (_lastSelectedGameObject)
                LerpToTransform(_lastSelectedGameObject.transform);
        }
    }
}