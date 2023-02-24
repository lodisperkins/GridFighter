using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;

namespace Lodis.UI
{
    [RequireComponent(typeof(Button))]
    public class EventButtonBehaviour : MonoBehaviour, ISelectHandler,IDeselectHandler, IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField]
        private UnityEvent _onSelect;
        [SerializeField]
        private UnityEvent _onDeselect;
        [SerializeField]
        private UnityEvent _onHighlight;
        [SerializeField]
        private UnityEvent _onUnhighlight;

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
            _onSelect?.Invoke();
        }

        
    }
}