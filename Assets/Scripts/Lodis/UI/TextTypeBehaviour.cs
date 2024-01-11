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
    private Text _text;

    // Start is called before the first frame update
    void Awake()
    {
        _text = GetComponent<Text>();
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

    private IEnumerator TypeText(float initalDelay)
    {
        string currentText = "";
        for (int i = 0; i < _textToType.Length; i++)
        {
            currentText += _textToType[i];
            _text.text = currentText;
            yield return new WaitForSeconds(_typeDelay);
        }

        _onTypeComplete?.Invoke();
    }

    public void BeginTyping(float delay)
    {
        StartCoroutine(TypeText(delay));
    }
}
