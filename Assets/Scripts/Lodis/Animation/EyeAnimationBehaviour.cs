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

        private bool _isBlinking;
        private TimedAction _currentAction;

        public Renderer Renderer { get => _renderer; set => _renderer = value; }

        private void Blink()
        {
            _isBlinking = true;
            Renderer.material.SetTexture(_textureName, _closed);

            _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                Renderer.material.SetTexture(_textureName, _idle);
                _isBlinking = false;
            }, TimedActionCountType.SCALEDTIME, _blinkDuration);

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
            Renderer.material.SetTexture(_textureName, _idle);
        }

        // Update is called once per frame
        void Update()
        {
            if ((_currentAction == null || !_currentAction.GetEnabled()) && !_isBlinking)
                _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args => Blink(), TimedActionCountType.SCALEDTIME, _timeBetweenBlinks);
        }

        private void OnDestroy()
        {
            RoutineBehaviour.Instance.StopAction(_currentAction);
        }
    }
}