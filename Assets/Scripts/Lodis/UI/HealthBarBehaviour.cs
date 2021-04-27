using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.Gameplay
{
    public class HealthBarBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _target;

        private HealthBehaviour _healthComponent;
        [SerializeField]
        private Gradient _healthGradient;
        [SerializeField]
        private Image _fill;
        [SerializeField]
        private Slider _slider;
        private float _maxValue = 1;
        public HealthBehaviour HealthComponent { get => _healthComponent; set => _healthComponent = value; }
        public float MaxValue { get => _maxValue; set => _maxValue = value; }



        // Start is called before the first frame update
        void Start()
        {
            if (_target)
            {
                HealthComponent = _target.GetComponent<HealthBehaviour>();
                MaxValue = HealthComponent.Health;
            }

            _slider = GetComponent<Slider>();
            _fill.color = _healthGradient.Evaluate(1f);
        }

        // Update is called once per frame
        void Update()
        {
            if (_healthComponent != null)
                _slider.value = _healthComponent.Health;

            _slider.maxValue = MaxValue;

            _fill.color = _healthGradient.Evaluate(_slider.value / _slider.maxValue);
        }
    }
}
