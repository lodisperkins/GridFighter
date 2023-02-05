using Lodis.AI;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Gameplay
{
    public class PlayerSpawnBehaviour : MonoBehaviour
    {
        private bool _p1DeviceSet;
        private bool _p2DeviceSet;
        private GameObject _player1;
        private GameObject _player2;
        private Movement.GridMovementBehaviour _p1Movement;
        private Movement.GridMovementBehaviour _p2Movement;
        private CharacterStateMachineBehaviour _p1StateManager;
        private KnockbackBehaviour _p1Knockback;
        private CharacterStateMachineBehaviour _p2StateManager;
        private KnockbackBehaviour _p2Knockback;
        private GameMode _mode;
        [SerializeField]
        private AI.AttackDummyBehaviour _dummy;
        [SerializeField]
        private Vector2 _RHSSpawnLocation;
        [SerializeField]
        private Vector2 _LHSSpawnLocation;
        [SerializeField]
        private PlayerInputManager _inputManager;
        [SerializeField]
        private GameObject _playerRef;
        [SerializeField]
        private GameObject _player1CharacterRef;
        [SerializeField]
        private GameObject _player2CharacterRef;
        private IControllable _p1Input;
        private IControllable _p2Input;
        private RingBarrierBehaviour _ringBarrierR;
        private RingBarrierBehaviour _ringBarrierL;
        private GridBehaviour _grid;
        private PanelBehaviour _lhsSpawnPanel;
        private PanelBehaviour _rhsSpawnPanel;
        private bool _suddenDeathActive;

        public Vector2 RHSSpawnLocation { get => _RHSSpawnLocation; private set => _RHSSpawnLocation = value; }
        public Vector2 LHSSpawnLocation { get => _LHSSpawnLocation; private set => _LHSSpawnLocation = value; }
        public KnockbackBehaviour P1HealthScript { get => _p1Knockback; }
        public KnockbackBehaviour P2HealthScript { get => _p2Knockback; }
        public bool SuddenDeathActive { get => _suddenDeathActive; set => _suddenDeathActive = value; }

        private void Awake()
        {
            _ringBarrierL = BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ringBarrierR = BlackBoardBehaviour.Instance.RingBarrierRHS;
            _inputManager.playerPrefab = _playerRef;
        }

        public void SpawnEntitiesByMode(GameMode gameMode)
        {
            _grid = BlackBoardBehaviour.Instance.Grid;
            _mode = gameMode;
            SpawnPlayer1();

            if (_mode != GameMode.SINGLEPLAYER)
            {
                SpawnPlayer2();
                BlackBoardBehaviour.Instance.Grid.AssignOwners(_player1.name, _player2.name);
                LoadAIDecisions();
            }
            else
                BlackBoardBehaviour.Instance.Grid.AssignOwners(_player1.name);
        }

        public void SpawnPlayer2()
        {
            //Spawn player 2
            if (_mode != GameMode.MULTIPLAYER)
                _player2 = Instantiate(_dummy.gameObject);
            else
                _player2 = _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[0]).gameObject;

            _p2Input = _player2.GetComponent<IControllable>();

            _p2Input.Character = Instantiate(_player2CharacterRef, _player2.transform);

            _p2Input.Character.name += "(P2)";
            _ringBarrierR.Owner = _p2Input.Character;
            _player2.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _p2Input.Character;
            //Get reference to player 2 components
            _p2Movement = _p2Input.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _p2Input.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p2Knockback = _p2Input.Character.GetComponent<KnockbackBehaviour>();
            _p2Input.PlayerID = BlackBoardBehaviour.Instance.Player2ID;

            BlackBoardBehaviour.Instance.Player2Controller = _p2Input;

            if (_grid.GetPanel(RHSSpawnLocation, out _rhsSpawnPanel, false))
                _p2Movement.MoveToPanel(_rhsSpawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for player 2. Spawn was " + RHSSpawnLocation);

            _p2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
        }

        public void SpawnPlayer1()
        {
            //Spawn player 2
            if (_mode == GameMode.SIMULATE)
                _player1 = Instantiate(_dummy.gameObject);
            else
                _player1 = _inputManager.JoinPlayer(0, 0, "Player", InputSystem.devices[0]).gameObject;

            _p1Input = _player1.GetComponent<IControllable>();

            _p1Input.Character = Instantiate(_player1CharacterRef, _player1.transform);

            _p1Input.Character.name += "(P1)";
            _ringBarrierL.Owner = _p1Input.Character;
            _player1.transform.forward = Vector3.right;
            BlackBoardBehaviour.Instance.Player1 = _p1Input.Character;
            //Get reference to player 2 components
            _p1Movement = _p1Input.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _p1Input.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p1Knockback = _p1Input.Character.GetComponent<KnockbackBehaviour>();
            _p1Input.PlayerID = BlackBoardBehaviour.Instance.Player1ID;

            BlackBoardBehaviour.Instance.Player1Controller = _p1Input;

            if (_grid.GetPanel(LHSSpawnLocation, out _lhsSpawnPanel, false))
                _p1Movement.MoveToPanel(_lhsSpawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for player 1. Spawn was " + LHSSpawnLocation);

            _p1Movement.Alignment = GridScripts.GridAlignment.LEFT;
        }

        public void ResetPlayers()
        {
            //Reset the health for the players
            //Player1
            _p1Knockback.LandingScript.CanCheckLanding = false;
            _p1Knockback.ResetHealth();
            //Player2
            _p2Knockback.LandingScript.CanCheckLanding = false;
            _p2Knockback.ResetHealth();

            //Reset the position for the players
            //Player 1
            GridMovementBehaviour movement = _p1Input.Character.GetComponent<GridMovementBehaviour>();
            movement.CancelMovement();
            movement.EnableMovement();
            movement.CanMoveDiagonally = true;
            movement.MoveToPanel(LHSSpawnLocation, true);
            movement.CanMoveDiagonally = false;

            InputBehaviour input = _player1.GetComponent<InputBehaviour>();
            if (input)
                input.ClearBuffer();

            //Player 2
            movement = _p2Input.Character.GetComponent<GridMovementBehaviour>();
            movement.CancelMovement();
            movement.EnableMovement();
            movement.CanMoveDiagonally = true;
            movement.MoveToPanel(RHSSpawnLocation, true);
            movement.CanMoveDiagonally = false;

            input = _player2.GetComponent<InputBehaviour>();
            if (input)
                input.ClearBuffer();

            MovesetBehaviour moveset = _p1Input.Character.GetComponent<MovesetBehaviour>();
            moveset.ResetAll();

            moveset = _p2Input.Character.GetComponent<MovesetBehaviour>();
            moveset.ResetAll();

            //Enable both players in case either are inactive
            _p1Input.Character.SetActive(true);
            _p2Input.Character.SetActive(true);
        }

        private void LoadAIDecisions()
        {
            if (_mode != GameMode.PRACTICE && _mode != GameMode.SIMULATE)
                return;

            AttackDummyBehaviour dummyController = BlackBoardBehaviour.Instance.Player2Controller as AttackDummyBehaviour;
            dummyController.LoadDecisions();

            if (_mode == GameMode.SIMULATE)
            {
                dummyController = BlackBoardBehaviour.Instance.Player1Controller as AttackDummyBehaviour;
                dummyController.LoadDecisions();
            }
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

        private void Update()
        {
            BlackBoardBehaviour.Instance.Player1State = _p1StateManager.StateMachine.CurrentState;


            if (_mode != GameMode.SINGLEPLAYER)
                BlackBoardBehaviour.Instance.Player2State = _p2StateManager?.StateMachine?.CurrentState;

            if (_mode == GameMode.SIMULATE)
                return;

            if (!_p1DeviceSet)
            {
                InputBehaviour p1Input = (InputBehaviour)_p1Input;

                InputDevice device;
                if (!DeviceInputReceived(out device))
                    return;
                //Debug.Log("Input Received P1 " + device.name);

                if (_mode == GameMode.MULTIPLAYER)
                {
                    InputBehaviour p2Input = (InputBehaviour)_p2Input;

                    if (p2Input.Devices.Find(args => args.deviceId == device.deviceId) != null)
                        return;
                }

                if (!device.name.Contains("Mouse"))
                {
                    //Debug.Log("Input Assigned P1 " + device.name);
                    AssignDevice(device, 1);
                    p1Input.enabled = true;
                    _p1DeviceSet = true;
                }
            }

            if (_mode == GameMode.MULTIPLAYER && !_p2DeviceSet)
            {
                InputBehaviour p1Input = (InputBehaviour)_p1Input;
                InputBehaviour p2Input = (InputBehaviour)_p2Input;

                InputDevice device = null;
                if (!DeviceInputReceived(out device))
                    return;
                //Debug.Log("Input Received P2 " + device.name);
                if (p1Input.Devices.Find(args => args.deviceId == device.deviceId) == null && !device.name.Contains("Mouse"))
                {
                    //Debug.Log("Input Assigned P2 " + device.name);
                    AssignDevice(device, 2);
                    p2Input.enabled = true;
                    _p2DeviceSet = true;
                }
            }
        }
    }
}