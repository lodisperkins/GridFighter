using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using UnityEngine.UI;
using Lodis.GridScripts;

namespace Lodis.AI
{
    public class AITrainingBehaviour : MonoBehaviour
    {
        private AIControllerBehaviour _aiController;
        private BehaviorExecutor _executor;
        private AIDummyMovementBehaviour _movement;
        private Movement.GridMovementBehaviour _opponentMovement;
        private Movement.GridMovementBehaviour _gridMovement;

        [SerializeField]
        private Text _behaviorButtonText;
        [SerializeField]
        private Text _energyButtonText;
        [SerializeField]
        private Text _invincibleText;
        private int _currentState = -1;
        private bool _initialized;

        private void Start()
        {
            _opponentMovement = BlackBoardBehaviour.Instance.Player1.GetComponent<Movement.GridMovementBehaviour>();
            _gridMovement = BlackBoardBehaviour.Instance.Player2.GetComponent<Movement.GridMovementBehaviour>();
        }

        public void Init()
        {
            _aiController = (AIControllerBehaviour)BlackBoardBehaviour.Instance.Player2Controller;
            _executor = _aiController.GetComponent<BehaviorExecutor>();
            _movement = _aiController.GetComponent<AIDummyMovementBehaviour>();
            _currentState = 0;
            _initialized = true;
        }

        public void ToggleEnergyText()
        {
            if (!MatchManagerBehaviour.InfiniteEnergy)
                _energyButtonText.text = "Infinite Energy : Off";
            else if (MatchManagerBehaviour.InfiniteEnergy)
                _energyButtonText.text = "Infinite Energy : On";
        }

        public void ToggleInvincibleText()
        {
            if (!MatchManagerBehaviour.Instance.InvincibleBarriers)
                _invincibleText.text = "Invincible Barriers : Off";
            else if (MatchManagerBehaviour.Instance.InvincibleBarriers)
                _invincibleText.text = "Invincible Barriers : On";
        }

        public void NextAIState()
        {
            _currentState++;

            if (_currentState > 2)
                _currentState = 0;
        }

        public void SetAIState(int currentState)
        {
            _currentState = currentState;
        }

        private void SetAIState()
        {
            if (_currentState == 0)
            {
                _aiController.enabled = false;
                _executor.enabled = false;
                _behaviorButtonText.text = "CPU Behavior : Follow";
                _movement.enabled = true;
            }
            else if (_currentState == 1)
            {
                _movement.enabled = false;
                _aiController.enabled = true;
                _behaviorButtonText.text = "CPU Behavior : Attack";
            }
            else if (_currentState == 2)
            {
                _movement.enabled = false;
                _aiController.enabled = false;
                _behaviorButtonText.text = "CPU Behavior : Idle";
            }
        }

        private void Update()
        {
            if (!_initialized)
                return;

            SetAIState();

            if (_currentState != 0)
                return;

            PanelBehaviour panel = null;

            Vector2 location = new Vector2(BlackBoardBehaviour.Instance.Grid.TempMaxColumns, _opponentMovement.Position.y);

            if (BlackBoardBehaviour.Instance.Grid.GetPanel(location, out panel, false, _gridMovement.Alignment))
                _movement.MoveToLocation(panel);
        }
    }
}