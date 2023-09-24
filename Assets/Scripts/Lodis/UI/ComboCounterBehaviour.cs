using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.ScriptableObjects;
using Lodis.Gameplay;
using Lodis.Movement;
using UnityEngine.UI;
using DG.Tweening;

namespace Lodis.UI
{
    [System.Serializable]
    struct ComboMessage
    {
        public int CountRequirement;
        public Color MessageColor;
        public string Message;
        public AudioClip AnnouncerClip;
    }

    public class ComboCounterBehaviour : MonoBehaviour
    {
        [SerializeField] private IntVariable _playerID;
        [SerializeField] private ComboMessage[] _comboMessages;
        [SerializeField] private float _messageDespawnDelay;
        [SerializeField] private Text _comboText;
        [SerializeField] private float _effectScale;
        [SerializeField] private float _effectDuration;
        [SerializeField] private AudioSource _announcer;
        private int _minHitCount;
        private bool _canCount;
        private TimedAction _disableTextAction;
        private KnockbackBehaviour _ownerOpponent;
        private int _nextComboMessageIndex;
        private int _hitCount;
        private string _currentComboMessage;
        private Color _currentColor;
        private AudioClip _currentClip;
        private CharacterStateMachineBehaviour _opponentStateMachine;
        [SerializeField]
        private StadiumMonitorBehaviour _stadiumMonitor;

        // Start is called before the first frame update
        void Awake()
        {
            _currentColor = Color.white;
            _minHitCount = _comboMessages[0].CountRequirement;
        }

        private void Start()
        {
            _ownerOpponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(_playerID).GetComponent<KnockbackBehaviour>();
            _opponentStateMachine = _ownerOpponent.GetComponent<CharacterStateMachineBehaviour>();

            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(() =>
            {
                ResetComboMessage();
                _hitCount = 0;
                _nextComboMessageIndex = 0;
            });

            _ownerOpponent.AddOnTakeDamageAction(() => _canCount = true);
            _ownerOpponent.AddOnTakeDamageAction(UpdateComboMessage);
            _opponentStateMachine.AddOnStateChangedAction(DisplayComboMessage);
        }

        private void StartSpawnEffect()
        {
            _comboText.rectTransform.DOComplete();
            _comboText.rectTransform.DOPunchScale(new Vector3(_effectScale, _effectScale, _effectScale), _effectDuration);
        }

        private void UpdateComboMessage()
        {
            _hitCount++;
            if (!_canCount || _hitCount < _minHitCount)
                return;

            if (_disableTextAction?.GetEnabled() == true)
            {
                RoutineBehaviour.Instance.StopAction(_disableTextAction);
                ResetComboMessage();
            }

            _comboText.enabled = true;

            if (_nextComboMessageIndex < _comboMessages.Length && _hitCount >= _comboMessages[_nextComboMessageIndex].CountRequirement)
            {
                _currentComboMessage = _comboMessages[_nextComboMessageIndex].Message;
                _currentColor = _comboMessages[_nextComboMessageIndex].MessageColor;
                _currentClip = _comboMessages[_nextComboMessageIndex].AnnouncerClip;
                _nextComboMessageIndex++;
                _announcer.Stop();
            }

            _stadiumMonitor?.SetComboScreenActive();

            _comboText.color = _currentColor;
            _comboText.text = _hitCount + " Hits";
            StartSpawnEffect();
        }

        public void DisplayComboMessage()
        {
            string currentState = _opponentStateMachine.LastState;
            if (!_comboText.enabled || currentState != "Idle" || _disableTextAction?.GetEnabled() == true)
                return;

            _canCount = false;
            StartSpawnEffect();
            _announcer.Stop();
            _announcer.PlayOneShot(_currentClip);
            _comboText.text = _currentComboMessage;

            RoutineBehaviour.Instance.StopAction(_disableTextAction);
            _disableTextAction = RoutineBehaviour.Instance.StartNewTimedAction(args => ResetComboMessage(), TimedActionCountType.SCALEDTIME, _messageDespawnDelay);

            _hitCount = 0;
            _nextComboMessageIndex = 0;
        }

        private void ResetComboMessage()
        {
            if (_stadiumMonitor?.CurrentScreenActive == MonitorScreen.COMBOSCREEN)
                _stadiumMonitor.SetVSPanelActive();

            _comboText.enabled = false;
            _announcer.Stop();
            _currentComboMessage = _comboMessages[0].Message;
            _currentColor = _comboMessages[0].MessageColor;
        }
    }
}
