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

    public void Shake()
    {
        transform.DOShakeRotation(_duration, _strength, _frequency, 90);
    }
}
