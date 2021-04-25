using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public static GridScripts.GridBehaviour Grid { get; private set; }
        public static PlayerState player1State = PlayerState.IDLE;
        public static PlayerState player2State = PlayerState.IDLE;

        /// <summary>
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public static PlayerState GetPlayerStateFromID(int id)
        {
            if (id == 0)
                return player1State;
            else if(id == 1)
                return player2State;

            return PlayerState.IDLE;
        }

        private void Awake()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }

        public static void InitializeGrid()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }
    }
}

