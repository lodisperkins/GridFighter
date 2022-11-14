using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.UI
{
    public class UIColorManagerBehaviour : MonoBehaviour
    {
        private int _player;
        [SerializeField]
        private ColorVariable _p1Color;
        [SerializeField]
        private ColorVariable _p2Color;

        public void SetPlayer(int playerNum)
        {
            _player = playerNum;
        }

        public void SetPlayerColor(string hexCode)
        {
            if (_player == 1)
                _p1Color.SetColor(hexCode);
            else if (_player == 2)
                _p2Color.SetColor(hexCode);
        }
    }
}
