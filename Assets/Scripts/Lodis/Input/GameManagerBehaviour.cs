using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEditor;


namespace Lodis.Gameplay
{
    public enum GameMode
    {
        SINGLEPLAYER,
        PRACTICE,
        MULTIPLAYER
    }

    public class GameManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputManager _inputManager;
        [SerializeField]
        private GameObject _playerRef;
        [SerializeField]
        private GridScripts.GridBehaviour _grid;
        [SerializeField]
        private GameMode _mode;
        private PlayerInput _player1;
        private PlayerInput _player2;
        private Movement.GridMovementBehaviour _p1Movement;
        private Movement.GridMovementBehaviour _p2Movement;
        private PlayerStateManagerBehaviour _p1StateManager;
        private PlayerStateManagerBehaviour _p2StateManager;
        private Input.InputBehaviour _p1Input;
        private Input.InputBehaviour _p2Input;
        [SerializeField]
        private HealthBarBehaviour _p1HealthBar;
        [SerializeField]
        private HealthBarBehaviour _p2HealthBar;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierL;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierR;
        [SerializeField]
        private AI.AttackDummyBehaviour _dummy;
        [SerializeField]
        private Vector2 _dummySpawnLocation;

        private void Awake()
        {
            _inputManager.playerPrefab = _playerRef;
        }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize grid
            _grid.CreateGrid();

            InputDevice[] devices = { Keyboard.current, Mouse.current };
            //Spawn player 1
            _inputManager.JoinPlayer(0, 0, "Player", devices);
            _player1 = PlayerInput.GetPlayerByIndex(0);
            _player1.name = _player1.name + "(P1)";
            _ringBarrierL.owner = _player1.name;

            //Get reference to player 1 components
            _p1Movement = _player1.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _player1.GetComponent<PlayerStateManagerBehaviour>();
            _p1Input = _player1.GetComponent<Input.InputBehaviour>();

            //Assign ID 
            _p1Input.PlayerID = 0;

            //Initialize base UI stats
            _p1HealthBar.HealthComponent = _player1.GetComponent<Movement.KnockbackBehaviour>();
            _p1HealthBar.MaxValue = 200;

            //Move player to spawn
            _p1Movement.Position = _grid.LhsSpawnPanel.Position;
            _p1Movement.Alignment = GridScripts.GridAlignment.LEFT;
            _player1.transform.forward = Vector3.right;

            //Spawns player 2 if the game mode is set to multiplayer
            if (_mode == GameMode.MULTIPLAYER)
            {
                //Spawn player 2
                _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[2]);
                _player2 = PlayerInput.GetPlayerByIndex(1);
                _player2.name = _player2.name + "(P2)";
                _ringBarrierR.owner = _player2.name;
                _player2.transform.forward = Vector3.left;

                //Get reference to player 2 components
                _p2Movement = _player2.GetComponent<Movement.GridMovementBehaviour>();
                _p2StateManager = _player2.GetComponent<PlayerStateManagerBehaviour>();
                _p2Input = _player2.GetComponent<Input.InputBehaviour>();

                //Initialize base UI stats
                _p2HealthBar.HealthComponent = _player2.GetComponent<Movement.KnockbackBehaviour>();
                _p2HealthBar.MaxValue = 200;

                //Move player to spawn
                _p2Input.PlayerID = 1;
                _p2Movement.Position = _grid.RhsSpawnPanel.Position;
                _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
                _grid.AssignOwners(_player1.name, _player2.name);
                return;
            }
            else if (_mode == GameMode.PRACTICE)
            {
                _inputManager.playerPrefab = _dummy.gameObject;

                _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[2]);
                _player2 = PlayerInput.GetPlayerByIndex(1);
                _player2.name = _player2.name + "(Dummy)";
                _ringBarrierR.owner = _player2.name;
                _player2.transform.forward = Vector3.left;

                //Get reference to player 2 components
                _p2Movement = _player2.GetComponent<Movement.GridMovementBehaviour>();
                _p2StateManager = _player2.GetComponent<PlayerStateManagerBehaviour>();
                _p2Input = _player2.GetComponent<Input.InputBehaviour>();

                //Initialize base UI stats
                _p2HealthBar.HealthComponent = _player2.GetComponent<Movement.KnockbackBehaviour>();
                _p2HealthBar.MaxValue = 200;

                //Move player to spawn
                _p2Input.PlayerID = 1;

                //Find spawn point for dummy
                GridScripts.PanelBehaviour spawnPanel = null;
                if (_grid.GetPanel(_dummySpawnLocation, out spawnPanel, false))
                    _p2Movement.Position = spawnPanel.Position;
                else
                    Debug.LogError("Invalid spawn point for dummy. Spawn was " + spawnPanel.Position);

                _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
                _grid.AssignOwners(_player1.name, _player2.name);
                return;
            }

            _grid.AssignOwners(_player1.name);
        }

        public void Restart()
        {
            SceneManager.LoadScene(0);
        }

        // Update is called once per frame
        void Update()
        {
            BlackBoardBehaviour.player1State = _p1StateManager.CurrentState;

            if (_mode == GameMode.MULTIPLAYER)
                BlackBoardBehaviour.player2State = _p2StateManager.CurrentState;
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(GameManagerBehaviour))]
    class GameManagerEditor : Editor
    {
        private GameManagerBehaviour _manager;

        private void Awake()
        {
            _manager = (GameManagerBehaviour)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Reset Game"))
            {
                _manager.Restart();
            }
        }
    }

#endif
}


