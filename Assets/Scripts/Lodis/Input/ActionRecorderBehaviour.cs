using CustomEventSystem;
using FixedPoints;
using Lodis.Gameplay;
using Lodis.Movement;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Types;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Input
{

    public class ActionRecording
    {
        public int FrameNumber;
        public InputFlag InputAction;

        public ActionRecording(int frameNumber, InputFlag actionID)
        {
            FrameNumber = frameNumber;
            InputAction = actionID;
        }
    }

    public class ActionRecorderBehaviour : SimulationBehaviour
    {
        [SerializeField, Tooltip("The name of the recording.")]
        private string _recordingName;
        [SerializeField, Tooltip("Indicates whether recording is enabled.")]
        private bool _canRecord;
        [SerializeField, Tooltip("Event triggered when recording begins.")]
        private CustomEventSystem.Event OnRecordBegin;
        [SerializeField, Tooltip("Event triggered when recording finishes.")]
        private CustomEventSystem.Event OnRecordFinish;

        //---
        private List<ActionRecording> _recordedActions = new List<ActionRecording>();

        private GridMovementBehaviour _ownerMovement;
        private MovesetBehaviour _ownerMoveset;
        private CharacterStateMachineBehaviour _stateMachine;

        private static JsonSerializerSettings _settings;
        private bool _startRecordingOnStart;
        private bool _matchOverHookRegistered;
        private bool _subscribedToInput;

        public string RecordingName { get => _recordingName; }
        public GridMovementBehaviour OwnerMovement { get => _ownerMovement; set => _ownerMovement = value; }
        public MovesetBehaviour OwnerMoveset { get => _ownerMoveset; set => _ownerMoveset = value; }
        public CharacterStateMachineBehaviour StateMachine { get => _stateMachine; set => _stateMachine = value; }
        public static JsonSerializerSettings Settings { get => _settings; set => _settings = value; }
       
        public bool CanRecord { get => _canRecord; private set => _canRecord = value; }

        public override string LogName => "ActionRecorderBehaviour";

        private static void EnsureSettings()
        {
            if (_settings != null)
                return;

            Settings = new JsonSerializerSettings();
            Settings.TypeNameHandling = TypeNameHandling.All;
        }

        protected override void Awake()
        {
            base.Awake();
            EnsureSettings();

            OwnerMovement = GetComponentInChildren<GridMovementBehaviour>();
            OwnerMovement.Entity.Data.AddComponent(this);

            OwnerMoveset = GetComponentInChildren<MovesetBehaviour>();
            StateMachine = GetComponentInChildren<CharacterStateMachineBehaviour>();
            SubscribeToInput();
        }

        protected virtual void OnEnable()
        {
            SubscribeToInput();

            if (!_matchOverHookRegistered && MatchManagerBehaviour.Instance != null)
            {
                MatchManagerBehaviour.Instance.AddOnMatchOverAction(() => SetRecordEnabled(false));
                _matchOverHookRegistered = true;
            }

            if (_startRecordingOnStart && !CanRecord)
            {
                SetRecordEnabled(true, false);
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            EnsureSettings();
        }

        private void SubscribeToInput()
        {
            if (_subscribedToInput)
                return;

            InputBehaviour.OnInputReceivedEvent += OnInputReceived;
            _subscribedToInput = true;
        }

        protected virtual void OnInputReceived(InputFlag input, EntityDataBehaviour entity)
        {
            if (entity == Entity && !GridGame.IsResimulating)
            {
                RecordNewAction(input);
            }
        }

        protected virtual void RecordNewAction(InputFlag input)
        {
            if (!CanRecord) return;

            ActionRecording recording = new ActionRecording(GridGameManager.FrameNumber, input);
            _recordedActions.Add(recording);
        }

        protected virtual void Save()
        {
            if (_recordedActions.Count == 0) return;

            string recordingPath = Application.persistentDataPath +"/Recordings/"+ RecordingName + ".txt";
            string recordingDirectory = Path.GetDirectoryName(recordingPath);

            if (!Directory.Exists(recordingDirectory))
            {
                Directory.CreateDirectory(recordingDirectory);
            }

            if (!File.Exists(recordingPath))
            {
                FileStream stream = File.Create(recordingPath);
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(recordingPath);
            string json = JsonConvert.SerializeObject(Deck.Seed, Settings);

            writer.WriteLine(json);

            json = JsonConvert.SerializeObject(_recordedActions, Settings);


            writer.Write(json);
            writer.Close();

            Debug.Log($"Recording saved at {recordingPath}");
        }

        protected virtual void OnApplicationQuit()
        {
            StopAndSaveRecordingIfNeeded();
        }

        protected virtual void OnDisable()
        {
            StopAndSaveRecordingIfNeeded();

            if (_subscribedToInput)
            {
                InputBehaviour.OnInputReceivedEvent -= OnInputReceived;
                _subscribedToInput = false;
            }
        }

        protected virtual void OnDestroy()
        {
            StopAndSaveRecordingIfNeeded();

            if (_subscribedToInput)
            {
                InputBehaviour.OnInputReceivedEvent -= OnInputReceived;
                _subscribedToInput = false;
            }
        }

        private void StopAndSaveRecordingIfNeeded()
        {
            if (!CanRecord)
                return;

            SetRecordEnabled(false);
        }

        public static List<ActionRecording> LoadRecording(string recordingName)
        {
            EnsureSettings();
            string recordingPath = Application.persistentDataPath + "/Recordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            List<ActionRecording> recordedActions = new List<ActionRecording>();

            StreamReader reader = new StreamReader(recordingPath);

            Deck.Seed = JsonConvert.DeserializeObject<int>(reader.ReadLine(), Settings);

            recordedActions = JsonConvert.DeserializeObject<List<ActionRecording>>(reader.ReadLine(), Settings);

            reader.Close();

            if (recordedActions.Count == 0)
                return null;

            return recordedActions;
        }

        public virtual void SetRecordEnabled(bool enabled, bool saveLast = true)
        {
            bool wasRecording = CanRecord;
            CanRecord = enabled;

            if (!CanRecord)
            {
                if (saveLast && wasRecording)
                    Save();

                _recordedActions.Clear();
            }
#if UNITY_EDITOR
            if (enabled)
            {
                OnRecordBegin?.Raise(gameObject);
            }
            else
            {
                OnRecordFinish?.Raise(gameObject);
            }
#endif
        }

        public void InitializeRecording(string recordingName, bool startRecordingImmediately)
        {
            _recordingName = recordingName;
            _startRecordingOnStart = startRecordingImmediately;

            if (isActiveAndEnabled)
            {
                SetRecordEnabled(startRecordingImmediately, false);
            }
        }

        public static Dictionary<int, ActionRecording> LoadRecordingDictionary(string recordingName)
        {
            EnsureSettings();
            string recordingPath = Application.persistentDataPath + "/Recordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            List<ActionRecording> recordedActions = new List<ActionRecording>();

            StreamReader reader = new StreamReader(recordingPath);

            Deck.Seed = JsonConvert.DeserializeObject<int>(reader.ReadLine(), Settings);

            recordedActions = JsonConvert.DeserializeObject<List<ActionRecording>>(reader.ReadLine(), Settings);

            reader.Close();

            if (recordedActions.Count == 0)
                return null;

            Dictionary<int, ActionRecording> recordingDictionary = new Dictionary<int, ActionRecording>();

            foreach (ActionRecording recording in recordedActions)
                recordingDictionary.Add(recording.FrameNumber, recording);

            return recordingDictionary;
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                SetRecordEnabled(!CanRecord);
            else if (Keyboard.current.tKey.wasPressedThisFrame)
                SetRecordEnabled(!CanRecord, false);

        }

        public override void Deserialize(BinaryReader br)
        {
            
        }

        /// <summary>
        /// Hashes the serialized action-recorder state, which is currently empty, so
        /// this component can still be included in sync diagnostics consistently.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return null;
        }

        public override void Serialize(BinaryWriter bw)
        {
             
        }
    }
}
