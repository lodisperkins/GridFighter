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
        private Gameplay.MovesetBehaviour _moveset;
        private bool _canMove = true;
        private Vector2 _storedMoveInput;
        private Vector2 _previousMoveInput;
        private float _minChargeLimit = 0.5f;
        [SerializeField]
        private InputActionAsset actions;

        private void Awake()
        {
            actions = GetComponent<PlayerInput>().actions;
            actions.actionMaps[0].actions[0].started += context => UpdateInputY(1);
            actions.actionMaps[0].actions[1].started += context => UpdateInputY(-1);
            actions.actionMaps[0].actions[2].started += context => UpdateInputX(-1);
            actions.actionMaps[0].actions[3].started += context => UpdateInputX(1);
            actions.actionMaps[0].actions[4].started += context => DisableMovement();
            actions.actionMaps[0].actions[4].canceled += context => UseAbility(Attack.WEAKNEUTRAL, context, new object[1]);
            actions.actionMaps[0].actions[4].canceled += context => EnableMovement();
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
        }

        public void UseAbility(Attack abilityType, InputAction.CallbackContext context, params object[] args)
        {
            float timeHeld = Mathf.Clamp((float)context.duration, 0, 4);

            if (timeHeld > _minChargeLimit && (int)abilityType < 4)
            {
                abilityType += 4;
                float powerScale = timeHeld * 0.1f + 1;
                args[0] = powerScale;

                _moveset.UseBasicAbility(abilityType, args);
                return;
            }

            _moveset.UseBasicAbility(abilityType);
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

