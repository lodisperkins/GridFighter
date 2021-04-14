using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;

namespace Lodis.Input
{
    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class InputBehaviour : MonoBehaviour
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private Gameplay.MovesetBehaviour _moveset;
        private bool _canMove = true;
        private Vector2 _storedInput;
        private Vector2 _previousInput;
        private int counter = 0;
        [SerializeField]
        private InputActionAsset actions;

        private void Awake()
        {
            actions = GetComponent<PlayerInput>().actions;
            actions.actionMaps[0].actions[0].started += context => UpdateInputY(1);
            actions.actionMaps[0].actions[1].started += context => UpdateInputY(-1);
            actions.actionMaps[0].actions[2].started += context => UpdateInputX(-1);
            actions.actionMaps[0].actions[3].started += context => UpdateInputX(1);
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<Gameplay.MovesetBehaviour>();
        }

        public void UpdateInputX(int x)
        {
            _storedInput = new Vector2(x, 0);
        }

        public void UpdateInputY(int y)
        {
            _storedInput = new Vector2(0, y);
        }

        // Update is called once per frame
        void Update()
        {
            if (_storedInput.magnitude > 0 && !_gridMovement.IsMoving)
            {
                _gridMovement.MoveToPanel(_storedInput + _gridMovement.Position);
                _gridMovement.Velocity = Vector2.zero;
                _storedInput = Vector2.zero;
            }
        }
    }
}

