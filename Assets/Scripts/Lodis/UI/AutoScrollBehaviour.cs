using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class AutoScrollBehaviour : MonoBehaviour
    {
        private ScrollRect _scroll;
        [SerializeField]
        private RectTransform _view;
        [SerializeField]
        private float _scrollAmount;
        [SerializeField]
        private EventSystem _eventSystem;
        private float _scrollAmountNormalized;
        private GameObject _currentItem;
        private float _currentItemPos;

        // Start is called before the first frame update
        void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            _scrollAmountNormalized = _scrollAmount / _view.rect.height;
        }

        // Update is called once per frame
        void Update()
        {
            Canvas.ForceUpdateCanvases();
            if (_currentItem != _eventSystem.currentSelectedGameObject)
            {
                _currentItem = _eventSystem.currentSelectedGameObject;
            }
            _currentItemPos = _currentItem.transform.position.y;

            if (_currentItemPos >= _view.transform.position.y +_view.rect.height / 2)
                _scroll.verticalNormalizedPosition += _scrollAmountNormalized;
            else if (_currentItemPos <= _view.transform.position.y - _view.rect.height / 2)
                _scroll.verticalNormalizedPosition -= _scrollAmountNormalized;
        }
    }
}