using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using UnityEngine.UI;
using Lodis.Utility;
using UnityEngine.Events;
using DG.Tweening;
using Lodis.Sound;

public class BurstMeterBehaviour : MonoBehaviour
{
    [Header("UI Scene References")]
    [SerializeField] private Image _fill;
    [SerializeField] private Slider _slider;
    [SerializeField] private GameObject _defensiveEffect;
    [SerializeField] private GameObject _offensiveEffect;

    [Header("Color Options")]
    [SerializeField] private Color _fullColor;
    [SerializeField] private Color _halfFullColor;
    [SerializeField] private Color _defaultColor;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip _offensiveBurstReady;
    [SerializeField] private AudioClip _defensiveBurstReady;

    [Header("Fill Events")]
    [SerializeField] private UnityEvent _onFilled;
    [SerializeField] private UnityEvent _onHalfFilled;


    //---
    private bool _filledEventCalled;
    private bool _halfFilledEventCalled;
    private MovesetBehaviour _target;

    public MovesetBehaviour Target { get => _target; set => _target = value; }

    public void Init(MovesetBehaviour target)
    {
        _target = target;
        _slider.maxValue = Target.MaxBurstEnergy.FixedValue;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;

        _slider.DOValue(Target.BurstEnergy, 0.1f);

        //Handle displaying effects.
        if (_slider.IsFilled())
        {
            //Change the color and toggle the defense effect.
            _fill.color = _fullColor;
            _defensiveEffect.gameObject.SetActive(true);
            _offensiveEffect.gameObject.SetActive(false);

            if (!_filledEventCalled)
            {
                _onFilled?.Invoke();
                _filledEventCalled = true;
                SoundManagerBehaviour.Instance.PlaySound(_defensiveBurstReady);
            }
        }
        //In case it was set manually without the meter value being set.
        else if (Target.CanDefensiveBurst)
        {

            //Change the color and toggle the defense effect.
            _fill.color = _fullColor;
            _slider.value = _slider.maxValue;
            _defensiveEffect.gameObject.SetActive(true);
            _offensiveEffect.gameObject.SetActive(false);

            if (!_filledEventCalled)
            {
                _onFilled?.Invoke();
                _filledEventCalled = true;
                SoundManagerBehaviour.Instance.PlaySound(_defensiveBurstReady);
            }
        }
        else if (_slider.value >= _slider.maxValue / 2)
        {

            //Change the color and toggle the defense effect.
            _fill.color = _halfFullColor;
            _offensiveEffect.gameObject.SetActive(true);
            _defensiveEffect.gameObject.SetActive(false);

            if (!_halfFilledEventCalled)
            {
                _onHalfFilled?.Invoke();
                _halfFilledEventCalled = true;
                SoundManagerBehaviour.Instance.PlaySound(_offensiveBurstReady);
            }
        }
        //In case it was set manually without the meter value being set.
        else if (Target.CanOffensiveBurst)
        {

            //Change the color and toggle the defense effect.
            _fill.color = _halfFullColor;
            _slider.value = 0.5f;
            _offensiveEffect.gameObject.SetActive(true);
            _defensiveEffect.gameObject.SetActive(false);


            if (!_halfFilledEventCalled)
            {
                _onHalfFilled?.Invoke();
                _halfFilledEventCalled = true;
                SoundManagerBehaviour.Instance.PlaySound(_offensiveBurstReady);
            }
        }
        else
        {
            //Reset everything if none of the condtions were met.
            _fill.color = _defaultColor;

            _offensiveEffect.gameObject.SetActive(false);
            _defensiveEffect.gameObject.SetActive(false);
            _filledEventCalled = false;
            _halfFilledEventCalled = false;
        }

    }
}
