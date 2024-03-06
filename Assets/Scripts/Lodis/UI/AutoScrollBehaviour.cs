using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class AutoScrollBehaviour : MonoBehaviour
    {
        [SerializeField]
        private bool _scrollHorizontal;
        [Tooltip("The object that has the mask for the viewing area.")]
        [SerializeField]
        private RectTransform _view;
        [Tooltip("How far to move the options when the selected option is not in view.")]
        [SerializeField]
        private float _distanceToScroll;
        [Tooltip("The event system needed in order to know which option is selected.")]
        [SerializeField]
        private EventSystem _eventSystem;
        [Tooltip("The options that the player will scroll through.")]
        [SerializeField]
        private RectTransform _content;
        private RectTransform _currentItem;

        public EventSystem EventSystem { get => _eventSystem; set => _eventSystem = value; }

        private bool CheckInBoundsOfMask()
        {
            Vector2 position = _currentItem.position;

            //Gets position of the view area's corners in the world space.
            Vector3[] corners = new Vector3[4];
            _view.GetWorldCorners(corners);

            Vector2 bottomLeft = corners[0];
            Vector2 topRight = corners[2];

            //Return whether or not the position fits in the corners.
            return position.x > bottomLeft.x && position.x < topRight.x
                && position.y > bottomLeft.y && position.y < topRight.y;
        }

        // Update is called once per frame
        void Update()
        {
            //Can't keep track of current selected without event system so return.
            if (!EventSystem)
                return;

            //Try to update the rect transform of the current item if needed.
            if (_currentItem != EventSystem.currentSelectedGameObject)
            {
                _currentItem = EventSystem.currentSelectedGameObject.GetComponent<RectTransform>();
            }

            //If the item is outside the view area...
            if (!CheckInBoundsOfMask())
            {
                //Find the direction to scroll to.
                Vector3 direction = (_view.position - _currentItem.position).normalized;
                direction.z = 0f;

                //Remove the x based on the option.
                if (!_scrollHorizontal)
                    direction.x = 0f;

                _content.position += direction * _distanceToScroll;

            }
        }
    }
}