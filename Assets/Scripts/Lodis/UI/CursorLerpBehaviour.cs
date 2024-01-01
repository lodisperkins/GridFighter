using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Lodis.Gameplay;
using UnityEngine.Events;

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
        private UnityEvent _onSelectionUpdated = new UnityEvent();

        public EventSystem EventSystem { get => _eventSystem; set => _eventSystem = value; }

        public void AddOnSelectionUpdatedAction(UnityAction action)
        {
            _onSelectionUpdated.AddListener(action);
        }

        public void LerpToTransform(Transform rect)
        {
            _moveTween.Kill();
            _moveTween = _cursor.DOMove(rect.transform.position, _lerpDuration).SetUpdate(true);
        }

        void Update()
        {
            if (!EventSystem || !_cursor.gameObject.activeInHierarchy)
                return;

            if (_lastSelectedGameObject != EventSystem.currentSelectedGameObject)
                _onSelectionUpdated?.Invoke();

            _lastSelectedGameObject = EventSystem.currentSelectedGameObject;

            if (_lastSelectedGameObject)
                LerpToTransform(_lastSelectedGameObject.transform);
        }
    }
}