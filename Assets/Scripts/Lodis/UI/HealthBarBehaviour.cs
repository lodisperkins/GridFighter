﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Lodis.Gameplay
{
    public class HealthBarBehaviour : MonoBehaviour
    {
        [SerializeField]
        private int _targetID;

        private HealthBehaviour _healthComponent;
        [SerializeField]
        private Gradient _healthGradient;
        [SerializeField]
        private Image _fill;
        [SerializeField]
        private Image[] _imagesToUpdate;
        [SerializeField]
        private Slider _slider;
        private float _maxValue = 1;
        private float _lastHealth;
        public HealthBehaviour HealthComponent { get => _healthComponent; set => _healthComponent = value; }
        public float MaxValue { get => _maxValue; set => _maxValue = value; }



        // Start is called before the first frame update
        void Start()
        {
            HealthComponent = BlackBoardBehaviour.Instance.GetPlayerFromID(_targetID).GetComponent<HealthBehaviour>();
            MaxValue = HealthComponent.MaxHealth.Value;

            _slider = GetComponent<Slider>();
            _fill.color = _healthGradient.Evaluate(1f);
        }

        // Update is called once per frame
        void Update()
        {
            if (_healthComponent != null)
                _slider.DOValue(_healthComponent.Health, 0.1f);

            _slider.maxValue = MaxValue;

            _fill.color = _healthGradient.Evaluate(_slider.value / _slider.maxValue);

            if (_lastHealth != _healthComponent.Health)
            {
                foreach(Image image in _imagesToUpdate)
                {
                    image.color = _healthGradient.Evaluate(_slider.value / _slider.maxValue);
                }
            }

            _lastHealth = _healthComponent.Health;
        }
    }
}
