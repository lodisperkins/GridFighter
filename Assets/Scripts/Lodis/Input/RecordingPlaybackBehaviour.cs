using Lodis.Gameplay;
using Lodis.Movement;
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
        private Coroutine _playbackRoutine;
        private GridMovementBehaviour _ownerMovement;
        private float _currentTime;
        private int _currentActionIndex;
        private int _startIndex;

        // Start is called before the first frame update
        void Awake()
        {
            _actions = ActionRecorderBehaviour.LoadRecording(_recordingName);
            _ownerMovement = GetComponent<GridMovementBehaviour>();
            _ownerMoveset = GetComponent<MovesetBehaviour>();
            _playbackRoutine = StartCoroutine(StartPlayback());
        }

        public void SetPlaybackTime(float timeStamp)
        {
            StopCoroutine(_playbackRoutine);
            ActionRecording recording = _actions.Find(action => action.TimeStamp == timeStamp);

            _startIndex = _actions.IndexOf(recording);

            _playbackRoutine = StartCoroutine(StartPlayback());
        }

        private IEnumerator StartPlayback()
        {
            for (_currentActionIndex = _startIndex; _currentActionIndex < _actions.Count; _currentActionIndex++)
            {
                yield return new WaitForSeconds(_actions[_currentActionIndex].TimeDelay);
                PerformAction(_actions[_currentActionIndex]);
            }
        }

        private void PerformAction(ActionRecording action)
        {
            if (action.ActionID == -1)
            {
                _ownerMovement.Move(action.ActionDirection);
                return;
            }

            _ownerMoveset.UseAbility(action.ActionID, 1.6f, action.ActionDirection);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}