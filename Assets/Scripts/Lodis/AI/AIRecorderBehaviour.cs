using Assets.Scripts.Lodis.AI;
using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
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

       
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            _actionTree = new DecisionTree(0.98f);
            _actionTree.MaxDecisionsCount = _maxDecisionCount;
            _actionTree.SaveLoadPath = Application.persistentDataPath + "/RecordedDecisionData";
            _actionTree.Load("_" + RecordingName);
            _recordings = Load(RecordingName);

            _knockbackBehaviour = GetComponent<KnockbackBehaviour>();
            _gridPhysics = GetComponent<GridPhysicsBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject);
            _opponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            _opponentKnocback = _opponent.GetComponent<KnockbackBehaviour>();
            _opponentGridPhysics = _opponent.GetComponent<GridPhysicsBehaviour>();
            _opponentMoveset = _opponent.GetComponent<MovesetBehaviour>();

            MatchManagerBehaviour.Instance.AddOnMatchStartAction(AddNewRecording);

            _opponentBarrier = OwnerMovement.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
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
        }

        private Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].RB)
                    averageVelocity += _attacksInRange[i].RB.velocity;

            return averageVelocity /= _attacksInRange.Count;
        }

        private Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                if ((object)physics != null)
                    return physics == null;

                return true;
            });

            for (int i = 0; i < _attacksInRange.Count; i++)
                averagePosition += _attacksInRange[i].gameObject.transform.position;

            return averagePosition /= _attacksInRange.Count;
        }

        private void OnDestroy()
        {
            if (!Application.isEditor) return;
            _actionTree?.Save("_" + RecordingName);
        }

        protected override void Save()
        {
            if (_recordings.Length == 0) return;

            string recordingPath = Application.persistentDataPath + "/AIRecordings/" + RecordingName + ".txt";
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
            if (!File.Exists(Application.persistentDataPath + "/AIRecordings/" + recordingName + ".txt"))
                return null;

            StreamReader reader = new StreamReader(Application.persistentDataPath + "/AIRecordings/" + recordingName + ".txt");
            List<ActionNode>[] recordings = JsonConvert.DeserializeObject<List<ActionNode>[]>(reader.ReadToEnd(), Settings);

            Debug.Log("Loaded " + recordings.Length + "recordings");
            reader.Close();

            return recordings;
        }

        protected override void RecordNewAction(int id)
        {
            ActionNode action = new ActionNode(null, null);

            action.CurrentState = StateMachine.StateMachine.CurrentState;

            action.AlignmentX = (int)OwnerMovement.GetAlignmentX();
            action.AveragePosition = GetAveragePosition();
            action.AverageVelocity = GetAverageVelocity();
            action.MoveDirection = OwnerMovement.MoveDirection;
            action.IsGrounded = _gridPhysics.IsGrounded;
            action.Energy = OwnerMoveset.Energy;

            action.CurrentAbilityID = id;
            action.IsAttacking = id != -1;

            action.Health = _knockbackBehaviour.Health;
            action.BarrierHealth = _ownerBarrier.Health;

            action.OwnerToTarget = _opponent.transform.position - transform.position;

            action.OpponentVelocity = _opponentGridPhysics.LastVelocity;
            action.OpponentEnergy = _opponentMoveset.Energy;
            action.OpponentMoveDirection = _opponentMove.MoveDirection;
            action.OpponentHealth = _opponentKnocback.Health;
            action.OpponentBarrierHealth = _opponentBarrier.Health;

            action.TimeStamp = CurrentTime;
            action.TimeDelay = CurrentTimeDelay;
            CurrentTimeDelay = 0;
            _actionTree.AddDecision(action);
            _recordings[_recordings.Length - 1].Add(action);
        }
    }
}