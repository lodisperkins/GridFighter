using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using UnityEngine.UI;
using Lodis.Utility;
using UnityEngine.Events;

public class BurstMeterBehaviour : MonoBehaviour
{
    private MovesetBehaviour _target;
    [SerializeField]
    private Image _fill;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private Color _fullColor;
    [SerializeField]
    private Color _defaultColor;
    [SerializeField]
    private UnityEvent _onFilled;
    private bool _filledEventCalled;

    public MovesetBehaviour Target { get => _target; set => _target = value; }

    public void Init(MovesetBehaviour target)
    {
        _target = target;
        _slider.maxValue = Target.MaxBurstEnergy.Value;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;
        _slider.value = Target.BurstEnergy;

        if (_slider.IsFilled())
        {
            _fill.color = _fullColor;

            if (!_filledEventCalled)
                _onFilled?.Invoke();
        }
        else if (Target.CanBurst)
        {
            _fill.color = _fullColor;
            _slider.value = _slider.maxValue;


            if (!_filledEventCalled)
                _onFilled?.Invoke();
        }
        else
        {
            _fill.color = _defaultColor;

            _filledEventCalled = false;
        }

    }
}
