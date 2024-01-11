using Lodis.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TextTypeBehaviour : MonoBehaviour
{
    [SerializeField]
    private string _textToType;
    [SerializeField]
    private float _typeDelay;
    [SerializeField]
    private bool _typeOnEnable;
    [SerializeField]
    private UnityEvent _onTypeComplete;
    [SerializeField]
    private UnityEvent _onSectionComplete;
    [SerializeField]
    private UnityEvent _onSectionStart;
    [SerializeField]
    private bool _waitForInputOnEnd;
    [SerializeField]
    private bool _forceCompleteOnSubmit;
    private bool _canContinue;

    private Text _text;
    private PlayerControls _controls;
    private Coroutine _typeRoutine;

    // Start is called before the first frame update
    void Awake()
    {
        _text = GetComponent<Text>();
        _controls = new PlayerControls();

        if (_waitForInputOnEnd)
        {
            _controls.UI.Submit.started += context =>
            {
                _canContinue = true;
            };
        }

        if (_forceCompleteOnSubmit)
        {
            _controls.UI.Submit.started += context =>
            {
                ForceComplete();
            };
        }
    }

    private void Start()
    {
        if (_typeOnEnable)
            BeginTyping(0);
    }

    public void SetTextToType(string text)
    {
        _textToType = text;
    }

    public void AddOnTypeCompleteAction(UnityAction action)
    {
        _onTypeComplete.AddListener(action);
    }
    
    public void AddOnSectionCompleteAction(UnityAction action)
    {
        _onSectionComplete.AddListener(action);
    }

    public void AddOnSectionStartAction(UnityAction action)
    {
        _onSectionComplete.AddListener(action);
    }

    private IEnumerator TypeText(float initalDelay)
    {
        string currentText = "";
        for (int i = 0; i < _textToType.Length; i++)
        {
            if (_textToType[i] == '\n' && _waitForInputOnEnd)
            {
                _onSectionComplete?.Invoke();
                yield return new WaitUntil(() => _canContinue);
                _onSectionStart?.Invoke();
                _canContinue = false;
            }

            currentText += _textToType[i];
            _text.text = currentText;
            yield return new WaitForSeconds(_typeDelay);
        }

        _onTypeComplete?.Invoke();
    }

    public void ForceComplete()
    {
        StopCoroutine(_typeRoutine);

        _text.text = _textToType;

        if (_waitForInputOnEnd)
            _onSectionComplete?.Invoke();

        _onTypeComplete?.Invoke();
    }

    public void BeginTyping(float delay)
    {
        _typeRoutine = StartCoroutine(TypeText(delay));
    }
}
