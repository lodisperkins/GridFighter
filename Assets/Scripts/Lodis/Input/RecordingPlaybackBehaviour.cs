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

        public MovesetBehaviour OwnerMoveset { get => _ownerMoveset; set => _ownerMoveset = value; }
        public GridMovementBehaviour OwnerMovement { get => _ownerMovement; set => _ownerMovement = value; }

        // Start is called before the first frame update
        void Awake()
        {
            _actions = ActionRecorderBehaviour.LoadRecording(_recordingName);
            OwnerMovement = GetComponentInChildren<GridMovementBehaviour>();
            OwnerMoveset = GetComponentInChildren<MovesetBehaviour>();
        }

        public void SetPlaybackTime(float timeStamp)
        {
            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            ActionRecording recording = _actions.Find(action => action.TimeStamp == timeStamp);

            _startIndex = _actions.IndexOf(recording);
        }

        private void PerformAction(ActionRecording action)
        {
            if (action.ActionID == -1)
            {
                OwnerMovement.Move(action.ActionDirection);
                return;
            }

            OwnerMoveset.UseAbility(action.ActionID, 1.6f, action.ActionDirection);
        }

        // Update is called once per frame
        void Update()
        {
            if (_playbackRoutine == null || !_playbackRoutine.GetEnabled())
            {
                _playbackRoutine = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                {
                    PerformAction(_actions[_currentActionIndex]);
                    _currentActionIndex++;

                    if (_currentActionIndex >= _actions.Count)
                        _currentActionIndex = 0;

                }, TimedActionCountType.SCALEDTIME, _actions[_currentActionIndex].TimeDelay);
            }
        }
    }
}