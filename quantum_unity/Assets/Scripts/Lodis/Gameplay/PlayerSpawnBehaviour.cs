using Lodis.AI;
using Lodis.CharacterCreation;
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
        [Header("Scene References")]
        [SerializeField] private Vector2 _RHSSpawnLocation;
        [SerializeField] private Vector2 _LHSSpawnLocation;
        [SerializeField] private PlayerInputManager _inputManager;
        [SerializeField] private GameObject _playerRef;
        [SerializeField] private QuantumRunnerLocalDebug _quantumRunner;

        [Header("Player Data")]
        [Tooltip("The data of the character to use when spawning player 1.")]
        [SerializeField] private CharacterData _player1Data;
        [Tooltip("The data of the character to use when spawning player 2.")]
        [SerializeField] private CharacterData _player2Data;
        [Tooltip("The ai dummy character to spawn.")]
        [SerializeField] private AI.AIControllerBehaviour _dummy;

        [Header("Custom Character Usage Flags")]
        [SerializeField] private BoolVariable _p1IsCustom;
        [SerializeField] private BoolVariable _p2IsCustom;

        //---
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
        private InputBehaviour _p2Input;

        private GameMode _mode;
        private IControllable _p1InputController;
        private IControllable _p2InputController;

        private RingBarrierBehaviour _ringBarrierR;
        private RingBarrierBehaviour _ringBarrierL;

        private GridBehaviour _grid;
        private PanelBehaviour _lhsSpawnPanel;
        private PanelBehaviour _rhsSpawnPanel;

        private bool _suddenDeathActive;

        private SceneManagerBehaviour _sceneManager;

        private MovesetBehaviour _p1Moveset;
        private InputBehaviour _p1Input;

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

            _p2InputController = _player2.GetComponent<IControllable>();
            _quantumRunner.Players.Add(_p2InputController);

            _p2InputController.Character = Instantiate(_player2Data.CharacterReference, _player2.transform);

            _p2InputController.Character.name += "(P2)";
            _ringBarrierR.Owner = _p2InputController.Character;
            _player2.transform.forward = Vector3.left;
            BlackBoardBehaviour.Instance.Player2 = _p2InputController.Character;

            //Get reference to player 2 components
            _p2Movement = _p2InputController.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p2StateManager = _p2InputController.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p2Knockback = _p2InputController.Character.GetComponent<KnockbackBehaviour>();
            _p2Moveset = _p2InputController.Character.GetComponent<MovesetBehaviour>();
            _p2Input = _player2.GetComponent<InputBehaviour>();

            ApplyBindingOverrides(_p2Input, SceneManagerBehaviour.Instance.P2InputProfile,SceneManagerBehaviour.Instance.P2ControlScheme, true);
            
            if (_p2IsCustom.Value)
            {
                _p2Moveset.NormalDeckRef = DeckBuildingManagerBehaviour.LoadCustomNormalDeck(Player2Data.DisplayName);
                _p2Moveset.SpecialDeckRef = DeckBuildingManagerBehaviour.LoadCustomSpecialDeck(Player2Data.DisplayName);
                MeshReplacementBehaviour meshManager = _p2InputController.Character.GetComponentInChildren<MeshReplacementBehaviour>();

                Color hairColor;
                Color faceColor;

                CustomCharacterManagerBehaviour.LoadCustomCharacter(Player2Data.DisplayName, meshManager, out hairColor, out faceColor);

                meshManager.HairColor = hairColor;
                meshManager.FaceColor = faceColor;
            }

            _p2InputController.PlayerID = BlackBoardBehaviour.Instance.Player2ID;
            _p2InputController.Enabled = true;
            BlackBoardBehaviour.Instance.Player2Controller = _p2InputController;

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


            _p1InputController = _player1.GetComponent<IControllable>();
            _p1InputController.Character = Instantiate(_player1Data.CharacterReference, _player1.transform);

            _quantumRunner.Players.Add(_p2InputController);

            _p1InputController.Character.name += "(P1)";
            _ringBarrierL.Owner = _p1InputController.Character;
            _player1.transform.forward = Vector3.right;
            BlackBoardBehaviour.Instance.Player1 = _p1InputController.Character;
            //Get reference to player 2 components
            _p1Movement = _p1InputController.Character.GetComponent<Movement.GridMovementBehaviour>();
            _p1StateManager = _p1InputController.Character.GetComponent<CharacterStateMachineBehaviour>();
            _p1Knockback = _p1InputController.Character.GetComponent<KnockbackBehaviour>();
            _p1InputController.PlayerID = BlackBoardBehaviour.Instance.Player1ID;

            _p1Moveset = _p1InputController.Character.GetComponent<MovesetBehaviour>();
            _p1Input = _player1.GetComponent<InputBehaviour>();

            ApplyBindingOverrides(_p1Input, SceneManagerBehaviour.Instance.P1InputProfile, SceneManagerBehaviour.Instance.P1ControlScheme);

            if (_p1IsCustom.Value)
            {
                _p1Moveset.NormalDeckRef = DeckBuildingManagerBehaviour.LoadCustomNormalDeck(Player1Data.DisplayName);
                _p1Moveset.SpecialDeckRef = DeckBuildingManagerBehaviour.LoadCustomSpecialDeck(Player1Data.DisplayName);
                MeshReplacementBehaviour meshManager = _p1InputController.Character.GetComponentInChildren<MeshReplacementBehaviour>();

                Color hairColor;
                Color faceColor;

                CustomCharacterManagerBehaviour.LoadCustomCharacter(Player1Data.DisplayName, meshManager, out hairColor, out faceColor);

                meshManager.HairColor = hairColor;
                meshManager.FaceColor = faceColor;
            }

            BlackBoardBehaviour.Instance.Player1Controller = _p1InputController;

            if (_grid.GetPanel(LHSSpawnLocation, out _lhsSpawnPanel, false))
                _p1Movement.MoveToPanel(_lhsSpawnPanel, true, GridScripts.GridAlignment.ANY);
            else
                Debug.LogError("Invalid spawn point for player 1. Spawn was " + LHSSpawnLocation);

            _p1Movement.Alignment = GridScripts.GridAlignment.LEFT;
        }

        private static void ApplyBindingOverrides(InputBehaviour playerInput, InputProfileData profile, string scheme, bool invertHorizontal = false)
        {
            if (!playerInput)
                return;

            SceneManagerBehaviour sceneManager = SceneManagerBehaviour.Instance;

            int index = playerInput.PlayerControls.Player.Attack.GetBindingIndex(group: scheme);

            InputProfileData inputProfile = profile;

            if (scheme == "Keyboard")
            {
                ApplyCompositeOverrides(inputProfile, playerInput.PlayerControls.Player.Move);
                ApplyCompositeOverrides(inputProfile, playerInput.PlayerControls.Player.AttackDirection, invertHorizontal);
            }

            playerInput.PlayerControls.Player.Attack.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.WeakAttack).Path);

            index = playerInput.PlayerControls.Player.ChargeAttack.GetBindingIndex(group: scheme);
            playerInput.PlayerControls.Player.ChargeAttack.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.StrongAttack).Path);

            index = playerInput.PlayerControls.Player.Special1.GetBindingIndex(group: scheme);
            playerInput.PlayerControls.Player.Special1.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.Special1).Path);

            index = playerInput.PlayerControls.Player.Special2.GetBindingIndex(group: scheme);
            playerInput.PlayerControls.Player.Special2.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.Special2).Path);

            index = playerInput.PlayerControls.Player.Burst.GetBindingIndex(scheme);
            playerInput.PlayerControls.Player.Burst.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.Burst).Path);

            index = playerInput.PlayerControls.Player.Shuffle.GetBindingIndex(scheme);
            playerInput.PlayerControls.Player.Shuffle.ApplyBindingOverride(index, inputProfile.GetBinding(BindingType.Shuffle).Path);
        }

        private static void ApplyCompositeOverrides(InputProfileData p1InputProfile, InputAction compositeAction, bool invertHorizontal = false)
        {
            InputActionSetupExtensions.BindingSyntax moveBinding = compositeAction.ChangeCompositeBinding("WASD");

            InputActionSetupExtensions.BindingSyntax upBinding = moveBinding.NextPartBinding("Up");
            InputActionSetupExtensions.BindingSyntax downBinding = moveBinding.NextPartBinding("Down");

            InputActionSetupExtensions.BindingSyntax leftBinding;
            InputActionSetupExtensions.BindingSyntax rightBinding;

            if (!invertHorizontal)
            {
                leftBinding = moveBinding.NextPartBinding("Left");
                rightBinding = moveBinding.NextPartBinding("Right");
            }
            else
            {
                leftBinding = moveBinding.NextPartBinding("Right");
                rightBinding = moveBinding.NextPartBinding("Left");
            }


            compositeAction.ApplyBindingOverride(upBinding.bindingIndex, p1InputProfile.GetBinding(BindingType.MoveUp).Path);
            compositeAction.ApplyBindingOverride(downBinding.bindingIndex, p1InputProfile.GetBinding(BindingType.MoveDown).Path);
            compositeAction.ApplyBindingOverride(leftBinding.bindingIndex, p1InputProfile.GetBinding(BindingType.MoveLeft).Path);
            compositeAction.ApplyBindingOverride(rightBinding.bindingIndex, p1InputProfile.GetBinding(BindingType.MoveRight).Path);
        }

        void OnDestroy()
        {
            _p1Input.PlayerControls.RemoveAllBindingOverrides();

            if (_p2Input)
                _p2Input.PlayerControls.RemoveAllBindingOverrides();
        }

        void OnEnable()
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
        }

        void OnDisable()
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
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
            GridMovementBehaviour movement = _p1InputController.Character.GetComponent<GridMovementBehaviour>();
            movement.CancelMovement();
            movement.EnableMovement();
            movement.CanMoveDiagonally = true;
            movement.MoveToPanel(LHSSpawnLocation, true);
            movement.CanMoveDiagonally = false;

            KnockbackBehaviour knockback = _p1InputController.Character.GetComponent<KnockbackBehaviour>();
            knockback.CancelHitStun();
            knockback.CancelStun();
            knockback.Physics.StopVelocity();
            knockback.Physics.RB.isKinematic = true;

            InputBehaviour input = _player1.GetComponent<InputBehaviour>();
            if (input)
                input.ClearBuffer();

            //Player 2
            movement = _p2InputController.Character.GetComponent<GridMovementBehaviour>();
            movement.CancelMovement();
            movement.EnableMovement();
            movement.CanMoveDiagonally = true;
            movement.MoveToPanel(RHSSpawnLocation, true);
            movement.CanMoveDiagonally = false;

            knockback = _p2InputController.Character.GetComponent<KnockbackBehaviour>();
            knockback.CancelHitStun();
            knockback.CancelStun();
            knockback.Physics.StopVelocity();
            knockback.Physics.RB.isKinematic = true;

            input = _player2.GetComponent<InputBehaviour>();
            if (input)
                input.ClearBuffer();

            MovesetBehaviour moveset = _p1InputController.Character.GetComponent<MovesetBehaviour>();
            moveset.ResetAll();

            moveset = _p2InputController.Character.GetComponent<MovesetBehaviour>();
            moveset.ResetAll();

            //Enable both players in case either are inactive
            _p1InputController.Character.SetActive(true);
            _p2InputController.Character.SetActive(true);
        }

        private void LoadAIDecisions()
        {
            if (_mode == GameMode.SINGLEPLAYER || _mode == GameMode.MULTIPLAYER)
                return;

            AIControllerBehaviour dummyController = BlackBoardBehaviour.Instance.Player2Controller as AIControllerBehaviour;
            dummyController.LoadDecisions();

            if (_mode == GameMode.SIMULATE)
            {
                dummyController = BlackBoardBehaviour.Instance.Player1Controller as AIControllerBehaviour;
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