using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class EventHandlerBehaviour : MonoBehaviour, ISubmitHandler, ICancelHandler
{

    [SerializeField]
    private UnityEvent _onCancel;
    [SerializeField]
    private UnityEvent _onSubmit;
    [SerializeField]
    private StandaloneInputModule _inputModule;

    private void Start()
    {
        if (!_inputModule)
            _inputModule = GetComponent<StandaloneInputModule>();
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (_onCancel != null)
            _onCancel.Invoke();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_onSubmit != null)
            _onSubmit.Invoke();
    }

    private void Update()
    {
        if (Input.GetButtonDown(_inputModule.cancelButton) && _onCancel != null)
            _onCancel.Invoke();

        if (Input.GetButtonDown(_inputModule.submitButton) && _onSubmit != null)
            _onSubmit.Invoke();
    }
}
