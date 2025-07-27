using Assets.Scripts.Lodis.AI;
using FixedPoints;
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
using System.Linq;
using System.Threading.Tasks;
using Types;
using UnityEngine;

namespace Lodis.AI
{
    public class ActionPlaybackInfo
    {
        public ActionNode CurrentSituation = new ActionNode(null, null);
        public List<ActionNode> Recording = new List<ActionNode>();
        public List<ActionNode> SpecialAttackNodes = new List<ActionNode>();

        public void InitSpecialNodes()
        {
            foreach (ActionNode action in Recording)
            {
                if (action.InputAction.HasFlag(InputFlag.Special1) || action.InputAction.HasFlag(InputFlag.Special2) && !SpecialAttackNodes.Contains(action))
                {
                    SpecialAttackNodes.Add(action);
                }
            }

            if (SpecialAttackNodes.Count > 1)
                SpecialAttackNodes.RemoveRange(1, SpecialAttackNodes.Count - 1);
        }

        public bool CheckCanPerformSpecials(MovesetBehaviour moveset)
        {
            if (SpecialAttackNodes == null || SpecialAttackNodes.Count == 0)
            {
                return true;
            }

            int currentIndex = 0;

            ActionNode action = SpecialAttackNodes[currentIndex];

            //If the first action is not in the current hand, we can't perform the action.
            if (!moveset.CheckIfAbilityIDInCurrentSlots(action.CurrentAbilityID))
                return false;

            Ability abilityInSlot = moveset.GetAbilityInCurrentSlotByID(action.CurrentAbilityID);

            if (abilityInSlot.abilityData.EnergyCost > moveset.Energy)
                return false; //If the first action is not in the current hand and isn't up next, we can't perform the action.

            //Increment the index to move on to the next special ability that we want to use.
            currentIndex++;

            if (currentIndex >= SpecialAttackNodes.Count)
                return true; //If we are at the end of the list, we can perform the action.

            action = SpecialAttackNodes[currentIndex];

            //If the second action is not in the current hand and isnt up next, we can't perform the action.
            if (!moveset.CheckIfAbilityIDInCurrentSlots(action.CurrentAbilityID) && moveset.NextAbilitySlot.abilityData.ID != action.CurrentAbilityID)
                return false;

            //Increment the index to move on to the next special ability that we want to use.
            currentIndex++;

            if (currentIndex >= SpecialAttackNodes.Count)
                return true; //If we are at the end of the list, we can perform the action.

            action = SpecialAttackNodes[currentIndex];

            //Go through the rest of the abilities in the deck in order to determine if we can perform the action.
            //Starting at the end because the stack pops from the back.
            for (int i = moveset.SpecialDeck.Count - 1; i >= 0; i--)
            {
                Ability ability = moveset.SpecialDeck[i];
                if (ability.abilityData.ID != action.CurrentAbilityID)
                {
                    return false;
                }

                currentIndex++;

                if (currentIndex >= SpecialAttackNodes.Count)
                    break;

                action = SpecialAttackNodes[currentIndex];
            }

            return true;
        }

    }

    public class AIRecorderBehaviour : ActionRecorderBehaviour
    {
       

        private DecisionTree _actionTree;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private List<HitColliderBehaviour> _attacksInRange = new List<HitColliderBehaviour>();
        private ActionPlaybackInfo[] _recordings; 

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
        private float _recordingConfidenceThreshold = 0.5f;
        private bool _opponentHitRecently;
        private HitColliderBehaviour _lastHit;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            _actionTree = new DecisionTree(0.98f);
            _actionTree.MaxDecisionsCount = _maxDecisionCount;
            _recordings = Load(RecordingName);
            //_recordings = new ActionPlaybackInfo[0];

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
            _opponentKnocback.AddOnTakeDamageTempAction(OnOpponentHit);

            //MatchManagerBehaviour.Instance.AddOnMatchStartAction(AddNewRecording);
            //_knockbackBehaviour.LandingScript.AddOnRecoverAction(AddNewRecording);

            _opponentBarrier = OwnerMovement.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;

            _opponentMoveset.OnUseAbility += () => CurrentTimeDelay = 0;

            UpdateSituationNode();
        }

        private void OnOpponentHit()
        {
            _opponentHitRecently = true;
            _lastHit = _opponentKnocback.LastCollider;
        }


        private void UpdateDecisions()
        {
            if (_recordings == null || _recordings.Length == 0)
                return;

            foreach (ActionPlaybackInfo recording in _recordings)
            {
                for (int i = 0; i < recording.Recording.Count; i++)
                {
                    _actionTree.AddDecision(recording.Recording[i]);
                }
            }
        }

