using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Lodis.UI;
using UnityEngine.Events;
using NaughtyAttributes;
using Types;

namespace Lodis.Gameplay
{
    public class HealthBarBehaviour : MonoBehaviour
    {
        [Header("Display Options")]
        [SerializeField] private bool _isPlayer = true;
        [ShowIf("_isPlayer")]
        [SerializeField] private int _targetID;
        [HideIf("_isPlayer")]
        [SerializeField] private HealthBehaviour _healthComponent;
        [SerializeField] private bool _textOnly;

        [Header("Health Bar UI References")]
        [HideIf("_textOnly")]
        [SerializeField] private Gradient _healthGradient;
        [HideIf("_textOnly")]
        [SerializeField] private Image _fill;
        [HideIf("_textOnly")]
        [SerializeField] private Image[] _imagesToUpdate;
        [HideIf("_textOnly")]
        [SerializeField] private Slider _slider;

        [Header("Damage Counter")]
        [SerializeField] private Text _damageCounter;
        [SerializeField] private Gradient _damageCounterDefaultColor;
        [SerializeField] private Color _damageCounterMaxColor;

        [Header("Danger mode")]
        [SerializeField] private bool _showInDanger = true;
        [ShowIf("_showInDanger")]
        [SerializeField] private UnityEvent _onDangerModeActive;
        [ShowIf("_showInDanger")]
        [SerializeField] private AudioClip _dangerVoiceClip;
        [ShowIf("_showInDanger")]
        [SerializeField] private bool _dangerIsThreshold;
        [ShowIf(EConditionOperator.And, "_dangerIsThreshold", "_showInDanger")]
        [SerializeField] private Fixed32 _dangerModeThreshold;

        //----
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
            if (_isPlayer)
                HealthComponent = BlackBoardBehaviour.Instance.GetPlayerFromID(_targetID).GetComponent<HealthBehaviour>();

            MaxValue = HealthComponent.MaxHealth.FixedValue;

            if (!_textOnly)
            {
                _slider = GetComponent<Slider>();
                _fill.color = _healthGradient.Evaluate(1f);
            }
            _damageCounterShake = _damageCounter.GetComponent<ShakeBehaviour>();
            _damageFlash = _damageCounter.GetComponent<TextFlashBehaviour>();

            HealthComponent.AddOnTakeDamageAction(_damageCounterShake.ShakeAnchoredPosition);
        }

        public bool CheckInDanger()
        {
            if (_dangerIsThreshold)
            {
                return _healthComponent.Health <= _dangerModeThreshold;
            }

            return _healthComponent.Health >= _healthComponent.MaxHealth;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_textOnly)
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
            }


            if (CheckInDanger() && !_dangerModeActive && _showInDanger)
            {
                _damageCounter.text = "Danger";
                _damageFlash.BaseColor = _damageCounterMaxColor;
                _damageFlash.StartFlash();    
                _dangerModeActive = true;
                _onDangerModeActive?.Invoke();

                if (_dangerVoiceClip)
                    Sound.SoundManagerBehaviour.Instance.PlayerAnnouncerSound(_dangerVoiceClip);
            }
            else if (!CheckInDanger() || !_showInDanger)
            {
                _damageFlash.StopFlash();
                _damageCounter.text = Mathf.RoundToInt(_healthComponent.Health).ToString() +"%";
                _damageCounter.color = _damageCounterDefaultColor.Evaluate(_healthComponent.Health / _healthComponent.MaxHealth);
                _dangerModeActive = false;
            }

            _lastHealth = _healthComponent.Health;
        }
    }
}
