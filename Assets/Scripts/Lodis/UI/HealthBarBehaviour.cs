using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Lodis.UI;

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
        [SerializeField]
        private Text _damageCounter;
        [SerializeField]
        private Color _damageCounterDefaultColor;
        [SerializeField]
        private Color _damageCounterMaxColor;
        private ShakeBehaviour _damageCounterShake;
        private float _maxValue = 1;
        private float _lastHealth;
        private bool _dangerModeActive;
        private TextFlashBehaviour _damageFlash;

        public HealthBehaviour HealthComponent { get => _healthComponent; set => _healthComponent = value; }
        public float MaxValue { get => _maxValue; set => _maxValue = value; }



        // Start is called before the first frame update
        void Start()
        {
            HealthComponent = BlackBoardBehaviour.Instance.GetPlayerFromID(_targetID).GetComponent<HealthBehaviour>();
            MaxValue = HealthComponent.MaxHealth.Value;

            _damageCounterShake = _damageCounter.GetComponent<ShakeBehaviour>();
            _damageFlash = _damageCounter.GetComponent<TextFlashBehaviour>();

            HealthComponent.AddOnTakeDamageAction(_damageCounterShake.ShakeAnchoredPosition);

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

            if (_healthComponent.Health == _healthComponent.MaxHealth && !_dangerModeActive)
            {
                _damageCounter.text = "Danger";
                _damageFlash.BaseColor = _damageCounterMaxColor;
                _damageFlash.StartFlash();    
                _dangerModeActive = true;
            }
            else if (_healthComponent.Health < _healthComponent.MaxHealth)
            {
                _damageFlash.StopFlash();
                _damageCounter.text = Mathf.RoundToInt(_healthComponent.Health).ToString() +"%";
                _damageCounter.color = _damageCounterDefaultColor;
                _dangerModeActive = false;
            }

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
