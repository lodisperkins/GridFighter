using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Lodis.UI
{
    public class AutoScrollBehaviour : MonoBehaviour
    {
        [Header("Window Contents")]
        [Tooltip("The object that has the mask for the viewing area.")]
        [SerializeField]
        private RectTransform _view;
        [Tooltip("The event system needed in order to know which option is selected.")]
        [SerializeField]
        private EventSystem _eventSystem;
        [Tooltip("The options that the player will scroll through.")]
        [SerializeField]
        private RectTransform _content;

        [Header("Scroll Options")]
        [SerializeField]
        private bool _scrollHorizontal;
        [Tooltip("How far to move the options when the selected option is not in view.")]
        [SerializeField]
        private float _distanceToScroll;
        [Tooltip("Whether or not the window will snap or smoothly lerp to the new position.")]
        [SerializeField]
        private bool _scrollSmooth;
        [Tooltip("The amount of time it takes to scroll to the new option smoothly.")]
        [SerializeField]
        private float _scrollSmoothDuration;
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

        private void OnDisable()
        {
            _content.DOKill();
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

            //If the item is inside the view area...
            if (CheckInBoundsOfMask())
                return;

            //Find the direction to scroll to.
            Vector3 direction = (_view.position - _currentItem.position).normalized;
            direction.z = 0f;

            //Remove the x based on the option.
            if (!_scrollHorizontal)
                direction.x = 0f;

            if (_scrollSmooth)
            {
                _content.DOKill();
                Vector3 newPosition = _content.position + direction * _distanceToScroll;
                _content.DOMove(newPosition, _scrollSmoothDuration);
            }
            else
            {
                _content.position += direction * _distanceToScroll;
            }
        }
    }
}