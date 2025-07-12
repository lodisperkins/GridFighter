using CustomEventSystem;
using FixedPoints;
using Lodis.Gameplay;
using Lodis.Movement;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Input
{

    public class ActionRecording
    {
        public float TimeDelay;
        public float TimeStamp;
        public InputFlag InputAction;
        public FVector2 ActionDirection;

        public ActionRecording(float timeDelay, float timeStamp, InputFlag actionID)
        {
            TimeStamp = timeStamp;
            TimeDelay = timeDelay;
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

        private float _currentTimeDelay;
        private float _currentTime;

        private static JsonSerializerSettings _settings;

        public string RecordingName { get => _recordingName; }
        public GridMovementBehaviour OwnerMovement { get => _ownerMovement; set => _ownerMovement = value; }
        public MovesetBehaviour OwnerMoveset { get => _ownerMoveset; set => _ownerMoveset = value; }
        public CharacterStateMachineBehaviour StateMachine { get => _stateMachine; set => _stateMachine = value; }
        public static JsonSerializerSettings Settings { get => _settings; set => _settings = value; }
        public float CurrentTime { get => _currentTime; protected set => _currentTime = value; }
        public float CurrentTimeDelay { get => _currentTimeDelay; protected set => _currentTimeDelay = value; }
        public bool CanRecord { get => _canRecord; private set => _canRecord = value; }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            Settings = new JsonSerializerSettings();
            Settings.TypeNameHandling = TypeNameHandling.All;

            OwnerMovement = GetComponent<GridMovementBehaviour>();
            OwnerMoveset = GetComponent<MovesetBehaviour>();
            StateMachine = GetComponent<CharacterStateMachineBehaviour>();

            InputBehaviour.OnInputReceivedEvent += OnInputReceived;

            //--old way of recording actions
            //OwnerMoveset.OnUseAbility += () => RecordNewAction(OwnerMoveset.LastAbilityInUse.abilityData.ID);

            //OwnerMoveset.AddOnManualShuffleAction(() => RecordNewAction(-2));

            //OwnerMovement.AddOnMoveBeginAction(() =>
            //{
            //    string lastState = StateMachine.LastState;
            //    string currentState = StateMachine.StateMachine.CurrentState;

            //    if (currentState != "Attacking" && (lastState == "Idle" || lastState == "Moving"))
            //    {
            //        RecordNewAction(-1);
            //    };
            //}
            //);

            //MatchManagerBehaviour.Instance.AddOnMatchStartAction(() => CanRecord = true);
            MatchManagerBehaviour.Instance.AddOnMatchOverAction(() => CanRecord = false);
        }

        protected virtual void OnInputReceived(InputFlag input, EntityDataBehaviour entity)
        {
            return;
            if (entity == Entity)
            {
                RecordNewAction(input);
            }
        }

        protected virtual void RecordNewAction(InputFlag input)
        {
            if (!CanRecord) return;

            float delay = CurrentTimeDelay;
            float stamp = _currentTime;

            ActionRecording recording = new ActionRecording(delay, stamp, input);
            CurrentTimeDelay = 0;
            _recordedActions.Add(recording);
        }

        protected virtual void Save()
        {
            if (_recordedActions.Count == 0) return;

            string recordingPath = Application.persistentDataPath +"/Recordings/"+ RecordingName + ".txt";
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
            if (CanRecord)
                Save();
        }

        public static List<ActionRecording> LoadRecording(string recordingName)
        {
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
            CurrentTime = 0;
            CurrentTimeDelay = 0;
            CanRecord = enabled;

            if (!CanRecord)
            {
                //if (saveLast)
                //    Save();

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

        public static Dictionary<float, ActionRecording> LoadRecordingDictionary(string recordingName)
        {
            string recordingPath = Application.persistentDataPath + "/Recordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            List<ActionRecording> recordedActions = new List<ActionRecording>();

            StreamReader reader = new StreamReader(recordingPath);

            recordedActions = JsonConvert.DeserializeObject<List<ActionRecording>>(reader.ReadToEnd(), Settings);

            reader.Close();

            if (recordedActions.Count == 0)
                return null;

            Dictionary<float, ActionRecording> recordingDictionary = new Dictionary<float, ActionRecording>();

            foreach (ActionRecording recording in recordedActions)
                recordingDictionary.Add(recording.TimeStamp, recording);

            return recordingDictionary;
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                SetRecordEnabled(!CanRecord);
            else if (Keyboard.current.tKey.wasPressedThisFrame)
                SetRecordEnabled(!CanRecord, false);

            if (!CanRecord)
                return;

            CurrentTime += Time.deltaTime;
            CurrentTimeDelay += Time.deltaTime;

        }

        public override void Serialize(BinaryWriter bw)
        {
            throw new System.NotImplementedException();
        }

        public override void Deserialize(BinaryReader br)
        {
            throw new System.NotImplementedException();
        }
    }
}