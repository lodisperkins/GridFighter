using System;
using Lodis.Utility;
using UnityEngine;
using static Lodis.Utility.RoutineBehaviour;

namespace Lodis.Gameplay
{
    public class RingBarrierFeedbackBehaviour : MonoBehaviour
    {
        private RingBarrierBehaviour _health;
        [SerializeField] private MeshRenderer _visual;
        [SerializeField] private Gradient _healthGradient;
        private float _baseTransparency;
        [SerializeField] [Range(0,1)] private float _maxTransparency;
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        private float _currentDuration;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Color _materialColor;
        private bool _fadeEnabled;
        private int _fadeDirection;
        private DelayedAction _fadeAction;
        
        // Start is called before the first frame update
        void Awake()
        {
            _health = GetComponent<RingBarrierBehaviour>();
            _health.AddOnTakeDamageAction(StartFadeIn);
        }

        private void StartFadeIn()
        {
            if (_fadeAction?.GetEnabled() == true)
                Instance.StopAction(_fadeAction);

            _materialColor = _visual.material.color;
            _materialColor.a = _baseTransparency;
            _visual.material.color = _materialColor;
            
            _fadeEnabled = true;
            _currentDuration = _fadeInDuration;
            _fadeDirection = 1;
            _fadeAction = Instance.StartNewConditionAction(StartFadeOut, condition => _materialColor.a >= _maxTransparency);
        }

        private void StartFadeOut(params object[] objects)
        {
            if (_fadeAction?.GetEnabled() == true)
                Instance.StopAction(_fadeAction);

            _fadeDirection = -1;
            _currentDuration = _fadeOutDuration;
            _fadeAction = Instance.StartNewConditionAction(args =>
            {
                _fadeEnabled = false;
                _materialColor.a = _baseTransparency;
                _visual.material.color = _materialColor;
            }, condition => _materialColor.a <= _baseTransparency);
        }
        
        // Update is called once per frame
        void Update()
        {
            if (!_fadeEnabled)
            {
                _visual.material.SetColor(EmissionColor, _healthGradient.Evaluate(_health.Health / _health.MaxHealth.Value));
                _baseTransparency = _healthGradient.Evaluate(_health.MaxHealth.Value / _health.Health).a;
                return;
            }
            
            _materialColor = _visual.material.color;
            _materialColor.a += (Time.deltaTime * (_maxTransparency - _baseTransparency) / _currentDuration) *
                                _fadeDirection;
            _visual.material.color = _materialColor;
        }
    }
}
