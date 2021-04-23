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
        private MovesetBehaviour _moveset;
        private bool _canMove = true;
        private Vector2 _storedMoveInput;
        private Vector2 _previousMoveInput;
        private Vector2 _attackDirection;
        private float _minChargeLimit = 0.5f;
        [SerializeField]
        private InputActionAsset _actions;

        private void Awake()
        {
            _actions = GetComponent<PlayerInput>().actions;
            _actions.actionMaps[0].actions[0].started += context => UpdateInputY(1);
            _actions.actionMaps[0].actions[1].started += context => UpdateInputY(-1);
            _actions.actionMaps[0].actions[2].started += context => UpdateInputX(-1);
            _actions.actionMaps[0].actions[3].started += context => UpdateInputX(1);
            _actions.actionMaps[0].actions[4].started += context => DisableMovement();
            _actions.actionMaps[0].actions[4].canceled += context => UseAbility(context, new object[2]);
            _actions.actionMaps[0].actions[4].canceled += context => EnableMovement();
            _actions.actionMaps[0].actions[5].performed += context => { _attackDirection = context.ReadValue<Vector2>(); };
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
        }

        public void UseAbility(InputAction.CallbackContext context, params object[] args)
        {
            AbilityType abilityType = AbilityType.NONE;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            if (_attackDirection.y != 0)
                abilityType = AbilityType.WEAKSIDE;
            else if (_attackDirection.x == -1)
                abilityType = AbilityType.WEAKBACKWARD;
            else if (_attackDirection.x == 1)
                abilityType = AbilityType.WEAKFORWARD;
            else
                abilityType = AbilityType.WEAKNEUTRAL;

            float powerScale = 0;
            args[0] = powerScale;
            args[1] = _attackDirection;

            float timeHeld = Mathf.Clamp((float)context.duration, 0, 4);

            if (timeHeld > _minChargeLimit && (int)abilityType < 4)
            {
                abilityType += 4;
                powerScale = timeHeld * 0.1f + 1;
                

                _moveset.UseBasicAbility(abilityType, args);
                return;
            }

            _moveset.UseBasicAbility(abilityType, args);
        }

        public void DisableMovement()
        {
            _canMove = false;
            _gridMovement.DisableMovement(condition =>  _canMove == true );
        }

        public void EnableMovement()
        {
            _canMove = true;
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
            if (_storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving)
            {
                _gridMovement.MoveToPanel(_storedMoveInput + _gridMovement.Position);
                _gridMovement.Velocity = Vector2.zero;
                _storedMoveInput = Vector2.zero;
            }
        }
    }
}

