using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;
using Lodis.Gameplay;

namespace Lodis.Input
{
    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class InputBehaviour : MonoBehaviour
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private PlayerDefenseBehaviour _defense;
        private MovesetBehaviour _moveset;
        private bool _canMove = true;
        private Vector2 _storedMoveInput;
        private Vector2 _previousMoveInput;
        private Vector2 _attackDirection;
        [Tooltip("The minimum amount of time needed to hold the button down to change it to the charge variation.")]
        [SerializeField]
        private float _minChargeLimit = 0.5f;
        [Tooltip("The maximum amount of time needed before an attack is fully charged.")]
        [SerializeField]
        private float _maxChargeTime = 5;
        [Tooltip("The amount of time needed to clear the buffer when a direciotn is pressed.")]
        [SerializeField]
        private float _attackDirectionBufferClearTime;
        private float _timeOfLastDirectionInput;
        private InputActionAsset _actions;
        private int _playerID;
        private Movement.Condition _inputCondition = null;
        private bool _inputDisabled;

        public int PlayerID
        {
            get
            {
                return _playerID;
            }
            set
            {
                _playerID = value;
            }
        }


        private void Awake()
        {
            //Initialize action delegates
            _actions = GetComponent<PlayerInput>().actions;
            _actions.actionMaps[0].actions[0].started += context => UpdateInputY(1);
            _actions.actionMaps[0].actions[1].started += context => UpdateInputY(-1);
            _actions.actionMaps[0].actions[2].started += context => UpdateInputX(-1);
            _actions.actionMaps[0].actions[3].started += context => UpdateInputX(1);
            _actions.actionMaps[0].actions[4].started += context => DisableMovement();
            _actions.actionMaps[0].actions[4].canceled += context => UseAbility(context, new object[2]);
            _actions.actionMaps[0].actions[4].canceled += context => EnableMovement();
            _actions.actionMaps[0].actions[6].started += context => _defense.ActivateParry();
            _actions.actionMaps[0].actions[6].started += context => DisableInput(condition => !_defense.IsParrying);
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _defense = GetComponent<PlayerDefenseBehaviour>();
        }


        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public void UseAbility(InputAction.CallbackContext context, params object[] args)
        {
            //Ignore player input if they are in knockback
            if (BlackBoardBehaviour.GetPlayerStateFromID(PlayerID) == PlayerState.KNOCKBACK)
                return;

            AbilityType abilityType = AbilityType.NONE;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Decide which ability type to use based on the input
            if (_attackDirection.y != 0)
                abilityType = AbilityType.WEAKSIDE;
            else if (_attackDirection.x < 0)
                abilityType = AbilityType.WEAKBACKWARD;
            else if (_attackDirection.x > 0)
                abilityType = AbilityType.WEAKFORWARD;
            else
                abilityType = AbilityType.WEAKNEUTRAL;

            //Assign the arguments for the ability
            args[1] = _attackDirection;
            //Find the power scale based on the time the button was held to use a charge ability
            float timeHeld = Mathf.Clamp((float)context.duration, 0, _maxChargeTime);
            if (timeHeld > _minChargeLimit && (int)abilityType < 4)
            {
                abilityType += 4;
                float powerScale = 0;
                powerScale = timeHeld * 0.1f + 1;
                args[0] = powerScale;
                _moveset.UseBasicAbility(abilityType, args);
                return;
            }

            //Use a normal ability if it was not held long enough
            _moveset.UseBasicAbility(abilityType, args);
        }

        /// <summary>
        /// Disable player movement on grid
        /// </summary>
        public void DisableMovement()
        {
            _canMove = false;
            _gridMovement.DisableMovement(condition =>  _canMove == true );
        }

        /// <summary>
        /// Enable player movement
        /// </summary>
        public void EnableMovement()
        {
            //Don't enable if player is in knockback
            if (BlackBoardBehaviour.GetPlayerStateFromID(PlayerID) == PlayerState.KNOCKBACK)
                return;

            _canMove = true;
        }

        public void DisableInput(Movement.Condition condition)
        {
            _inputDisabled = true;
            _actions.Disable();
            _inputCondition = condition;
        }

        public void UpdateInputX(int x)
        {
            _storedMoveInput = new Vector2(x, 0);
        }

        public void UpdateInputY(int y)
        {
            _storedMoveInput = new Vector2(0, y);
        }

        // Update is called once per frame
        void Update()
        {

            if (_inputCondition != null)
                if (_inputCondition.Invoke())
                {
                    _actions.Enable();
                    _inputDisabled = false;
                    _inputCondition = null;
                }

            //Move if the is a movement stored and movement is allowed
            if (_storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving)
            {
                _gridMovement.MoveToPanel(_storedMoveInput + _gridMovement.Position);
                _gridMovement.Velocity = Vector2.zero;
                _storedMoveInput = Vector2.zero;
            }

            //Stores the current attack direction input
            Vector3 attackDirInput = _actions.actionMaps[0].actions[5].ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _attackDirection = attackDirInput;
                _timeOfLastDirectionInput = Time.time;
            }
                
            //Clear the buffer if its exceeded the alotted time
            if (Time.time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _attackDirection = Vector2.zero;
        }
    }
}

