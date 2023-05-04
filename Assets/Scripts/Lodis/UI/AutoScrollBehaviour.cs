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
        private float _distanceToScroll;
        [SerializeField]
        private EventSystem _eventSystem;
        [SerializeField]
        private RectTransform _content;
        private float _scrollAmountNormalized;
        private RectTransform _currentItem;
        private float _currentItemPos;

        public EventSystem EventSystem { get => _eventSystem; set => _eventSystem = value; }

        // Start is called before the first frame update
        void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            _currentItemPos = _content.anchoredPosition.x;
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentItem != EventSystem.currentSelectedGameObject)
            {
                _currentItem = EventSystem.currentSelectedGameObject.GetComponent<RectTransform>();
            }

            Vector2 position = (Vector2)_scroll.transform.InverseTransformPoint(_content.position) - (Vector2)_scroll.transform.InverseTransformPoint(_currentItem.position);

            float distance = Vector2.Distance(position, _content.anchoredPosition);

            if (distance >= _distanceToScroll)
                _content.anchoredPosition = position;
        }
    }
}