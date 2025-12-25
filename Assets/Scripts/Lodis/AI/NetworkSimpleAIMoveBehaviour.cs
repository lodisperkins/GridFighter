using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections.Generic;
using UnityEngine;
using FixedPoints;
using static Lodis.AI.AIUtilities;
using System.IO;
using Types;
using UnityEngine.Events;
using NaughtyAttributes;

namespace Lodis.AI
{
    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class NetworkSimpleAIMovementBehaviour : SimulationBehaviour
    {
        [SerializeField] private Movement.GridMovementBehaviour _movementBehaviour;
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _moveAnimParmIsDirection;
        [HideIf("_moveAnimParmIsDirection")]
        [SerializeField] private string _moveAnimParam;
        [ShowIf("_moveAnimParmIsDirection")]
        [SerializeField] private string _moveAnimParamX;
        [ShowIf("_moveAnimParmIsDirection")]
        [SerializeField] private string _moveAnimParamY;
        [SerializeField] private bool _faceMovementDirection;
        [SerializeField] private UnityEvent _onReachedDestination;

        private PanelBehaviour _moveTarget;
        private List<PanelBehaviour> _currentPath = new List<PanelBehaviour>();
        private Heuristic _pathFindHeuristic;

        private int _currentPathIndex;
        private bool _reachedDestination;
        private bool _needPath;

        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; }
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

        private void OnValidate()
        {
            if (_movementBehaviour == null)
                _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        protected override void Awake()
        {
            base.Awake();
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ClearPath);
        }

        public override void End()
        {
            base.End();

            _currentPathIndex = 0;
            _moveTarget = null;
            NeedPath = false;
            ReachedDestination = false;
        }

        public void MoveToLocation(PanelBehaviour panel)
        {
            if (_moveTarget == panel)
                return;

            _reachedDestination = false;
            _moveTarget = panel;
            NeedPath = true;
        }

        public void MoveToLocation(FVector2 panelPosition, Heuristic heuristic = null)
        {
            if (_moveTarget?.Position == panelPosition)
                return;

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
                _onReachedDestination?.Invoke();
                return;
            }

            PanelBehaviour currentPanel = _currentPath[_currentPathIndex];

            if (currentPanel == null || !_movementBehaviour.MoveToPanel(currentPanel, false))
                Debug.Log(Entity.Data.Name + " cannot move to panel at location " + _moveTarget?.Position +
                    ". Panel at location " + _currentPath[_currentPathIndex].Position + " cannot be reached.");
        }

        public override void Tick(Fixed32 dt)
        {
            PanelBehaviour start = _movementBehaviour.CurrentPanel;

            //Update pathing.
            if (NeedPath && !_movementBehaviour.IsMoving)
            {
                if (_moveTarget == null)
                {
                    return;
                }

                _currentPath = AI.AIUtilities.Instance.GetPath(start, _moveTarget, false, _movementBehaviour.Alignment, false, _pathFindHeuristic);
                _needPath = false;
                _currentPathIndex = 1;

                if (_currentPath.Count > 1)
                {
                    _movementBehaviour.MoveToPanel(_currentPath[_currentPathIndex], false);
                }
            }
            FVector2 direction = (_currentPath[_currentPathIndex].FixedWorldPosition - FixedTransform.WorldPosition).GetNormalized().GetWithoutY();

            //Update animation and facing.
            if (_animator)
            {
                if (!_moveAnimParmIsDirection)
                {
                    _animator.SetBool(_moveAnimParam, !ReachedDestination);
                }
                else
                {
                    _animator.SetFloat(_moveAnimParamX, direction.X);
                    _animator.SetFloat(_moveAnimParamY, direction.Y);
                }
            }

            if (_faceMovementDirection && !ReachedDestination)
            {
                

                if (direction != FVector2.Zero)
                {
                    Entity.FixedTransform.Forward = direction;
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