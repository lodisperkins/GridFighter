using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public enum PlayerState
    {
        ACTIVE,
        KNOCKBACK,
        DOWN
    }



    public class BlackBoardBehaviour : MonoBehaviour
    {
        public static GridScripts.GridBehaviour Grid { get; private set; }
        public static PlayerState player1State = PlayerState.ACTIVE;
        public static PlayerState player2State = PlayerState.ACTIVE;

        private void Awake()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }
    }
}

