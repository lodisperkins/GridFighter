using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Lodis.Gameplay
{
    public class GameManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputManager _inputManager;
        [SerializeField]
        private GameObject _playerRef;
        [SerializeField]
        private GridScripts.GridBehaviour _grid;
        private PlayerInput _player1;
        private PlayerInput _player2;

        private void Awake()
        {
            _inputManager.playerPrefab = _playerRef;
        }

        // Start is called before the first frame update
        void Start()
        {
            //Spawn players
            _inputManager.JoinPlayer(0, 0, "Player", Keyboard.current);
            _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[2]);

            //Store references to players
            _player1 = PlayerInput.GetPlayerByIndex(0);
            _player2 = PlayerInput.GetPlayerByIndex(1);

            //Initialize grid
            _grid.CreateGrid();

            //Move players to spawn
            Movement.GridMovementBehaviour player1Movement = _player1.GetComponent<Movement.GridMovementBehaviour>();
            Movement.GridMovementBehaviour player2Movement = _player2.GetComponent<Movement.GridMovementBehaviour>();
            player1Movement.Position = _grid.LhsSpawnPanel.Position;
            player1Movement.Alignment = GridScripts.GridAlignment.LEFT;
            player2Movement.Position = _grid.RhsSpawnPanel.Position;
            player2Movement.Alignment = GridScripts.GridAlignment.RIGHT;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}


