using Lodis.Input;
using Lodis.Sound;
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
    private AudioClip _typeSound;
    [SerializeField]
    private float _typeSoundVolume;
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
    private bool _isTyping;
    private bool _shouldSkipDelay;
    private int _lastSectionIndex;

    private Text _text;
    private PlayerControls _controls;
    private Coroutine _typeRoutine;

    public bool IsTyping { get => _isTyping; private set => _isTyping = value; }

    // Start is called before the first frame update
    void Awake()
    {
        _text = GetComponent<Text>();
        _controls = new PlayerControls();

        if (_waitForInputOnEnd)
        {
            InputBehaviour.OnActionDown(() => _canContinue = true);
        }

        if (_forceCompleteOnSubmit)
        {
            InputBehaviour.OnActionDown(ForceComplete);

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
            IsTyping = true;

            if (_textToType[i] == '\n' && _waitForInputOnEnd)
            {
                IsTyping = false;
                _onSectionComplete?.Invoke();
                yield return new WaitUntil(() => _canContinue);
                i++;
                _lastSectionIndex = i;
                currentText = "";
                _onSectionStart?.Invoke();
                _canContinue = false;
                _shouldSkipDelay = false;
            }

            currentText += _textToType[i];
            _text.text = currentText;
            SoundManagerBehaviour.Instance.PlaySound(_typeSound, _typeSoundVolume);

            if (!_shouldSkipDelay)
                yield return new WaitForSeconds(_typeDelay);
        }

        _onTypeComplete?.Invoke();
    }

    public void ForceComplete()
    {
        if (!IsTyping)
            return;

        _shouldSkipDelay = true;
        _canContinue = false;
    }

    public void BeginTyping(float delay)
    {
        if (_textToType == "")
            return;

        _typeRoutine = StartCoroutine(TypeText(delay));
    }
}
