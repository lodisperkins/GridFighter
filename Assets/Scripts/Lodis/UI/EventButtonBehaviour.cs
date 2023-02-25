using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using Lodis.Sound;

namespace Lodis.UI
{
    [RequireComponent(typeof(Button))]
    public class EventButtonBehaviour : MonoBehaviour, ISelectHandler,IDeselectHandler, IPointerEnterHandler,IPointerExitHandler
    {
        [Header("Button Events")]
        [SerializeField]
        private UnityEvent _onSelect;
        [SerializeField]
        private UnityEvent _onDeselect;
        [SerializeField]
        private UnityEvent _onHighlight;
        [SerializeField]
        private UnityEvent _onUnhighlight;
        private Button _button;

        [Header("Button Sounds")]
        [SerializeField]
        private AudioClip _selectSound;
        [SerializeField]
        private AudioClip _clickSound;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => SoundManagerBehaviour.Instance.PlaySound(_clickSound));
        }

        public void OnSelect()
        {
            if (!_button)
                _button = GetComponent<Button>();

            _button.OnSelect(null);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _onDeselect?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHighlight?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onUnhighlight?.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SoundManagerBehaviour.Instance.PlaySound(_selectSound);
            _onSelect?.Invoke();
        }
    }
}