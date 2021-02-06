using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Lodis.Input
{
    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class InputBehaviour : MonoBehaviour, PlayerControls.IPlayerMovementActions
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private bool _canMove = true;
        private Vector2 _storedInput;

        public void OnMovement(InputAction.CallbackContext context)
        {
            if (_gridMovement.IsMoving)
            {
                _storedInput = context.ReadValue<Vector2>();
                return;
            }

            if (_canMove)
            {
                _gridMovement.MoveToPanel(context.ReadValue<Vector2>() + _gridMovement.Position);
                Debug.Log(context.ReadValue<Vector2>());
                _gridMovement.Velocity = Vector2.zero;
                _canMove = false;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
        }   

        // Update is called once per frame
        void Update()
        {
            
            _canMove = _gridMovement.Velocity == Vector2.zero;
            if (!_gridMovement.IsMoving && _storedInput.magnitude > 0)
            {
                _gridMovement.MoveToPanel(_storedInput + _gridMovement.Position);
                _gridMovement.Velocity = Vector2.zero;
                _storedInput = Vector2.zero;
            }
        }
    }
}

