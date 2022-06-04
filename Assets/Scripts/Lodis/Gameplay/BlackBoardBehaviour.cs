using Lodis.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public GridScripts.GridBehaviour Grid { get; private set; }
        public string Player1State = null;
        public string Player2State = null;
        public FloatVariable MaxKnockBackHealth;
        public GameObject Player1;
        public GameObject Player2;
        private List<GameObject> _entitiesInGame = new List<GameObject>();
        private List<HitColliderBehaviour> _lhsActiveColliders = new List<HitColliderBehaviour>();
        private List<HitColliderBehaviour> _rhsActiveColliders = new List<HitColliderBehaviour>();
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

        public List<HitColliderBehaviour> GetLHSActiveColliders()
        {
            if (_lhsActiveColliders.Count > 0)
                _lhsActiveColliders.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return hitCollider == null;

                    return true;
                });

            return _lhsActiveColliders;
        }
        public List<HitColliderBehaviour> GetRHSActiveColliders()
        {
            if (_rhsActiveColliders.Count > 0)
                _rhsActiveColliders.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return hitCollider == null;

                    return true;
                });

            return _rhsActiveColliders;
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

        /// <summary>
        /// Gets the opponent of the given player.
        /// </summary>
        public GameObject GetOpponentForPlayer(GameObject player)
        {
            int id = player.GetComponent<Input.InputBehaviour>().PlayerID;

            if (id == 1)
                return Player2;

            return Player1;
        }
    }
}

