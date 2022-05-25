using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyBarBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject _target;

    private MovesetBehaviour _movesetComponent;
    [SerializeField]
    private Gradient _energyGradient;
    [SerializeField]
    private Image _fill;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private FloatVariable _maxValue;
    public MovesetBehaviour MovesetComponent { get => _movesetComponent; set => _movesetComponent = value; }
    public FloatVariable MaxValue { get => _maxValue; }


    // Start is called before the first frame update
    void Start()
    {
        if (_target)
        {
            MovesetComponent = _target.GetComponent<MovesetBehaviour>();
        }

        _slider = GetComponent<Slider>();
        _slider.maxValue = MaxValue.Value;
        //_fill.color = _energyGradient.Evaluate(1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (_movesetComponent != null)
            _slider.value = _movesetComponent.Energy;

        //_fill.color = _energyGradient.Evaluate(_slider.value / _slider.maxValue);
    }
}
