using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class AutoScrollBehaviour : MonoBehaviour
    {
        [Header("Window Contents")]
        [Tooltip("The object that has the mask for the viewing area.")]
        [SerializeField] private RectTransform _view;

        [Tooltip("The event system needed in order to know which option is selected.")]
        [SerializeField] private EventSystem _eventSystem;

        [Tooltip("The options that the player will scroll through.")]
        [SerializeField] private RectTransform _content;

        [Header("Scroll Options")]
        [Tooltip("Scroll horizontally instead of vertically.")]
        [SerializeField] private bool _scrollHorizontal;

        [Tooltip("How far to move the options when the selected option is not in view.\n" +
                 "If the scroll behaviour is rapidly moving up and down, you may have the scroll distance too large.\n" +
                 "If the scroll behaviour is slowly moving towards its destination, you may have the scroll distance too small.")]
        [SerializeField] private float _distanceToScroll = 50f;

        [Tooltip("Whether or not the window will snap or smoothly lerp to the new position.")]
        [SerializeField] private bool _scrollSmooth = true;

        [Tooltip("The amount of time it takes to scroll to the new option smoothly.")]
        [SerializeField] private float _scrollSmoothDuration = 0.25f;

        [Tooltip("Scroll using the transform's local position instead of the global position.")]
        [SerializeField] private bool _useLocalPosition = true;

        [Tooltip("Extra margin inside the viewport to avoid edge sensitivity.")]
        [SerializeField] private float _viewportMargin = 10f;

        private RectTransform _currentItem;
        private TweenerCore<Vector3, Vector3, VectorOptions> _moveTween;

        public EventSystem EventSystem { get => _eventSystem; set => _eventSystem = value; }

        /// <summary>
        /// Check if the currently selected UI item is within the visible bounds of the mask/view.
        /// </summary>
        private bool IsItemVisible()
        {
            if (_currentItem == null)
                return true;

            Rect itemRect = GetWorldRect(_currentItem);
            Rect viewRect = GetWorldRect(_view);

            // Expand view rect slightly to add a margin buffer
            viewRect.xMin += _viewportMargin;
            viewRect.xMax -= _viewportMargin;
            viewRect.yMin += _viewportMargin;
            viewRect.yMax -= _viewportMargin;

            return viewRect.Overlaps(itemRect);
        }

        /// <summary>
        /// Gets the world-space bounding rect of a RectTransform.
        /// </summary>
        private Rect GetWorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }

        /// <summary>
        /// Moves the scroll content smoothly using DOTween.
        /// </summary>
        private void ScrollSmoothly(Vector3 direction)
        {
            if (_moveTween != null && _moveTween.IsActive()) return;

            Vector3 newPosition = _useLocalPosition
                ? _content.localPosition + direction * _distanceToScroll
                : _content.position + direction * _distanceToScroll;

            // Kill any existing tweens on this target
            DOTween.Kill(_content);

            _moveTween = (_useLocalPosition
                ? _content.DOLocalMove(newPosition, _scrollSmoothDuration)
                : _content.DOMove(newPosition, _scrollSmoothDuration))
                .SetEase(Ease.OutCubic)
                .SetTarget(_content);
        }

        private void OnDisable()
        {
            if (_content != null)
                DOTween.Kill(_content);
        }

        private void Update()
        {
            // Can't keep track of current selected without event system so return.
            if (!_eventSystem)
                return;

            GameObject selectedObj = _eventSystem.currentSelectedGameObject;

            // Try to update the rect transform of the current item if needed.
            if (selectedObj == null || !selectedObj.TryGetComponent(out RectTransform selectedRect))
                return;

            if (_currentItem != selectedRect)
                _currentItem = selectedRect;

            // If the item is inside the view area, no scrolling is needed.
            if (IsItemVisible())
                return;

            // Find the direction to scroll to.
            Vector3 direction = (_view.position - _currentItem.position).normalized;
            direction.z = 0f;

            // Remove the x based on the option.
            if (!_scrollHorizontal)
                direction.x = 0f;
            else
                direction.y = 0f;

            direction = Vector3.ClampMagnitude(direction, 1f);

            if (_scrollSmooth)
            {
                ScrollSmoothly(direction);
                return;
            }

            Vector3 delta = direction * _distanceToScroll;

            if (_useLocalPosition)
                _content.localPosition += delta;
            else
                _content.position += delta;
        }
    }
}
