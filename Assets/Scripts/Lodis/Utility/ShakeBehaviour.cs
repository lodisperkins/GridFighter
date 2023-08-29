using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class ShakeBehaviour : MonoBehaviour
{
    [SerializeField]
    private float _duration;
    [SerializeField]
    private float _strength;
    [SerializeField]
    private int _frequency;
    private RectTransform _rectTransform;
    private Tweener _tweener;
    private Quaternion _startRotation;
    private Vector3 _startPosition;

    private void Awake()
    {
        _startRotation = transform.rotation;
        _startPosition = transform.position;
    }

    public void ShakeRotation()
    {
         _tweener = transform.DOShakeRotation(_duration, _strength, _frequency, 90);
        _tweener.onComplete += () => transform.rotation = _startRotation;
    }

    public void ShakeRotation(float strengthScale)
    {
         _tweener = transform.DOShakeRotation(_duration, _strength * strengthScale, _frequency, 90);
        _tweener.onComplete += () => transform.rotation = _startRotation;
    }

    public void ShakeRotation(float duration, float strength, int frequency)
    {
         _tweener = transform.DOShakeRotation(duration, strength, frequency, 90);
        _tweener.onComplete += () => transform.rotation = _startRotation;
    }

    public void ShakePosition()
    {
        _tweener = transform.DOShakePosition(_duration, _strength, _frequency, 90);
        _tweener.onComplete += () => transform.position = _startPosition;
    }

    public void ShakeAnchoredPosition()
    {
        if (!_rectTransform)
            _rectTransform = GetComponent<RectTransform>();

        if (_tweener == null || !_tweener.IsPlaying())
            _tweener = _rectTransform.DOShakeAnchorPos(_duration, _strength, _frequency, 90);
        else
            _tweener.Restart();
    }

    public void ShakePosition(float duration, float strength, int frequency)
    {
        if (duration <= 0)
            return;

        _tweener = transform.DOShakePosition(duration, new Vector3(strength, strength, 0), frequency, 90, false, true);
        _tweener.onComplete += () => transform.localPosition = _startPosition;
    }

}
