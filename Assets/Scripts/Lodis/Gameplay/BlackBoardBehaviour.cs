using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.UI;
using Lodis.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    public class BlackBoardBehaviour : MonoBehaviour
    {
        public List<Color> AbilityCostColors;
        public ColorVariable Player1Color;
        public ColorVariable Player2Color;
        public float LHSTotalDamage;
        public float RHSTotalDamage;
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
        public ParticleSystem ReflectEffect;
        public ParticleSystem ClashEffect;
        public ParticleSystem[] HitEffects;
        public ComboCounterBehaviour Player1ComboCounter;
        public ComboCounterBehaviour Player2ComboCounter;
        private List<GridMovementBehaviour> _entitiesInGame = new List<GridMovementBehaviour>();
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
        public List<GridMovementBehaviour> GetEntitiesInGame()
        {
            _entitiesInGame.RemoveAll(entity => entity == null || (!entity.gameObject.activeInHierarchy && !entity.CompareTag("Player")));
            return _entitiesInGame;
        }

        /// <summary>
        /// Removes all abilties and entites from the grid
        /// </summary>
        public void ClearGrid()
        {
            DisableAllAbilityColliders();
            DisableAllNonPlayerEntities();
            Grid.CancelRowExchange();
        }

        /// <summary>
        /// Destroys all entities currently in the scene with the exception of the players
        /// </summary>
        public void DisableAllNonPlayerEntities()
        {
            foreach (GridMovementBehaviour entity in _entitiesInGame)
            {
                if (!entity.CompareTag("Player"))
                    ObjectPoolBehaviour.Instance.ReturnGameObject(entity.gameObject);
            }
        }

        /// <summary>
        /// Gets all ability colliders that are aligned with the left side
        /// </summary>
        /// <returns>A list of hit colliders</returns>
        public List<HitColliderBehaviour> GetLHSActiveColliders()
        {
            if (_lhsActiveColliders.Count > 0)
                _lhsActiveColliders.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return !hitCollider.gameObject.activeInHierarchy;

                    return true;
                });

            return _lhsActiveColliders;
        }

        /// <summary>
        /// Gets all ability colliders that are aligned with the right side
        /// </summary>
        /// <returns>A list of hit colliders</returns>
        public List<HitColliderBehaviour> GetRHSActiveColliders()
        {
            if (_rhsActiveColliders.Count > 0)
                _rhsActiveColliders.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return !hitCollider.gameObject.activeInHierarchy;

                    return true;
                });

            return _rhsActiveColliders;
        }

        public List<HitColliderBehaviour> GetActiveColliders(GridScripts.GridAlignment alignment)
        {
            if (alignment == GridScripts.GridAlignment.LEFT)
                return GetLHSActiveColliders();
            else if (alignment == GridScripts.GridAlignment.RIGHT)
                return GetRHSActiveColliders();

            return null;
        }

        /// <summary>
        /// Returns all ability colliders to the object pool
        /// </summary>
        public void DisableAllAbilityColliders()
        {
            foreach (HitColliderBehaviour collider in _lhsActiveColliders)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(collider.gameObject, Time.deltaTime);
            }

            foreach (HitColliderBehaviour collider in _rhsActiveColliders)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(collider.gameObject, Time.deltaTime);
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
        public IControllable GetPlayerControllerFromID(IntVariable id)
        {
            if (!id) return null;

            if (id.Value == 1)
                return Player1Controller;
            else if(id.Value == 2)
                return Player2Controller;

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
        /// Gets the state of the player that matches the ID
        /// </summary>
        /// <param name="id">The player's ID</param>
        /// <returns></returns>
        public GameObject GetPlayerFromID(int id)
        {
            if (id == 1)
                return Player1;
            else if(id == 2)
                return Player2;

            return null;
        }

        public int GetIDFromPlayer(GameObject player)
        {
            if (Player1 == player)
                return Player1ID;
            else if (Player2 == player)
                return Player2ID;

            return 0;
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
        /// Adds the game object to the list of significant actors on the screen
        /// </summary>
        /// <param name="entity"></param>
        public void AddEntityToList(Movement.GridMovementBehaviour entity)
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

        /// <summary>
        /// Gets the color of the players alignment
        /// </summary>
        /// <param name="id">A scriptable object storing the players ID</param>
        public Color GetPlayerColorByID(IntVariable id)
        {
            if (!id) return Color.black;

            if (id.Value == 1)
                return Player1Color;
            else if (id.Value == 2)
                return Player2Color;

            return Color.black;
        }

        /// <summary>
        /// Gets the color of the player alignment
        /// </summary>
        /// <param name="alignment">The alignment to get the color for</param>
        public Color GetPlayerColorByAlignment(GridScripts.GridAlignment alignment)
        {
            if (alignment == GridScripts.GridAlignment.ANY)
                return Player2Color.Value + Player1Color.Value;

            if (alignment == GridScripts.GridAlignment.LEFT)
                return Player1Color;
            else if (alignment == GridScripts.GridAlignment.RIGHT)
                return Player2Color;

            return Color.black;
        }
    }
}

