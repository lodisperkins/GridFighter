using Lodis.Input;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public List<Color> AbilityCostColors;
        public Color Player1Color;
        public Color Player2Color;
        public GridScripts.GridBehaviour Grid { get; private set; }
        public string Player1State = null;
        public string Player2State = null;
        public FloatVariable MaxKnockBackHealth;
        public GameObject Player1;
        public GameObject Player2;
        public IControllable Player1Controller;
        public IControllable Player2Controller;
        public IntVariable Player1ID;
        public IntVariable Player2ID;
        public RingBarrierBehaviour RingBarrierRHS;
        public RingBarrierBehaviour RingBarrierLHS;
        public ParticleSystem BlockEffect;
        public ParticleSystem[] HitEffects;
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

        public void ClearGrid()
        {
            DestroyAllAbilityColliders();
            DestroyAllNonPlayerEntities();
        }

        public void DestroyAllNonPlayerEntities()
        {
            foreach (GameObject entity in _entitiesInGame)
            {
                if (!entity.CompareTag("Player"))
                    Destroy(entity);
            }
        }

        public List<HitColliderBehaviour> GetLHSActiveColliders()
        {
            if (_lhsActiveColliders.Count > 0)
                _lhsActiveColliders.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return hitCollider.gameObject.activeInHierarchy;

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
                        return hitCollider.gameObject.activeInHierarchy;

                    return true;
                });

            return _rhsActiveColliders;
        }

        public void DestroyAllAbilityColliders()
        {
            foreach (HitColliderBehaviour collider in GetLHSActiveColliders())
            {
                if (collider.transform.root.CompareTag("Player"))
                    ObjectPoolBehaviour.Instance.ReturnGameObject(collider.gameObject);
                else
                    ObjectPoolBehaviour.Instance.ReturnGameObject(collider.transform.root.gameObject);
            }

            foreach (HitColliderBehaviour collider in GetRHSActiveColliders())
            {
                if (collider.transform.root.CompareTag("Player"))
                    ObjectPoolBehaviour.Instance.ReturnGameObject(collider.gameObject);
                else
                    ObjectPoolBehaviour.Instance.ReturnGameObject(collider.transform.root.gameObject);
            }
        }

        /// <summary>
        /// Gets the state of the player passed in
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public string GetPlayerState(GameObject player)
        {
            if (player == Player1)
                return Player1State;
            else if (player == Player2)
                return Player2State;

            return null;
        }

        /// <summary>
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public string GetPlayerStateFromID(IntVariable id)
        {
            if (id.Value == 1)
                return Player1State;
            else if(id.Value == 2)
                return Player2State;

            return null;
        }
        /// <summary>
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public GameObject GetPlayerFromID(IntVariable id)
        {
            if (!id) return null;

            if (id.Value == 1)
                return Player1;
            else if(id.Value == 2)
                return Player2;

            return null;
        }

        /// <summary>
        /// Gets a reference to the player that is aligned with the given side
        /// </summary>
        /// <param name="alignment">The player's grid alignment.</param>
        /// <returns></returns>
        public GameObject GetPlayerFromAlignment(GridScripts.GridAlignment alignment)
        {
            if (alignment == GridScripts.GridAlignment.LEFT)
                return Player1;
            else if(alignment == GridScripts.GridAlignment.RIGHT)
                return Player2;

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
        public GameObject GetOpponentForPlayer(IntVariable id)
        {
            if (!id) return null;

            if (id.Value == 1)
                return Player2;
            else if (id.Value == 2)
                return Player1;

            return null;
        }

        /// <summary>
        /// Gets the opponent of the given player.
        /// </summary>
        public GameObject GetOpponentForPlayer(GameObject player)
        {
            if (player == Player1)
                return Player2;
            else if (player == Player2)
                return Player1;

            return null;
        }

        public Color GetPlayerColorByID(IntVariable id)
        {
            if (!id) return Color.black;

            if (id.Value == 1)
                return Player1Color;
            else if (id.Value == 2)
                return Player2Color;

            return Color.black;
        }

        public Color GetPlayerColorByAlignment(GridScripts.GridAlignment alignment)
        {
            if (alignment == GridScripts.GridAlignment.ANY)
                return Color.white;

            if (alignment == GridScripts.GridAlignment.LEFT)
                return Player1Color;
            else if (alignment == GridScripts.GridAlignment.RIGHT)
                return Player2Color;

            return Color.black;
        }
    }
}

