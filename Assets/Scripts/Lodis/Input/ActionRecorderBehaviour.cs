using Lodis.Gameplay;
using Lodis.Movement;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.Input
{
    public class ActionRecording
    {
        public float TimeDelay;
        public float TimeStamp;
        public int ActionID;
        public Vector2 ActionDirection;

        public ActionRecording(float timeDelay, float timeStamp, int actionID, Vector2 actionDirection)
        {
            TimeStamp = timeStamp;
            TimeDelay = timeDelay;
            ActionID = actionID;
            ActionDirection = actionDirection;
        }
    }

    public class ActionRecorderBehaviour : MonoBehaviour
    {
        private List<ActionRecording> _recordedActions = new List<ActionRecording>();
        private GridMovementBehaviour _ownerMovement;
        private MovesetBehaviour _ownerMoveset;
        private CharacterStateMachineBehaviour _stateMachine;
        private float _currentTimeDelay;
        private float _currentTime;
        private static JsonSerializerSettings _settings;
        [SerializeField]
        private string _recordingName;

        // Start is called before the first frame update
        void Start()
        {
            _settings = new JsonSerializerSettings();
            _settings.TypeNameHandling = TypeNameHandling.All;
            _ownerMovement = GetComponent<GridMovementBehaviour>();
            _ownerMoveset = GetComponent<MovesetBehaviour>();
            _stateMachine = GetComponent<CharacterStateMachineBehaviour>();
            _ownerMoveset.OnUseAbility += () => RecordNewAction(_ownerMoveset.LastAbilityInUse.abilityData.ID);

            _ownerMovement.AddOnMoveBeginAction(() =>
            {
                string lastState = _stateMachine.LastState;
                string currentState = _stateMachine.StateMachine.CurrentState;

                if (currentState != "Attacking" && (lastState == "Idle" || lastState == "Moving"))
                {
                    RecordNewAction(-1);
                };
            }
            );
        }

        private void RecordNewAction(int id)
        {
            Vector2 direction = Vector2.zero;

            if (id == -1)
                direction = _ownerMovement.MoveDirection;
            else
                direction = _ownerMoveset.LastAttackDirection;

            ActionRecording recording = new ActionRecording(_currentTimeDelay, _currentTime, id, direction);
            _currentTimeDelay = 0;
            _recordedActions.Add(recording);
        }

        private void Save()
        {
            if (_recordedActions.Count == 0) return;

            string recordingPath = Application.persistentDataPath +"/Recordings/"+ _recordingName + ".txt";
            if (!File.Exists(recordingPath))
            {
                FileStream stream = File.Create(recordingPath);
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(recordingPath);
            string json = JsonConvert.SerializeObject(Deck.Seed, _settings);

            writer.WriteLine(json);

            json = JsonConvert.SerializeObject(_recordedActions, _settings);

            writer.Write(json);
            writer.Close();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public static List<ActionRecording> LoadRecording(string recordingName)
        {
            string recordingPath = Application.persistentDataPath + "/Recordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            List<ActionRecording> recordedActions = new List<ActionRecording>();

            StreamReader reader = new StreamReader(recordingPath);

            Deck.Seed = JsonConvert.DeserializeObject<int>(reader.ReadLine(), _settings);

            recordedActions = JsonConvert.DeserializeObject<List<ActionRecording>>(reader.ReadLine(), _settings);

            reader.Close();

            if (recordedActions.Count == 0)
                return null;

            return recordedActions;
        }

        public static Dictionary<float, ActionRecording> LoadRecordingDictionary(string recordingName)
        {
            string recordingPath = Application.persistentDataPath + "/Recordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            List<ActionRecording> recordedActions = new List<ActionRecording>();

            StreamReader reader = new StreamReader(recordingPath);

            recordedActions = JsonConvert.DeserializeObject<List<ActionRecording>>(reader.ReadToEnd(), _settings);

            reader.Close();

            if (recordedActions.Count == 0)
                return null;

            Dictionary<float, ActionRecording> recordingDictionary = new Dictionary<float, ActionRecording>();

            foreach (ActionRecording recording in recordedActions)
                recordingDictionary.Add(recording.TimeStamp, recording);

            return recordingDictionary;
        }

        // Update is called once per frame
        void Update()
        {
            _currentTime += Time.deltaTime;
            _currentTimeDelay += Time.deltaTime;
        }
    }
}