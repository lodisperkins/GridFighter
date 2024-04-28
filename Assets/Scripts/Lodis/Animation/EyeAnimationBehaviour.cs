using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Animation
{
    public class EyeAnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Renderer _renderer;
        [SerializeField]
        private string _textureName;

        [SerializeField]
        private float _timeBetweenBlinks;
        [SerializeField]
        private float _blinkDuration;

        [SerializeField]
        private Texture2D _idle;
        [SerializeField]
        private Texture2D _closed;
        [SerializeField]
        private Texture2D _hurt;
        [SerializeField]
        private Texture2D _angry;
        [SerializeField] 
        private CharacterStateMachineBehaviour _stateMachine;
        [SerializeField] 
        private HealthBehaviour _health;

        private Texture2D _currentOpenEye;

        private bool _isBlinking;
        private TimedAction _currentAction;
        private bool _canBlink = true;

        public Renderer Renderer { get => _renderer; set => _renderer = value; }


        private void Awake()
        {
            _canBlink = true;
            _currentOpenEye = _idle;

            if (!_health || !_stateMachine)
                return;

            _stateMachine.AddOnStateChangedAction(SetEyes);
            _health.AddOnTakeDamageAction(ChangeEyesToHurt);
            _health.AddOnStunAction(() => SetStunEffects(true));
            _health.AddOnStunDisabledAction(() => SetStunEffects(false));
        }

        private void Blink()
        {
            if (!Renderer)
                return;

            _isBlinking = true;
            Renderer.material.SetTexture(_textureName, _closed);

            _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                if (!Renderer)
                    return;

                Renderer.material.SetTexture(_textureName, _currentOpenEye);
                _isBlinking = false;
            }, TimedActionCountType.SCALEDTIME, _blinkDuration);

        }

        private void SetStunEffects(bool enabled)
        {
            if (enabled)
                ChangeEyesToHurt();
            else
                ChangeEyesToIdle();
        }

        private void SetEyes()
        {
            if (_stateMachine.StateMachine.CurrentState == "Idle")
                ChangeEyesToIdle();
            else if (_stateMachine.StateMachine.CurrentState == "Attacking")
                ChangeEyesToAngry();
        }

        public void ChangeEyesToClosed(float time = 0)
        {
            _canBlink = false;
            RoutineBehaviour.Instance.StopAction(_currentAction);
            _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _canBlink = true, TimedActionCountType.SCALEDTIME, time);
            _isBlinking = false;
            Renderer.material.SetTexture(_textureName, _closed);
        }

        public void ChangeEyesToHurt()
        {
            RoutineBehaviour.Instance.StopAction(_currentAction);
            _isBlinking = false;
            Renderer.material.SetTexture(_textureName, _hurt);
        }

        public void ChangeEyesToIdle()
        {
            _isBlinking = false;
            _currentOpenEye = _idle;
            Renderer.material.SetTexture(_textureName, _idle);
        }

        public void ChangeEyesToAngry()
        {
            _isBlinking = false;
            _currentOpenEye = _angry;
            Renderer.material.SetTexture(_textureName, _angry);
        }

        // Update is called once per frame
        void Update()
        {
            if ((_currentAction == null || !_currentAction.GetEnabled()) && !_isBlinking && _canBlink)
                _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args => Blink(), TimedActionCountType.SCALEDTIME, _timeBetweenBlinks);
        }
        
        
    }
}