﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.ScriptableObjects;
using Lodis.Gameplay;
using Lodis.Movement;
using UnityEngine.UI;

namespace Lodis.UI
{
    [System.Serializable]
    struct ComboMessage
    {
        public int CountRequirement;
        public Color MessageColor;
        public string Message;
    }

    public class ComboCounterBehaviour : MonoBehaviour
    {
        [SerializeField] private IntVariable _playerID;
        [SerializeField] private ComboMessage[] _comboMessages;
        [SerializeField] private float _messageDespawnDelay;
        [SerializeField] private Text _comboText;
        private int _minHitCount;
        private bool _canCount;
        private TimedAction _disableTextAction;
        private KnockbackBehaviour _ownerOpponent;
        private int _nextComboMessageIndex;
        private int _hitCount;
        private string _currentComboMessage;
        private Color _currentColor;

        // Start is called before the first frame update
        void Awake()
        {
            _currentColor = Color.white;
            _minHitCount = _comboMessages[0].CountRequirement;
        }

        private void Start()
        {
            _ownerOpponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(_playerID).GetComponent<KnockbackBehaviour>();
            _ownerOpponent.AddOnKnockBackAction(() => _canCount = true);
            _ownerOpponent.AddOnTakeDamageAction(UpdateComboMessage);
            _ownerOpponent.LandingScript.AddOnLandAction(ResetComboMessage);
        }

        private void UpdateComboMessage()
        {
            if (!_canCount)
                return;

            _comboText.enabled = true;
            _hitCount++;

            if (_nextComboMessageIndex < _comboMessages.Length && _hitCount >= _comboMessages[_nextComboMessageIndex].CountRequirement)
            {
                _currentComboMessage = _comboMessages[_nextComboMessageIndex].Message;
                _currentColor = _comboMessages[_nextComboMessageIndex].MessageColor;
                _nextComboMessageIndex++;
            }

            _comboText.color = _currentColor;
            _comboText.text = _hitCount + " Hits";
        }

        public void ResetComboMessage()
        {
            _canCount = false;
            _comboText.text = _currentComboMessage;
            _disableTextAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                _comboText.enabled = false;
                _currentComboMessage = _comboMessages[0].Message;
                _currentColor = _comboMessages[0].MessageColor;

            }, TimedActionCountType.SCALEDTIME, _messageDespawnDelay);

            _hitCount = 0;
            _nextComboMessageIndex = 0;
        }
    }
}
