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
    private Vector3 _startPos;
    public void Shake()
    {
        _startPos = transform.position;
         _tweener = transform.DOShakeRotation(_duration, _strength, _frequency, 90);
        _tweener.onComplete += () => transform.position = _startPos;
    }
}