        private void RemoveLastRecording()
        {
            if (_recordings == null || _recordings.Length == 0)
                return;

            ActionPlaybackInfo[] temp = new ActionPlaybackInfo[_recordings.Length - 1];

            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = _recordings[i];
            }

            _recordings = temp;
        }

        private void AddNewRecording()
        {
            if (_recordings == null)
                _recordings = new ActionPlaybackInfo[0];

            ActionPlaybackInfo[] temp = new ActionPlaybackInfo[_recordings.Length + 1];

            for (int i = 0; i < _recordings.Length; i++)
            {
                temp[i] = _recordings[i];
            }

            temp[_recordings.Length] = new ActionPlaybackInfo();
            _recordings = temp;

            CurrentTimeDelay = 0;
            CurrentTime = 0;
        }

        private FVector3 GetAverageVelocity()
        {
            FVector3 averageVelocity = FVector3.Zero;

            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);

            if (_attacksInRange == null || _attacksInRange.Count == 0)
                return FVector3.Zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].GridPhysics)
                    averageVelocity += _attacksInRange[i].GridPhysics.Velocity;

            return averageVelocity;
        }

        private FVector3 GetAveragePosition()
        {
            FVector3 averagePosition = FVector3.Zero;

            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);

            if (_attacksInRange == null || _attacksInRange.Count == 0)
                return FVector3.Zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                averagePosition += _attacksInRange[i].FixedTransform.WorldPosition - (FVector3)transform.position;

            return averagePosition;
        }

        protected void CleanRecordings()
        {
            if (_recordings == null || _recordings.Length == 0)
                return;

            List<ActionPlaybackInfo> validRecordings = new List<ActionPlaybackInfo>();

            foreach (var recording in _recordings)
            {
                // Find the first and last meaningful action indices  
                int firstMeaningfulIndex = recording.Recording.FindIndex(action => action.InputAction != InputFlag.NONE);
                int lastMeaningfulIndex = recording.Recording.FindLastIndex(action => action.InputAction != InputFlag.NONE);

                // If there are no meaningful actions, skip this recording  
                if (firstMeaningfulIndex == -1 || lastMeaningfulIndex == -1)
                    continue;

                // Trim the recording to only include meaningful actions  
                recording.Recording = recording.Recording.GetRange(firstMeaningfulIndex, lastMeaningfulIndex - firstMeaningfulIndex + 1);

                validRecordings.Add(recording);
            }

            _recordings = validRecordings.ToArray();
        }

        protected override void Save()
        {
            if (_recordings?.Length == 0) return;

            CleanRecordings();

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

            Debug.Log($"Recording saved at {recordingPath}");
        }

        public static ActionPlaybackInfo[] Load(string recordingName)
        {

            string recordingPath = "";

            if (Application.isEditor)
                recordingPath = Application.dataPath + "/StreamingAssets/" + "/AIRecordings/" + recordingName + ".txt";
            else
                recordingPath = Application.streamingAssetsPath + "/AIRecordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            StreamReader reader = new StreamReader(recordingPath);
            ActionPlaybackInfo[] recordings = JsonConvert.DeserializeObject<ActionPlaybackInfo[]>(reader.ReadToEnd(), Settings);

            Debug.Log("Loaded " + recordings?.Length + "recordings");
            reader.Close();

            return recordings;
        }

        public static async Task<ActionPlaybackInfo[]> LoadAsync(string recordingName, int limit = -1)
        {
            string recordingPath = "";

            if (Application.isEditor)
                recordingPath = Application.dataPath + "/StreamingAssets" + "/AIRecordings/" + recordingName + ".txt";
            else
                recordingPath = Application.streamingAssetsPath + "/AIRecordings/" + recordingName + ".txt";

            if (!File.Exists(recordingPath))
                return null;

            string fileContent;
            using (StreamReader reader = new StreamReader(recordingPath))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            ActionPlaybackInfo[] recordingData = await Task.Run(() => JsonConvert.DeserializeObject<ActionPlaybackInfo[]>(fileContent, Settings));

            await Task.Run(() =>
            {
                if (recordingData == null || recordingData.Length == 0)
                    return;

                foreach (var recording in recordingData)
                {
                    recording.InitSpecialNodes();
                }
            });

            //Old code to check if the character had the action. Back when custom character recorded data was loaded.
            //List<ActionPlaybackInfo> recordings = new List<ActionPlaybackInfo>();

            //int recordingMax = limit == -1 ? recordingData.Length : limit;

            //for (int i = 0; i < recordingMax; i++)
            //{
            //    bool recordingValid = false;

            //    ActionPlaybackInfo recording = new ActionPlaybackInfo();

            //    ActionPlaybackInfo currentData = recordingData[i];

            //    for (int j = 0; j < currentData.Recording.Count; j++)
            //    {
            //        int currentAction = currentData.Recording[j].CurrentAbilityID;

            //        if (currentAction > 0 && !ownerMoveset.SpecialDeckRef.Contains(currentAction) && !ownerMoveset.NormalDeckRef.Contains(currentAction))
            //            break;

            //        recording.Recording.Add(currentData.Recording[j]);
            //        recordingValid = true;
            //    }

            //    if (recordingValid)
            //        recordings.Add(recording);
            //}

            Debug.Log("Loaded " + recordingData.Length + " recordings");

            return recordingData;
        }

        private void UpdateSituationNode()
        {
            _currentSituation = new ActionNode(null, null);

            _currentSituation.CurrentState = StateMachine.StateMachine.CurrentState;

            _currentSituation.AlignmentX = (int)OwnerMovement.GetAlignmentX();
            _currentSituation.AverageHitBoxOffset = (Vector3)GetAveragePosition();
            _currentSituation.AverageVelocity = (Vector3)GetAverageVelocity();
            _currentSituation.MoveDirection = (Vector2)OwnerMovement.MoveDirection;
            _currentSituation.IsGrounded = _gridPhysics.IsGrounded;

            if (OwnerMoveset.AbilityInUse)
            {
                _currentSituation.AttackDirection = (Vector2)OwnerMoveset.LastAttackDirection;
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

            _currentSituation.PanelPosition = (Vector2)OwnerMovement.Position;
            _currentSituation.OpponentPanelPosition = (Vector2)_opponentMove.Position;

            _currentSituation.OwnerToTarget = _opponent.transform.position - transform.position;

            _currentSituation.OpponentVelocity = (Vector3)_opponentGridPhysics.Velocity;
            _currentSituation.OpponentEnergy = _opponentMoveset.Energy;
            _currentSituation.OpponentMoveDirection = (Vector2)_opponentMove.MoveDirection;
            _currentSituation.OpponentHealth = _opponentKnocback.Health;
            _currentSituation.OpponentBarrierHealth = _opponentBarrier.Health;
            _currentSituation.MatchTimeRemaining = MatchTimerBehaviour.Instance.MatchTimeRemaining;
        }

        protected override void RecordNewAction(InputFlag input)
        {
            if (_recordings.Length == 0 && input == InputFlag.NONE)
                return;

            if (!CanRecord)
                return;

            UpdateSituationNode();
            ActionNode action = _currentSituation.GetShallowCopy();

            action.IsSpecialAttack = input.HasFlag(InputFlag.Special1 | InputFlag.Special2);

            if (input.HasFlag(InputFlag.Special1))
            {
                Ability slot1Ability = OwnerMoveset.GetAbilityInCurrentSlot(0);

                if (slot1Ability != null)
                    action.CurrentAbilityID = slot1Ability.abilityData.ID;
                else
                    action.CurrentAbilityID = -1;
            }
            else if (input.HasFlag(InputFlag.Special2))
            {
                Ability slot2Ability = OwnerMoveset.GetAbilityInCurrentSlot(1);

                if (slot2Ability != null)
                    action.CurrentAbilityID = slot2Ability.abilityData.ID;
                else
                    action.CurrentAbilityID = -1;
            }
            else
            {
                action.CurrentAbilityID = -1;
            }

            action.InputAction = input;

            _recordings[_recordings.Length - 1].Recording.Add(action);

            if (action.IsSpecialAttack)
            {
                List<ActionNode> specialNodes = _recordings[_recordings.Length - 1].SpecialAttackNodes;

                if (specialNodes.Count > 0)
                {
                    ActionNode lastSpecialNode = specialNodes[specialNodes.Count - 1];

                    if (lastSpecialNode.CurrentAbilityID != action.CurrentAbilityID)
                    {
                        _recordings[_recordings.Length - 1].SpecialAttackNodes.Add(action);
                    }
                }
                else
                {
                    _recordings[_recordings.Length - 1].SpecialAttackNodes.Add(action);
                }
            }

            CurrentTimeDelay = 0;

            Debug.Log("Recorded action");

        }

        private void OnDisable()
        {
            Save();
        }

        public override void SetRecordEnabled(bool enabled, bool saveLast = true)
        {
            base.SetRecordEnabled(enabled, saveLast);
            
            if (!enabled && !saveLast)
            {
                RemoveLastRecording();
            }

            if (enabled)
            {
                AddNewRecording();
            }
        }

        protected override void Update()
        {
            base.Update();

            if (CanRecord)
            {
                RecordNewAction(_input.Flags);
            }
        }
    }
}