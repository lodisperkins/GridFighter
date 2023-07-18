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
        private FloatVariable _matchTime;
        [SerializeField]
        private float _textEffectDuration;
        [SerializeField]
        private float _textEnableDelay;
        [SerializeField]
        private float _textDisableDelay;
        [SerializeField]
        private Vector3 _scaleEffectStrength;
        [SerializeField]
        private Text _startTextBox;
        [SerializeField]
        private ParticleSystem _startEffect;
        [SerializeField]
        private ParticleSystem _secondaryStartEffect;
        [SerializeField]
        private ParticleSystem _suddenDeathStartEffect;
        [SerializeField]
        private ParticleSystem _suddenDeathSecondaryStartEffect;
        [SerializeField]
        private Color _suddenDeathTextColor;
        [SerializeField]
        private Color _startTextColor;
        [SerializeField]
        private Color _suddenDeathTextOutlineColor;
        [SerializeField]
        private Color _startTextOutlineColor;
        [SerializeField]
        private string _suddentDeathReadyUpText;
        [SerializeField]
        private string _suddenDeathMatchStartText;
        [SerializeField]
        private string _readyUpText;
        [SerializeField]
        private string _matchStartText;
        [SerializeField]
        private AudioSource _announcer;
        [SerializeField]
        private AudioClip _readyClip;
        [SerializeField]
        private AudioClip _suddenDeathReadyClip;
        [SerializeField]
        private AudioClip _startClip;
        private Rect _defaultRect;
        private Outline _textOutline;
        private TimedAction _currentAction;

        private void Awake()
        {
            _textOutline = _startTextBox.GetComponent<Outline>();
            _defaultRect = _startTextBox.rectTransform.rect;
        }

        // Start is called before the first frame update
        void Start()
        {
            MatchManagerBehaviour.Instance.AddOnMatchStartAction(() =>
            {
                _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args => DisableAll(), TimedActionCountType.SCALEDTIME, _textDisableDelay);
            });

            MatchManagerBehaviour.Instance.AddOnMatchRestartAction( () => { DisableAll(); BeginReadyUpEffect(); });
            BeginReadyUpEffect();
        }

        private void DisableAll()
        {
            RoutineBehaviour.Instance.StopAction(_currentAction);
            _startEffect.gameObject.SetActive(false);
            _secondaryStartEffect.gameObject.SetActive(false);
            _suddenDeathSecondaryStartEffect.gameObject.SetActive(false);
            _suddenDeathStartEffect.gameObject.SetActive(false);
            _startTextBox.enabled = false;
            _startTextBox.rectTransform.rect.Set(_defaultRect.x, _defaultRect.y, _defaultRect.width, _defaultRect.height);
        }

        private void BeginReadyUpEffect()
        {
            _startTextBox.rectTransform.rect.Set(_defaultRect.x, _defaultRect.y, _defaultRect.width, _defaultRect.height);
            AudioClip currentClip = null;
            if (MatchManagerBehaviour.Instance.SuddenDeathActive)
            {
                _suddenDeathSecondaryStartEffect.gameObject.SetActive(false);
                _suddenDeathStartEffect.gameObject.SetActive(true);
                _textOutline.effectColor = _suddenDeathTextOutlineColor;
                _startTextBox.color = _suddenDeathTextColor;
                currentClip = _suddenDeathReadyClip;
            }
            else
            {
                _secondaryStartEffect.gameObject.SetActive(false);
                _startEffect.gameObject.SetActive(true);
                _textOutline.effectColor = _startTextOutlineColor;
                _startTextBox.color = _startTextColor;
                currentClip = _readyClip;
            }

            _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                _startTextBox.text = MatchManagerBehaviour.Instance.SuddenDeathActive ? _suddentDeathReadyUpText : _readyUpText;
                _startTextBox.enabled = true;
                _startTextBox.rectTransform.DOPunchScale(_scaleEffectStrength, _textEffectDuration).onComplete = BeginMatchStartEffect;
                _announcer.Stop();
                _announcer.PlayOneShot(_readyClip);

            }, TimedActionCountType.SCALEDTIME, _textEnableDelay);
        }

        private void BeginMatchStartEffect()
        {
            float currentDelay = (_matchStartTime.Value - MatchTimerBehaviour.Instance.TimeSinceRoundStart);

            if (MatchManagerBehaviour.Instance.SuddenDeathActive)
                _suddenDeathSecondaryStartEffect.gameObject.SetActive(true);
            else
                _secondaryStartEffect.gameObject.SetActive(true);

            _currentAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                if (MatchManagerBehaviour.Instance.SuddenDeathActive)
                    _suddenDeathStartEffect.gameObject.SetActive(false);
                else
                    _startEffect.gameObject.SetActive(false);

                _startTextBox.text = MatchManagerBehaviour.Instance.SuddenDeathActive ? _suddenDeathMatchStartText : _matchStartText;
                _startTextBox.enabled = true;
                _startTextBox.rectTransform.DOPunchScale(_scaleEffectStrength, _textEffectDuration);
                _announcer.Stop();
                _announcer.PlayOneShot(_startClip);

            }, TimedActionCountType.SCALEDTIME, currentDelay);
        }
    }
}