using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Lodis.Gameplay;
using Lodis.Utility;

namespace Lodis.UI
{
    public class StartEffectBehaviour : MonoBehaviour
    {
        [SerializeField]
        private FloatVariable _matchStartTime;
        [SerializeField]
        private float _textEffectDuration;
        [SerializeField]
        private float _textEnableDelay;
        [SerializeField]
        private float _textDisableDelay;
        [SerializeField]
        private Vector3 _scaleEffectStrength;
        [SerializeField]
        private Text _startText;
        [SerializeField]
        private ParticleSystem _startEffect;
        [SerializeField]
        private ParticleSystem _secondaryStartEffect;

        // Start is called before the first frame update
        void Start()
        {
            GameManagerBehaviour.Instance.AddOnMatchStartAction(() =>
            {
                BeginStartMatchEffect(true);
                RoutineBehaviour.Instance.StartNewTimedAction(args => DisableAll(), TimedActionCountType.SCALEDTIME, _textDisableDelay);
            });

            GameManagerBehaviour.Instance.AddOnMatchRestartAction( () => BeginStartMatchEffect(false));
            BeginStartMatchEffect(false);
        }

        private void DisableAll()
        {
            _startEffect.gameObject.SetActive(false);
            _secondaryStartEffect.gameObject.SetActive(false);
            _startText.enabled = false;
        }

        public void BeginStartMatchEffect(bool isSecondary)
        {

            _secondaryStartEffect.gameObject.SetActive(isSecondary);
            _startEffect.gameObject.SetActive(!isSecondary);

            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                _startText.text = isSecondary? "Fight" : "Ready";
                _startText.enabled = true;
                _startText.rectTransform.DOPunchScale(_scaleEffectStrength, _textEffectDuration);

            }, TimedActionCountType.SCALEDTIME, _textEnableDelay);
        }
    }
}