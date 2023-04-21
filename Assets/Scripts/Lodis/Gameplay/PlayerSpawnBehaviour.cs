using Lodis.AI;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.UI;
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
        private MovesetBehaviour _p2Moveset;
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
        [SerializeField()]
        [Tooltip("The data of the character to use when spawning player 1.")]
        private CharacterData _player1Data;
        [SerializeField()]
        [Tooltip("The data of the character to use when spawning player 2.")]
        private CharacterData _player2Data;
        [SerializeField]
        private BoolVariable _p1IsCustom;
        [SerializeField]
        private BoolVariable _p2IsCustom;
        private IControllable _p1Input;
        private IControllable _p2Input;
        private RingBarrierBehaviour _ringBarrierR;
        private RingBarrierBehaviour _ringBarrierL;
        private GridBehaviour _grid;
        private PanelBehaviour _lhsSpawnPanel;
        private PanelBehaviour _rhsSpawnPanel;
        private bool _suddenDeathActive;
        private SceneManagerBehaviour _sceneManager;
        private MovesetBehaviour _p1Moveset;

        public Vector2 RHSSpawnLocation { get => _RHSSpawnLocation; private set => _RHSSpawnLocation = value; }
        public Vector2 LHSSpawnLocation { get => _LHSSpawnLocation; private set => _LHSSpawnLocation = value; }

        /// <summary>
        /// The data of the character to use when spawning player 1.
        /// </summary>
        public CharacterData Player1Data { get => _player1Data; set => _player1Data = value; }
        /// <summary>
        /// The data of the character to use when spawning player 1.
        /// </summary>
        public CharacterData Player2Data { get => _player2Data; set => _player2Data = value; }
        public KnockbackBehaviour P1HealthScript { get => _p1Knockback; }
        public KnockbackBehaviour P2HealthScript { get => _p2Knockback; }
        public bool SuddenDeathActive { get => _suddenDeathActive; set => _suddenDeathActive = value; }

        private void Awake()
        {
            _ringBarrierL = BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ringBarrierR = BlackBoardBehaviour.Instance.RingBarrierRHS;
            _inputManager.playerPrefab = _playerRef;
            _sceneManager = SceneManagerBehaviour.Instance;
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
            {
                _player2 = _inputManager.JoinPlayer(1, 1, _sceneManager.P2ControlScheme, _sceneManager.P2Devices).gameObject;
                _player2.GetComponent<InputBehaviour>().Devices = _sceneManager.P2Devices;
            }

            _p2Input = _player2.GetComponent<IControllable>();

            _p2Input.Character = Instantiate(_player2Data.CharacterReference, _player2.transform);

            _p2Input.Character.name += "(P2)";
            _ringBarrierR.Owner = _p2Input.Character;
            _player2.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _p2Input.Character;
            //Get reference to player 2 components
            _p2Movement = _p2Input.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _p2Input.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p2Knockback = _p2Input.Character.GetComponent<KnockbackBehaviour>();
            _p2Moveset = _p2Input.Character.GetComponent<MovesetBehaviour>();

            if (_p2IsCustom.Value)
            {
                _p2Moveset.NormalDeckRef = DeckBuildingManagerBehaviour.LoadCustomNormalDeck("Custom");
                _p2Moveset.SpecialDeckRef = DeckBuildingManagerBehaviour.LoadCustomSpecialDeck("Custom");
            }

            _p2Input.PlayerID = BlackBoardBehaviour.Instance.Player2ID;
            _p2Input.Enabled = true;
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
            {
                _player1 = _inputManager.JoinPlayer(0, 0, _sceneManager.P1ControlScheme, _sceneManager.P1Devices).gameObject;
                _player1.GetComponent<InputBehaviour>().Devices = _sceneManager.P1Devices;
            }


            _p1Input = _player1.GetComponent<IControllable>();
            _p1Input.Character = Instantiate(_player1Data.CharacterReference, _player1.transform);

            _p1Input.Character.name += "(P1)";
            _ringBarrierL.Owner = _p1Input.Character;
            _player1.transform.forward = Vector3.right;
            BlackBoardBehaviour.Instance.Player1 = _p1Input.Character;
            //Get reference to player 2 components
            _p1Movement = _p1Input.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _p1Input.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p1Knockback = _p1Input.Character.GetComponent<KnockbackBehaviour>();
            _p1Input.PlayerID = BlackBoardBehaviour.Instance.Player1ID;

            _p1Moveset = _p1Input.Character.GetComponent<MovesetBehaviour>();

            if (_p2IsCustom)
            {
                _p1Moveset.NormalDeckRef = DeckBuildingManagerBehaviour.LoadCustomNormalDeck("Custom");
                _p1Moveset.SpecialDeckRef = DeckBuildingManagerBehaviour.LoadCustomSpecialDeck("Custom");
            }

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

        private void Update()
        {
            BlackBoardBehaviour.Instance.Player1State = _p1StateManager.StateMachine.CurrentState;


            if (_mode != GameMode.SINGLEPLAYER)
                BlackBoardBehaviour.Instance.Player2State = _p2StateManager?.StateMachine?.CurrentState;
        }
    }
}