using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.UI
{
    public class PlayerColorManagerBehaviour : MonoBehaviour
    {
        private int _player;
        [SerializeField]
        private ColorVariable _p1Color;
        [SerializeField]
        private ColorVariable _p2Color;
        [SerializeField]
        private Color[] _possibleColors;
        [SerializeField]
        private CustomEventSystem.Event _setColorEvent;
        [SerializeField]
        private UnityEvent _onSetColor;

        public Color[] PossibleColors { get => _possibleColors; private set => _possibleColors = value; }
        public ColorVariable P1Color { get => _p1Color; private set => _p1Color = value; }
        public ColorVariable P2Color { get => _p2Color; private set => _p2Color = value; }

        public void SetPlayer(int playerNum)
        {
            _player = playerNum;
        }

        public void SetPlayerColor(string hexCode)
        {
            if (_player == 1)
                P1Color.SetColor(hexCode);
            else if (_player == 2)
                P2Color.SetColor(hexCode);

            _setColorEvent?.Raise(gameObject);
            _onSetColor?.Invoke();
        }

        public void SetPlayerColor(int player, int index)
        {
            if (player == 1)
                P1Color.Value = PossibleColors[index];
            else if (player == 2)
                P2Color.Value = PossibleColors[index];

            _setColorEvent?.Raise(gameObject);
            _onSetColor?.Invoke();
        }
    }
}
