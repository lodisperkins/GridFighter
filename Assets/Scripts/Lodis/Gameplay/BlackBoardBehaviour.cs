using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public GridScripts.GridBehaviour Grid { get; private set; }
        public string Player1State = null;
        public string Player2State = null;
        public float ProjectileHeight;
        public FloatVariable MaxKnockBackHealth;
        public GameObject Player1;
        public GameObject Player2;
        private List<GameObject> _entitiesInGame = new List<GameObject>();
        private static BlackBoardBehaviour _instance;

        /// <summary>
        /// Gets the static instance of the black board. Creates one if none exists
        /// </summary>
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

        /// <summary>
        /// Removes all null enemies from the entities list and returns the new list
        /// </summary>
        /// <returns>The list of in game entities</returns>
        public List<GameObject> GetEntitiesInGame()
        {
            _entitiesInGame.RemoveAll(entity => entity == null);
            return _entitiesInGame;
        }

        /// <summary>
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public string GetPlayerStateFromID(int id)
        {
            if (id == 0)
                return Player1State;
            else if(id == 1)
                return Player2State;

            return null;
        }

        private void Awake()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }

        /// <summary>
        /// Finds the grid in the scene to initialize the grid property
        /// </summary>
        public void InitializeGrid()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }

        /// <summary>
        /// Adds the game object to the list of significant characters on the screen
        /// </summary>
        /// <param name="entity"></param>
        public void AddEntityToList(GameObject entity)
        {
            _entitiesInGame.Add(entity);
        }
    }
}

