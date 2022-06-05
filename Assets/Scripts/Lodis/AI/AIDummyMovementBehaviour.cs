﻿using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class AIDummyMovementBehaviour : MonoBehaviour
    {
        private Coroutine _moveRoutine;
        private PanelBehaviour _moveTarget;
        private bool _needPath;
        private List<PanelBehaviour> _currentPath;
        private int _currentPathIndex;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private MovesetBehaviour _moveset;
        private StateMachine _stateMachine;
        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; }
        public StateMachine StateMachine { get => _stateMachine; }

        // Start is called before the first frame update
        void Start()
        {
            _stateMachine = GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            _movementBehaviour = GetComponent<Movement.GridMovementBehaviour>();
            _movementBehaviour.AddOnMoveEndAction(MoveToNextPanel);
            _currentPath = new List<PanelBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
        }

        private IEnumerator MoveRoutine(List<PanelBehaviour> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                _movementBehaviour.MoveToPanel(path[i], false, _movementBehaviour.Alignment);
                yield return new WaitUntil(() => !_movementBehaviour.IsMoving);
            }
        }

        public void MoveToLocation(PanelBehaviour panel)
        {
            if (_moveTarget == panel) return;

            _moveTarget = panel;
            _needPath = true;
        }

        public void MoveToLocation(Vector2 panelPosition)
        {
            if (_moveTarget.Position == panelPosition) return;

            BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _moveTarget, false, _movementBehaviour.Alignment);
            _needPath = true;
        }

        public void MoveToNextPanel()
        {
            _currentPathIndex++;

            if (_currentPathIndex >= _currentPath.Count || _currentPath.Count < 0)
                return;

            _movementBehaviour.MoveToPanel(_currentPath[_currentPathIndex], false);
            _currentPath.RemoveAt(0);
        }

        // Update is called once per frame
        void Update()
        {
            PanelBehaviour start = _movementBehaviour.CurrentPanel;

            if (_needPath && (StateMachine.CurrentState == "Idle" || (StateMachine.CurrentState == "Attack" && _moveset.LastAbilityInUse.abilityData.CanCancelOnMove)))
            {
                _currentPath = AI.AIUtilities.Instance.GetPath(start, _moveTarget, false, _movementBehaviour.Alignment);
                _needPath = false;
                _currentPathIndex = 0;

                if (_currentPath.Count > 0)
                    _movementBehaviour.MoveToPanel(_currentPath[_currentPathIndex], false);
            }

            if (StateMachine.CurrentState != "Idle" && StateMachine.CurrentState != "Moving" && _currentPath.Count > 0)
                _needPath = true;
        }
    }
}