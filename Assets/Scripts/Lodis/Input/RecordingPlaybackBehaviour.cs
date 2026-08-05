using FixedPoints;
using Lodis.AI;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

namespace Lodis.Input
{
    public class RecordingPlaybackBehaviour : SimulationBehaviour
    {
        [Tooltip("The file name of the recording to load and playback.")]
        [SerializeField] private string _recordingName;
        [Tooltip("Whether or not to start playing the recording immiediately.")]
        [SerializeField] private bool _autoPlayback = true;
        [Tooltip("If enabled, playback stops once the final recorded input has been used instead of looping back to the start.")]
        [SerializeField] private bool _playOnce;

        //-----
        private Dictionary<int, ActionRecording> _actions;
        private MovesetBehaviour _ownerMoveset;
        private TimedAction _playbackAction;
        private GridMovementBehaviour _ownerMovement;
        private int _currentActionFrame;
        private bool _isPaused;
        private Fixed32 _currentDelay;
        private InputBehaviour _inputBehaviour;
        private AIControllerBehaviour _aiControllerBehaviour;
        private int _frameOffset;

        public MovesetBehaviour OwnerMoveset { get => _ownerMoveset; set => _ownerMoveset = value; }
        public GridMovementBehaviour OwnerMovement { get => _ownerMovement; set => _ownerMovement = value; }
        public Fixed32 CurrentDelay { get => _currentDelay; private set => _currentDelay = value; }

        public override string LogName => nameof(RecordingPlaybackBehaviour);

        public string RecordingName { get => _recordingName; set => _recordingName = value; }
        public bool PlayOnce { get => _playOnce; set => _playOnce = value; }

        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();

            _inputBehaviour = GetComponent<InputBehaviour>();
            _aiControllerBehaviour = GetComponent<AIControllerBehaviour>();

            if (_inputBehaviour != null)
            {
                _inputBehaviour.AIControlled = true;
            }

            if (_aiControllerBehaviour != null)
            {
                _aiControllerBehaviour.enabled = false;
            }

            OwnerMovement = GetComponentInChildren<GridMovementBehaviour>();
            OwnerMoveset = GetComponentInChildren<MovesetBehaviour>();
            OwnerMovement.Entity.Data.AddComponent(this);
            _inputBehaviour.OnGetFlags += HandlePlaybackPolling;

            if (!string.IsNullOrWhiteSpace(RecordingName))
            {
                _actions = ActionRecorderBehaviour.LoadRecordingDictionary(RecordingName);
            }
        }

        private void OnDestroy()
        {
            if (_inputBehaviour != null)
            {
                _inputBehaviour.OnGetFlags -= HandlePlaybackPolling;
            }
        }

        private void HandlePlaybackPolling()
        {
            if (_inputBehaviour == null)
                return;

            _inputBehaviour.AIFlags = InputFlag.NONE;

            if (_isPaused || !_autoPlayback || _actions == null || _actions.Count == 0)
                return;

            int playbackFrame = GridGameManager.FrameNumber - _frameOffset;

            if (playbackFrame < 0)
                return;

            if (_playOnce && _currentActionFrame >= _actions.Count)
            {
                _autoPlayback = false;
                return;
            }

            if (!_actions.TryGetValue(playbackFrame, out ActionRecording action))
            {
                return;
            }

            _inputBehaviour.AIFlags = action.InputAction;
            _currentActionFrame++;

            Debug.Log("Performed input action: " + action.InputAction.ToString() + " at frame: " + playbackFrame);
        }

        public void PausePlayback()
        {
            //RoutineBehaviour.Instance.StopAction(_playbackAction);
            _isPaused = true;
        }

        public void UnpausePlayback() 
        {
            _isPaused = false;
        }

        protected override string[] GetLogItems()
        {
            return null;
        }

        public override void Serialize(BinaryWriter bw)
        {
        }

        public override void Deserialize(BinaryReader br)
        {
        }

        public void InitializePlayback(string recordingName, bool autoPlayback, bool playOnce = false)
        {
            _recordingName = recordingName;
            _autoPlayback = autoPlayback;
            _playOnce = playOnce;

            if (string.IsNullOrWhiteSpace(recordingName))
            {
                Debug.LogError("Recording name is null or empty. Playback will not be initialized.");
                return;
            }

            _actions = ActionRecorderBehaviour.LoadRecordingDictionary(recordingName);

            _currentActionFrame = 0;
            _currentDelay = 0;
            _playbackAction = null;

            if (_inputBehaviour != null)
            {
                _inputBehaviour.AIControlled = true;
                _inputBehaviour.AIFlags = InputFlag.NONE;
            }

            if (_aiControllerBehaviour != null)
            {
                _aiControllerBehaviour.enabled = false;
            }
        }

        public void SetPlaybackEnabled(bool enabled)
        {
            _autoPlayback = enabled;
            _frameOffset = GridGameManager.FrameNumber;

            if (!enabled && _inputBehaviour != null)
            {
                _inputBehaviour.AIFlags = InputFlag.NONE;
            }
        }
    }
}
