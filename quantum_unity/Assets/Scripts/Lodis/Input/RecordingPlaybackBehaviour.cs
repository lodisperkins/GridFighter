using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lodis.Input
{
    public class RecordingPlaybackBehaviour : MonoBehaviour
    {
        [SerializeField]
        private string _recordingName;
        private List<ActionRecording> _actions;
        private MovesetBehaviour _ownerMoveset;
        private TimedAction _playbackRoutine;
        private GridMovementBehaviour _ownerMovement;
        private float _currentTime;
        private int _currentActionIndex;
        private int _startIndex;
        private bool _isPaused;
        [SerializeField]
        private bool _autoPlayback = true;
        private float _currentDelay;

        public MovesetBehaviour OwnerMoveset { get => _ownerMoveset; set => _ownerMoveset = value; }
        public GridMovementBehaviour OwnerMovement { get => _ownerMovement; set => _ownerMovement = value; }
        public float CurrentDelay { get => _currentDelay; private set => _currentDelay = value; }

        // Start is called before the first frame update
        void Awake()
        {
            _actions = ActionRecorderBehaviour.LoadRecording(_recordingName);
            OwnerMovement = GetComponentInChildren<GridMovementBehaviour>();
            OwnerMoveset = GetComponentInChildren<MovesetBehaviour>();
        }

        public void SetPlaybackTime(float timeStamp)
        {
            //RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            ActionRecording recording = null;

            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].TimeStamp == timeStamp)
                {
                    recording = _actions[i];
                    break;
                }
            }

            _currentActionIndex = _actions.IndexOf(recording);
            if (_currentActionIndex == -1)
                _currentActionIndex = 0;
            StartPlayback(_currentActionIndex);
        }

        private void PerformAction(ActionRecording action)
        {
            if (action.ActionID == -1)
            {
                OwnerMovement.Move(action.ActionDirection);
                return;
            }
            else if (action.ActionID == -2)
            {
                OwnerMoveset.ManualShuffle();
                return;
            }

            OwnerMoveset.UseAbility(action.ActionID, 1.6f, action.ActionDirection);
        }

        private void StartPlayback(int index)
        {
            _currentDelay = _actions[_currentActionIndex].TimeDelay;
            _playbackRoutine = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                PerformAction(_actions[index]);

            }, TimedActionCountType.SCALEDTIME, _actions[_currentActionIndex].TimeDelay);

        }

        public void PausePlayback()
        {
            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            _isPaused = true;
        }

        public void UnpausePlayback()
        {
            _isPaused = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isPaused || !_autoPlayback)
                return;

            if (_playbackRoutine == null || !_playbackRoutine.GetEnabled())
            {
                StartPlayback(_currentActionIndex);
                _currentActionIndex++;

                if (_currentActionIndex >= _actions.Count)
                    _currentActionIndex = 0;
            }
        }
    }
}