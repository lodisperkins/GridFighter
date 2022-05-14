using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    private void Awake()
    {
        _startRotation = transform.rotation;
    }

    public void Shake()
    {
         _tweener = transform.DOShakeRotation(_duration, _strength, _frequency, 90);
        _tweener.onComplete += () => transform.rotation = _startRotation;
    }
}
