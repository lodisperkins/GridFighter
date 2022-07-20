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

    public void ShakeRotation(float duration, float strength, int frequency)
    {
         _tweener = transform.DOShakeRotation(duration, strength, frequency, 90);
        _tweener.onComplete += () => transform.rotation = _startRotation;
    }

    public void ShakePosition()
    {
        if (_tweener?.active == false)
            _startPosition = transform.localPosition;

        _tweener = transform.DOShakePosition(_duration, _strength, _frequency, 90);
        _tweener.onComplete += () => transform.localPosition = _startPosition;
    }

    public void ShakePosition(float duration, float strength, int frequency)
    {
        if (_tweener?.active == false)
            _startPosition = transform.localPosition;

        _tweener = transform.DOShakePosition(duration, new Vector3(strength, strength, 0), frequency, 90, false, true);
        if (_tweener != null)
            _tweener.onComplete += () => transform.localPosition = _startPosition;
    }

    internal void ShakeRotation(float fallScreenShakeDuration, object fallScreenShakeStrength, object fallScreenShakeFrequency)
    {
        throw new NotImplementedException();
    }
}
