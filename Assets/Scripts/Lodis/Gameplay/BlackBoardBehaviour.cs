using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public GridScripts.GridBehaviour Grid { get; private set; }
        public PlayerState player1State = PlayerState.IDLE;
        public PlayerState player2State = PlayerState.IDLE;
        public float projectileHeight;
        public FloatVariable MaxKnockBackHealth;
        public GameObject Player1;
        public GameObject Player2;
        private List<GameObject> _entitiesInGame = new List<GameObject>();
        private static BlackBoardBehaviour _instance;

        public static BlackBoardBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(BlackBoardBehaviour)) as BlackBoardBehaviour;

                if (!_instance)
                {
                    GameObject blackBoard = new GameObject("BlackBoard");
                    _instance = blackBoard.AddComponent<BlackBoardBehaviour>();
                }

                return _instance;
            }
        }

        public List<GameObject> EntitiesInGame { get => _entitiesInGame; }

        /// <summary>
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public PlayerState GetPlayerStateFromID(int id)
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

        public void InitializeGrid()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }

        public void AddEntityToList(GameObject entity)
        {
            _entitiesInGame.Add(entity);
        }
    }
}

