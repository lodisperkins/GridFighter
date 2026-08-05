using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FixedPoints;
using static Lodis.AI.AIUtilities;
using System.IO;
using Types;

namespace Lodis.AI
{
    public class NetworkCharacterAIMovementBehaviour : SimulationBehaviour
    {
        [SerializeField] private bool _cancelPathingOnHit;

        private PanelBehaviour _moveTarget;
        private List<PanelBehaviour> _currentPath;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private MovesetBehaviour _moveset;
        private StateMachine _stateMachine;
        private CharacterStateMachineBehaviour _characterStateMachine;
        private Heuristic _pathFindHeuristic;

        private int _currentPathIndex;
        private bool _reachedDestination;
        private bool _needPath;

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

        public override string LogName => "NetworkCharacterAIMovementBehaviour";

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(_currentPathIndex);
            bw.Write(_reachedDestination);
            bw.Write(_needPath);
        }

        public override void Deserialize(BinaryReader br)
        {
            _currentPathIndex = br.ReadInt32();
            _reachedDestination = br.ReadBoolean();
            _needPath = br.ReadBoolean();
        }

        /// <summary>
        /// Hashes the serialized path-following state so rollback mismatches can be
        /// narrowed down to this movement controller.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return new string[]
            {
                $"Current Path Index: {_currentPathIndex}",
                $"Reached Destination: {_reachedDestination}",
                $"Need Path: {_needPath}"
            };
        }

        public override void Begin()
        {
            _characterStateMachine = GetComponentInChildren<CharacterStateMachineBehaviour>();
            _stateMachine = _characterStateMachine.StateMachine;
            _movementBehaviour = GetComponentInChildren<Movement.GridMovementBehaviour>();
            _currentPath = new List<PanelBehaviour>();
            _moveset = GetComponentInChildren<MovesetBehaviour>();

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
                Debug.Log(_movementBehaviour.gameObject.name + " cannot move to panel at location " + _moveTarget?.Position +
                    ". Panel at location " + _currentPath[_currentPathIndex].Position + " cannot be reached.");
        }

        // Update is called once per frame
        public override void Tick(Fixed32 dt)
        {
            PanelBehaviour start = _movementBehaviour.CurrentPanel;

            if (NeedPath && (StateMachine.CurrentState == "Idle" || (StateMachine.CurrentState == "Attack" && _moveset.LastAbilityInUse.GetCurrentCancelRule()?.CanCancelOnMove == true)))
            {
                if (_moveTarget == null || _moveTarget == null)
                {
                    return;
                }

                _currentPath = AI.AIUtilities.Instance.GetPath(start, _moveTarget, false, _movementBehaviour.Alignment, false, _pathFindHeuristic);
                _needPath = false;
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
