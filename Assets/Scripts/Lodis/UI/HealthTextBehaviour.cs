using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthTextBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject _target;
    private HealthBehaviour _healthComponent;
    [SerializeField]
    private Gradient _healthGradient;
    [SerializeField]
    private Text _text;
    private float _maxValue = 1;
    public HealthBehaviour HealthComponent { get => _healthComponent; set => _healthComponent = value; }
    public float MaxValue { get => _maxValue; set => _maxValue = value; }

    // Start is called before the first frame update
    void Start()
    {
        if (_target)
        {
            HealthComponent = _target.GetComponent<HealthBehaviour>();
            MaxValue = HealthComponent.MaxHealth.Value;
        }

        _text.color = _healthGradient.Evaluate(1f);
    }

    // Update is called once per frame
    void Update()
    {
        _text.text = Mathf.Round(_healthComponent.Health).ToString();
        _text.color = _healthGradient.Evaluate(_healthComponent.Health / _healthComponent.MaxHealth.Value);
    }
}
