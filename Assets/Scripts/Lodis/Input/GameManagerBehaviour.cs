using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;
using Lodis.Utility;

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
        private GameObject _cpu;
        private Movement.GridMovementBehaviour _p1Movement;
        private Movement.GridMovementBehaviour _p2Movement;
        private CharacterStateMachineBehaviour _p1StateManager;
        private CharacterStateMachineBehaviour _p2StateManager;
        private Input.InputBehaviour _p1Input;
        private Input.InputBehaviour _p2Input;
        private MovesetBehaviour _player2Moveset;
        private MovesetBehaviour _player1Moveset;
        [SerializeField]
        private HealthBarBehaviour _p1HealthBar;
        [SerializeField]
        private HealthBarBehaviour _p2HealthBar;
        [SerializeField]
        private AbilityDebugTextBehaviour _abilityTextP1;
        [SerializeField]
        private AbilityDebugTextBehaviour _abilityTextP2;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierL;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierR;
        [SerializeField]
        private AI.AttackDummyBehaviour _dummy;
        [SerializeField]
        private Vector2 _dummySpawnLocation;
        [SerializeField]
        private int _targetFrameRate;
        private int _player1DeviceIndex;
        
        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
        }


        private void Awake()
        {
            _inputManager.playerPrefab = _playerRef;
            _grid.DestroyTempPanels();
            //Initialize grid
            _grid.CreateGrid();
            Application.targetFrameRate = _targetFrameRate;
        }

        // Start is called before the first frame update
        void Start()
        {
            switch (_mode)
            {
                case GameMode.SINGLEPLAYER:
                    SpawnPlayer1();
                    _grid.AssignOwners(_player1.name);
                    break;

                case GameMode.PRACTICE:
                    SpawnPlayer1();
                    SpawnCPU();
                    _grid.AssignOwners(_player1.name, _cpu.name);
                    break;

                case GameMode.MULTIPLAYER:
                    SpawnPlayer1();
                    SpawnPlayer2();
                    _grid.AssignOwners(_player1.name, _player2.name);
                    break;

            }
        }

        private void SpawnCPU()
        {
            _inputManager.playerPrefab = _dummy.gameObject;

            _cpu = Instantiate(_inputManager.playerPrefab);
            _cpu.name = _inputManager.playerPrefab.name + "(Dummy)";
            _ringBarrierR.owner = _cpu.name;
            _cpu.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _cpu.gameObject;
            //Get reference to player 2 components
            _p2Movement = _cpu.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _cpu.GetComponent<CharacterStateMachineBehaviour>();
            _player2Moveset = _cpu.GetComponent<MovesetBehaviour>();

            //Initialize base UI stats
            _p2HealthBar.HealthComponent = _cpu.GetComponent<Movement.KnockbackBehaviour>();
            _p2HealthBar.MaxValue = 200;
            _abilityTextP2.MoveSet = _player2Moveset;

            //Find spawn point for dummy
            GridScripts.PanelBehaviour spawnPanel = null;
            if (_grid.GetPanel(_dummySpawnLocation, out spawnPanel, false))
                _p2Movement.MoveToPanel(spawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for dummy. Spawn was " + _dummySpawnLocation);

            _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
        }

        private void SpawnPlayer2()
        {
            //Spawn player 2
            _player2 = _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[0]);
            _player2.name += "(P2)";
            _ringBarrierR.owner = _player2.name;
            _player2.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _player2.gameObject;
            //Get reference to player 2 components
            _p2Movement = _player2.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _player2.GetComponent<CharacterStateMachineBehaviour>();
            _p2Input = _player2.GetComponent<Input.InputBehaviour>();
            _player2Moveset = _player2.GetComponent<MovesetBehaviour>();

            //Initialize base UI stats
            _p2HealthBar.HealthComponent = _player2.GetComponent<Movement.KnockbackBehaviour>();
            _p2HealthBar.MaxValue = 200;
            _abilityTextP2.MoveSet = _player2Moveset;

            //Move player to spawn
            _p2Input.PlayerID = 1;
            _p2Movement.MoveToPanel(_grid.RhsSpawnPanel, true, GridScripts.GridAlignment.ANY);
            _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
            _grid.AssignOwners(_player1.name, _player2.name);
        }

        private void SpawnPlayer1()
        {
            //Spawn player 1
            //if (InputSystem.devices.Count <= 2 || (InputSystem.devices.Count == 3 && _mode == GameMode.MULTIPLAYER))
            //{
            //    InputDevice[] devices = { InputSystem.devices[0], InputSystem.devices[1] };
            //    _inputManager.JoinPlayer(0, 0, "Player", devices);
            //    _player1DeviceIndex = 1;
            //}
            //else if (InputSystem.devices.Count > 2)
            //{ 
            //    _inputManager.JoinPlayer(0, 0, "Player", InputSystem.devices[2]);
            //    _player1DeviceIndex = 2;
            //}


            _inputManager.JoinPlayer(0, 0, "Player");

            _player1 = PlayerInput.GetPlayerByIndex(0);
            _player1.name = _player1.name + "(P1)";
            _ringBarrierL.owner = _player1.name;

            //Get reference to player 1 components
            _p1Movement = _player1.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _player1.GetComponent<CharacterStateMachineBehaviour>();
            _p1Input = _player1.GetComponent<Input.InputBehaviour>();
            _player1Moveset = _player1.GetComponent<MovesetBehaviour>();
            BlackBoardBehaviour.Instance.Player1 = _player1.gameObject;
            //Assign ID 
            _p1Input.PlayerID = 0;

            //Initialize base UI stats
            _p1HealthBar.HealthComponent = _player1.GetComponent<Movement.KnockbackBehaviour>();
            _p1HealthBar.MaxValue = 200;
            _abilityTextP1.MoveSet = _player1Moveset;

            //Move player to spawn
            _p1Movement.MoveToPanel(_grid.LhsSpawnPanel, true, GridScripts.GridAlignment.ANY);
            _p1Movement.Alignment = GridScripts.GridAlignment.LEFT;
            _player1.transform.forward = Vector3.right;
        }

        public void Restart()
        {
            SceneManager.LoadScene(0);
        }

        private bool DeviceInputReceived(out InputDevice device)
        {
            device = null;

            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                if (InputSystem.devices[i].IsPressed())
                {
                    device = InputSystem.devices[i];
                    return true;
                }
            }

            return false;
        }

        private void AssignDevice(InputDevice device, int player)
        {
            Input.InputBehaviour playerInput = null;
            if (player == 1)
                playerInput = _p1Input;
            else playerInput = _p2Input;

            //If input was detected by a keyboard or mouse...
            if (device.name.Contains("Mouse") || device.name.Contains("Keyboard"))
            {
                //...set the input device array to be the keyboard and mouse
                playerInput.Devices = new List<InputDevice>(InputSystem.devices.GetDevices(args =>
                {
                    InputDevice input = (InputDevice)args[0];
                    return input.name.Contains("Mouse") || input.name.Contains("Keyboard");
                }
                ));
                return;
            }

            playerInput.Devices = new List<InputDevice>() { device };
        }

        // Update is called once per frame
        void Update()
        {
            BlackBoardBehaviour.Instance.Player1State = _p1StateManager.StateMachine.CurrentState;
            
            if (_p1Input.Devices.Count == 0)
            {
                InputDevice device;
                if (DeviceInputReceived(out device))
                    AssignDevice(device, 1);
            }

            if (_mode == GameMode.MULTIPLAYER)
            { 
                BlackBoardBehaviour.Instance.Player2State = _p2StateManager.StateMachine.CurrentState;

                if (_p2Input.Devices.Count != 0)
                    return;

                InputDevice device;
                if (DeviceInputReceived(out device) && !_p1Input.Devices.Contains(device))
                    AssignDevice(device, 2);
            }

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


