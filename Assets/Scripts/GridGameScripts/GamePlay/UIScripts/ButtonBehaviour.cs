using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;

public class ButtonBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{

    public UnityEvent _onHighlight;
    [SerializeField]
    public UnityEvent _onUnhighlight;

    public void OnDeselect(BaseEventData eventData)
    {
        if (_onUnhighlight != null)
            _onUnhighlight.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_onHighlight != null)
            _onHighlight.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_onUnhighlight != null)
            _onUnhighlight.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_onHighlight != null)
            _onHighlight.Invoke();
    }
}

    
