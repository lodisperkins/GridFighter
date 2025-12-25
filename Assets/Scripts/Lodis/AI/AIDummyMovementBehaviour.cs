using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections.Generic;
using UnityEngine;
using FixedPoints;
using static Lodis.AI.AIUtilities;

namespace Lodis.AI
{
    public class AIDummyMovementBehaviour : MonoBehaviour
    {
        [SerializeField] private bool _cancelPathingOnHit;

        private AIControllerBehaviour _dummyBehaviour;
        private Coroutine _moveRoutine;
        private PanelBehaviour _moveTarget;
        private bool _needPath;
        private List<PanelBehaviour> _currentPath;
        private int _currentPathIndex;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private MovesetBehaviour _moveset;
        private StateMachine _stateMachine;
        private CharacterStateMachineBehaviour _characterStateMachine;
        private Heuristic _pathFindHeuristic;
        private bool _reachedDestination;

        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; }
        public StateMachine StateMachine { get => _stateMachine; }
        public bool ReachedDestination { get => _reachedDestination; private set => _reachedDestination = value; }
        public bool NeedPath
        {
            get => _needPath;
            private set
            { 
                _needPath = value;

                if (_needPath)
                    _movementBehaviour.AddOnMoveEndAction(MoveToNextPanel);
                else
                    _movementBehaviour.RemoveOnMoveEndAction(MoveToNextPanel);
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            _dummyBehaviour = GetComponent<AIControllerBehaviour>();
            _characterStateMachine = _dummyBehaviour.Character.GetComponent<CharacterStateMachineBehaviour>();
            _stateMachine = _characterStateMachine.StateMachine;
            _movementBehaviour = _dummyBehaviour.Character.GetComponent<Movement.GridMovementBehaviour>();
            _currentPath = new List<PanelBehaviour>();
            _moveset = _dummyBehaviour.Character.GetComponent<MovesetBehaviour>();

            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ClearPath);
        }

        public void MoveToLocation(PanelBehaviour panel)
        {
            if (_moveTarget == panel) return;

            _reachedDestination = false;
            _moveTarget = panel;
            NeedPath = true;
        }

        public void MoveToLocation(FVector2 panelPosition, Heuristic heuristic = null)
        {
            if (_moveTarget?.Position == panelPosition) return;

            BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _moveTarget, false, _movementBehaviour.Alignment);
            _reachedDestination = false;
            NeedPath = true;
        }

        public void MoveToNextPanel()
        {
            _currentPathIndex++;

            if (_currentPathIndex >= _currentPath.Count || _currentPath.Count < 0)
            {
                ReachedDestination = true;
                return;
            }

            PanelBehaviour currentPanel = _currentPath[_currentPathIndex];

            if (currentPanel == null || !_movementBehaviour.MoveToPanel(currentPanel, false))
                Debug.Log(_dummyBehaviour.Character.name + " cannot move to panel at location " + _moveTarget?.Position +
                    ". Panel at location " + _currentPath[_currentPathIndex].Position + " cannot be reached.");
        }

        // Update is called once per frame
        void Update()
        {
            PanelBehaviour start = _movementBehaviour.CurrentPanel;

            if (NeedPath && (StateMachine.CurrentState == "Idle" || (StateMachine.CurrentState == "Attack" && _moveset.LastAbilityInUse.GetCurrentCancelRule()?.CanCancelOnMove == true)))
            {
                if (_moveTarget == null || _moveTarget == null)
                {
                    return;
                }

                _currentPath = AI.AIUtilities.Instance.GetPath(start, _moveTarget, false, _movementBehaviour.Alignment, false, _pathFindHeuristic);
                NeedPath = false;
                _currentPathIndex = 1;

                if (_currentPath.Count > 1)
                    _movementBehaviour.MoveToPanel(_currentPath[_currentPathIndex], false);
            }

            if (_characterStateMachine.CompareState("Tumbling", "Flinching") && _currentPath.Count > 0 && !ReachedDestination)
            {
                if (_cancelPathingOnHit)
                {
                    ClearPath();
                }
                else
                {
                    NeedPath = true;
                }
            }
        }

        private void ClearPath()
        {
            _currentPath.Clear();
            _currentPathIndex = 0;
            NeedPath = false;
        }
    }
}