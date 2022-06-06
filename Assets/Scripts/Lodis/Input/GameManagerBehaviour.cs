using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;
using Lodis.Utility;
using Lodis.Input;

namespace Lodis.Gameplay
{
    public enum GameMode
    {
        SINGLEPLAYER,
        PRACTICE,
        MULTIPLAYER,
        SIMULATE
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
        private GameObject _player1;
        private GameObject _player2;
        private Movement.GridMovementBehaviour _p1Movement;
        private Movement.GridMovementBehaviour _p2Movement;
        private CharacterStateMachineBehaviour _p1StateManager;
        private CharacterStateMachineBehaviour _p2StateManager;
        private IControllable _p1Input;
        private IControllable _p2Input;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierL;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierR;
        [SerializeField]
        private AI.AttackDummyBehaviour _dummy;
        [SerializeField]
        private Vector2 _dummyRHSSpawnLocation;
        [SerializeField]
        private Vector2 _dummyLHSSpawnLocation;
        [SerializeField]
        private int _targetFrameRate;
        [SerializeField]
        private bool _invincibleBarriers;
        [SerializeField]
        private float _timeScale = 1;
        private bool _p1DeviceSet;
        private bool _p2DeviceSet;

        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
        }


        private void Awake()
        {
            _inputManager.playerPrefab = _playerRef;
            _grid.DestroyTempPanels();
            _grid.InvincibleBarriers = _invincibleBarriers;
            //Initialize grid
            _grid.CreateGrid();
            Application.targetFrameRate = _targetFrameRate;

            SpawnEntitiesByMode();
        }

        private void SpawnEntitiesByMode()
        {
            SpawnPlayer1();

            if (_mode != GameMode.SINGLEPLAYER)
            {
                SpawnPlayer2();
                _grid.AssignOwners(_player1.name, _player2.name);
            }
            else
                _grid.AssignOwners(_player1.name);
        }

        private void SpawnPlayer2()
        {
            //Spawn player 2
            if (_mode != GameMode.MULTIPLAYER)
                _player2 = Instantiate(_dummy.gameObject);
            else
                _player2 = _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[0]).gameObject;

            _player2.name += "(P2)";
            _ringBarrierR.Owner = _player2;
            _player2.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _player2.gameObject;
            //Get reference to player 2 components
            _p2Movement = _player2.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _player2.GetComponent<CharacterStateMachineBehaviour>();
            _p2Input = _player2.GetComponent<IControllable>();
            _p2Input.PlayerID = BlackBoardBehaviour.Instance.Player2ID;


            GridScripts.PanelBehaviour spawnPanel = null;
            if (_grid.GetPanel(_dummyRHSSpawnLocation, out spawnPanel, false))
                _p2Movement.MoveToPanel(spawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for player 2. Spawn was " + _grid.RhsSpawnPanel);

            _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
        }

        private void SpawnPlayer1()
        {
            //Spawn player 2
            if (_mode == GameMode.SIMULATE)
                _player1 = Instantiate(_dummy.gameObject);
            else
                _player1 = _inputManager.JoinPlayer(0, 0, "Player", InputSystem.devices[0]).gameObject;

            _player1.name += "(P1)";
            _ringBarrierL.Owner = _player1;
            _player1.transform.forward = Vector3.right;
            BlackBoardBehaviour.Instance.Player1 = _player1.gameObject;
            //Get reference to player 2 components
            _p1Movement = _player1.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _player1.GetComponent<CharacterStateMachineBehaviour>();
            _p1Input = _player1.GetComponent<IControllable>();
            _p1Input.PlayerID = BlackBoardBehaviour.Instance.Player1ID;


            GridScripts.PanelBehaviour spawnPanel = null;
            if (_grid.GetPanel(_dummyLHSSpawnLocation, out spawnPanel, false))
                _p1Movement.MoveToPanel(spawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for player 1. Spawn was " + _grid.LhsSpawnPanel);

            _p1Movement.Alignment = GridScripts.GridAlignment.LEFT;
        }

        public void Restart()
        {
            SceneManager.LoadScene(0);
        }

        private bool DeviceInputReceived(out InputDevice device)
        {
            device = null;
            InputBehaviour p1Input = (InputBehaviour)_p1Input;

            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                if (InputSystem.devices[i].IsPressed() && !InputSystem.devices[i].name.Contains("Mouse")
                    && !p1Input.Devices.Contains(InputSystem.devices[i]))
                {
                    if (_mode == GameMode.MULTIPLAYER)
                    { 
                        InputBehaviour p2Input = (InputBehaviour)_p2Input;
                        if (p2Input?.Devices.Contains(InputSystem.devices[i]) == true)
                            continue;
                    }

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
                playerInput = (InputBehaviour)_p1Input;
            else playerInput = (InputBehaviour)_p2Input;

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
            
            if (_mode == GameMode.SIMULATE)
                return;

            if (!_p1DeviceSet)
            {
                InputBehaviour p1Input = (InputBehaviour)_p1Input;

                InputDevice device;
                if (!DeviceInputReceived(out device))
                    return;
                Debug.Log("Input Received P1 " + device.name);

                if (_mode == GameMode.MULTIPLAYER)
                {
                    InputBehaviour p2Input = (InputBehaviour)_p2Input;

                    if (p2Input.Devices.Find(args => args.deviceId == device.deviceId) != null)
                        return;
                }

                if (!device.name.Contains("Mouse"))
                {
                    Debug.Log("Input Assigned P1 " + device.name);
                    AssignDevice(device, 1);
                    p1Input.enabled = true;
                    _p1DeviceSet = true;
                }
            }

            if (_mode == GameMode.MULTIPLAYER && !_p2DeviceSet)
            {
                BlackBoardBehaviour.Instance.Player2State = _p2StateManager.StateMachine.CurrentState;
                InputBehaviour p1Input = (InputBehaviour)_p1Input;
                InputBehaviour p2Input = (InputBehaviour)_p2Input;

                InputDevice device = null;
                if (!DeviceInputReceived(out device))
                    return;
                Debug.Log("Input Received P2 " + device.name);
                if (p1Input.Devices.Find(args => args.deviceId == device.deviceId) == null && !device.name.Contains("Mouse"))
                {
                    Debug.Log("Input Assigned P2 " + device.name);
                    AssignDevice(device, 2);
                    p2Input.enabled = true;
                    _p2DeviceSet = true;
                }
            }

            Time.timeScale = _timeScale;
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


