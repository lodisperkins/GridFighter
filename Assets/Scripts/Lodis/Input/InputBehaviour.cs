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
        private Vector2 _inputVal;

        public void OnMovement(InputAction.CallbackContext context)
        {
            if (_canMove)
            {
                if (context.ReadValue<Vector2>() == new Vector2(1,1))
                {
                    _gridMovement.Speed *= 2;
                    _gridMovement.MoveToPanel(new Vector2(context.ReadValue<Vector2>().x, 0) + _gridMovement.Position);
                    _gridMovement.MoveToPanel(new Vector2(0, context.ReadValue<Vector2>().y) + _gridMovement.Position);
                    _gridMovement.Speed /= 2;
                }
                _gridMovement.MoveToPanel(context.ReadValue<Vector2>() + _gridMovement.Position);
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
        }
    }
}

