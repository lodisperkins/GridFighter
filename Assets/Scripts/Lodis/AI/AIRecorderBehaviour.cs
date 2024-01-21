using Assets.Scripts.Lodis.AI;
using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.UI;
using Lodis.Utility;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.AI
{
    public class AIRecorderBehaviour : ActionRecorderBehaviour
    {
        private DecisionTree _actionTree;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private List<HitColliderBehaviour> _attacksInRange = new List<HitColliderBehaviour>();
        private List<ActionNode>[] _recordings; 

        private GameObject _opponent;
        private GridMovementBehaviour _opponentMove;
        private KnockbackBehaviour _opponentKnocback;

        [SerializeField]
        private int _maxDecisionCount;
        [SerializeField]
        private float _timeDelayMax;
        private GridPhysicsBehaviour _gridPhysics;
        private GridPhysicsBehaviour _opponentGridPhysics;
        private IntVariable _playerID;

        public Vector2 MovePosition;

        private Vector3 _opponentVelocity;
        private Vector3 _opponentDisplacement;
        private float _opponentHealth;
        private List<HitColliderBehaviour> _lastAttacksInRange;
        private RingBarrierBehaviour _ownerBarrier;
        private RingBarrierBehaviour _opponentBarrier;
        private MovesetBehaviour _opponentMoveset;
        private ActionNode _currentSituation;
        private InputBehaviour _input;
        private CharacterStateMachineBehaviour _opponentStateMachine;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            _actionTree = new DecisionTree(0.98f);
            _actionTree.MaxDecisionsCount = _maxDecisionCount;
            _recordings = Load(RecordingName);
            UpdateDecisions();

            _knockbackBehaviour = GetComponent<KnockbackBehaviour>();
            _gridPhysics = GetComponent<GridPhysicsBehaviour>();
            _input = GetComponentInParent<InputBehaviour>();
            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject);
            _opponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            _opponentKnocback = _opponent.GetComponent<KnockbackBehaviour>();
            _opponentGridPhysics = _opponent.GetComponent<GridPhysicsBehaviour>();
            _opponentMoveset = _opponent.GetComponent<MovesetBehaviour>();
            _opponentStateMachine = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject).GetComponent<CharacterStateMachineBehaviour>();

            MatchManagerBehaviour.Instance.AddOnMatchStartAction(AddNewRecording);
            _knockbackBehaviour.LandingScript.AddOnRecoverAction(AddNewRecording);

            _opponentBarrier = OwnerMovement.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;

            _opponentMoveset.OnUseAbility += () => CurrentTimeDelay = 0;

            UpdateSituationNode();
        }

        private void UpdateDecisions()
        {
            if (_recordings == null || _recordings.Length == 0)
                return;

            foreach (List<ActionNode> recording in _recordings)
            {
                for (int i = 0; i < recording.Count; i++)
                {
                    _actionTree.AddDecision(recording[i]);
                }
            }
        }

        private void AddNewRecording()
        {
            if (_recordings == null)
                _recordings = new List<ActionNode>[0];

            List<ActionNode>[] temp = new List<ActionNode>[_recordings.Length + 1];

            for (int i = 0; i < _recordings.Length; i++)
            {
                temp[i] = _recordings[i];
            }

            temp[_recordings.Length] = new List<ActionNode>();
            _recordings = temp;

            CurrentTimeDelay = 0;
            CurrentTime = 0;
        }

        private Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);

            if (_attacksInRange == null || _attacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].RB)
                    averageVelocity += _attacksInRange[i].RB.velocity;

            return averageVelocity;
        }

        private Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);

            if (_attacksInRange == null || _attacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                averagePosition += _attacksInRange[i].gameObject.transform.position - transform.position;

            return averagePosition;
        }

        protected override void Save()
        {
            if (_recordings?.Length == 0) return;


            string recordingPath = "";

            if (Application.isEditor)
                recordingPath = Application.dataPath +"/StreamingAssets/" + "/AIRecordings/" + RecordingName + ".txt";
            else
                recordingPath = Application.streamingAssetsPath + "/AIRecordings/" + RecordingName + ".txt";

            if (!File.Exists(recordingPath))
            {
                FileStream stream = File.Create(recordingPath);
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(recordingPath);
            string json = JsonConvert.SerializeObject(_recordings, Settings);

            writer.Write(json);
            writer.Close();
        }

        public static List<ActionNode>[] Load(string recordingName)
        {

            string recordingPath = "";

            if (Application.isEditor)
                recordingPath = Application.dataPath + "/StreamingAssets/" + "/AIRecordings/" + recordingName + ".txt";
            else
                recordingPath = Application.streamingAssetsPath + "/AIRecordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            StreamReader reader = new StreamReader(recordingPath);
            List<ActionNode>[] recordings = JsonConvert.DeserializeObject<List<ActionNode>[]>(reader.ReadToEnd(), Settings);

            Debug.Log("Loaded " + recordings.Length + "recordings");
            reader.Close();

            return recordings;
        }

        public static List<ActionNode>[] Load(string recordingName, MovesetBehaviour ownerMoveset, int limit = -1)
        {

            string recordingPath = "";

            if (Application.isEditor)
                recordingPath = Application.dataPath + "/StreamingAssets" + "/AIRecordings/" + recordingName + ".txt";
            else
                recordingPath = Application.streamingAssetsPath + "/AIRecordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            StreamReader reader = new StreamReader(recordingPath);
            List<ActionNode>[] recordingData = JsonConvert.DeserializeObject<List<ActionNode>[]>(reader.ReadToEnd(), Settings);

            List<ActionNode>[] recordings = new List<ActionNode>[0];

            bool recordingValid = false;

            int recordingMax = limit == -1.0f ? recordingData.Length : limit;

            for (int i = 0; i < recordingMax; i++)
            {
                recordingValid = false;

                List<ActionNode> recording = new List<ActionNode>();

                List<ActionNode> currentData = recordingData[i];

                for (int j = 0; j < currentData.Count; j++)
                {
                    int currentAction = currentData[j].CurrentAbilityID;

                    if (currentAction > 0 && !ownerMoveset.SpecialDeckRef.Contains(currentAction) && !ownerMoveset.NormalDeckRef.Contains(currentAction))
                        break;

                    recording.Add(currentData[j]);
                    recordingValid = true;
                }

                if (recordingValid)
                    recordings.Add(recording);
            }

            Debug.Log("Loaded " + recordingData.Length + "recordings");
            reader.Close();

            return recordingData;
        }

        private void UpdateSituationNode()
        {
            _currentSituation = new ActionNode(null, null);

            _currentSituation.CurrentState = StateMachine.StateMachine.CurrentState;

            _currentSituation.AlignmentX = (int)OwnerMovement.GetAlignmentX();
            _currentSituation.AverageHitBoxOffset = GetAveragePosition();
            _currentSituation.AverageVelocity = GetAverageVelocity();
            _currentSituation.MoveDirection = OwnerMovement.MoveDirection;
            _currentSituation.IsGrounded = _gridPhysics.IsGrounded;

            if (OwnerMoveset.AbilityInUse)
            {
                _currentSituation.AttackDirection = OwnerMoveset.LastAttackDirection;
                _currentSituation.Energy = OwnerMoveset.Energy;
                _currentSituation.CurrentAbilityID = OwnerMoveset.LastAbilityInUse.abilityData.ID;
            }
            else
            {
                _currentSituation.CurrentAbilityID = -1;
            }

            _currentSituation.OpponentState = _opponentStateMachine.StateMachine.CurrentState;

            _currentSituation.Health = _knockbackBehaviour.Health;
            _currentSituation.BarrierHealth = _ownerBarrier.Health;

            _currentSituation.PanelPosition = OwnerMovement.Position;
            _currentSituation.OwnerToTarget = _opponent.transform.position - transform.position;

            _currentSituation.OpponentVelocity = _opponentGridPhysics.LastVelocity;
            _currentSituation.OpponentEnergy = _opponentMoveset.Energy;
            _currentSituation.OpponentMoveDirection = _opponentMove.MoveDirection;
            _currentSituation.OpponentHealth = _opponentKnocback.Health;
            _currentSituation.OpponentBarrierHealth = _opponentBarrier.Health;
            _currentSituation.MatchTimeRemaining = MatchTimerBehaviour.Instance.MatchTimeRemaining;
        }

        protected override void RecordNewAction(int id)
        {
            if (!CanRecord)
                return;

            UpdateSituationNode();
            ActionNode action = _currentSituation.GetShallowCopy();

            action.TimeStamp = CurrentTime;
            action.TimeDelay = CurrentTimeDelay;

            CurrentTimeDelay = 0;
            //_actionTree.AddDecision(action);
            _recordings[_recordings.Length - 1].Add(action);

        }

        protected override void Update()
        {
            base.Update();
            if (CurrentTimeDelay >= _timeDelayMax && _timeDelayMax > 0 && CanRecord)
            {
                CurrentTimeDelay = 0;
            }
        }
    }
}