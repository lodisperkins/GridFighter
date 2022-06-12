using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using UnityEngine.UI;
using Lodis.Utility;

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

    public MovesetBehaviour Target { get => _target; set => _target = value; }

    public void Init(MovesetBehaviour target)
    {
        _target = target;
        _slider.maxValue = Target.BurstChargeTime;
        Target.OnBurst += () => _slider.value = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;

        if (!_slider.IsFilled())
        {
            _slider.value += Time.deltaTime;
            _fill.color = _defaultColor;
        }
        else
            _fill.color = _fullColor;
    }
}
