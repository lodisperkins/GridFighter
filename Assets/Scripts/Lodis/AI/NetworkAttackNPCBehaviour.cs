using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.Movement;
using System;
using System.Collections;
using UnityEngine;
using System.IO;
using Types;
using FixedPoints;
using NaughtyAttributes;
using Lodis.GridScripts;

namespace Lodis.AI
{
    public class NetworkAttackNPCBehaviour : SimulationBehaviour
    {
        [SerializeField] private GameObject _character;
        [SerializeField] private NetworkCharacterAIMovementBehaviour _AIMovementBehaviour;
        [Tooltip("Pick the attack this NPC should perform")]
        [SerializeField] private Gameplay.AbilityType _attackType;

        [Tooltip("Sets the amount of time the NPC will wait before attacking again")]
        [SerializeField] private Fixed32 _attackDelay;
        [Tooltip("Sets the value that amplifies the power of strong attacks")]
        [SerializeField] private Fixed32 _attackStrength;
        [Tooltip("The direction on the grid this NPC is looking in. Useful for changing the direction of attacks")]
        [SerializeField] private FVector2 _attackDirection;
        [SerializeField] private bool _isSummon;
        [ShowIf("_isSummon")]
        [SerializeField] private bool _followOwner;
        [ShowIf("_isSummon")]
        [SerializeField] private bool _followOpponent;
        [ShowIf("_isSummon")]
        [Range(-1,2)]
        [SerializeField] private int _followX = -1;


        //---
        private Fixed32 _timeOfLastAttack;
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private int _lastSlot;
        private bool _chargingAttack;
        private Gameplay.MovesetBehaviour _moveset;
        private GridMovementBehaviour _opponentMove;
        private GridMovementBehaviour _ownerMove;
        private GridMovementBehaviour _movementBehaviour;
        private PanelBehaviour _lastTrackedPanel;


        public StateMachine StateMachine { get => _stateMachine; }

        public MovesetBehaviour Moveset { get => _moveset; set => _moveset = value; }

        public KnockbackBehaviour Knockback { get => _knockbackBehaviour; private set => _knockbackBehaviour = value; }

        public FVector2 AttackDirection
        {
            get
            {
                return _attackDirection;
            }
            set
            {
                _attackDirection = value;
            }
        }

        public GameObject Character { get => _character; set => _character = value; }

        public EntityDataBehaviour Owner { get; set; }
        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; private set => _movementBehaviour = value; }

        public override void Serialize(BinaryWriter bw)
        {
            _timeOfLastAttack.Serialize(bw);
            bw.Write(_lastSlot);
            bw.Write(_chargingAttack);
        }

        public override void Deserialize(BinaryReader br)
        {
            _timeOfLastAttack.Deserialize(br);
            _lastSlot = br.ReadInt32();
            _chargingAttack = br.ReadBoolean();
        }

        public override void Init()
        {
            base.Init();

            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
            MovementBehaviour = Character.GetComponent<GridMovementBehaviour>();
            _moveset = Character.GetComponent<Gameplay.MovesetBehaviour>();
        }

        public override void Begin()
        {
            base.Begin();

            if (_followOpponent)
                _opponentMove = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<GridMovementBehaviour>();
            
            _ownerMove = Owner.GetComponent<GridMovementBehaviour>();

            MovementBehaviour.Alignment = _ownerMove.Alignment;
        }

        private void UpdateTargetPanel(PanelBehaviour panel)
        {
            FVector2 targetPanel = panel.Position;

            if (_followX != -1)
            {
                targetPanel.X = _followX;    

                if (_ownerMove.Alignment == GridAlignment.RIGHT)
                {
                    targetPanel.X = GridBehaviour.Instance.GetMirroredPanelAcrossX(targetPanel.X, targetPanel.Y).Position.X;
                }
            }
            else
            {
                targetPanel.X = MovementBehaviour.CurrentPanel.Position.X;
            }

            _AIMovementBehaviour.MoveToLocation(targetPanel);

            _lastTrackedPanel = panel;
        }

        private void UseChargedAttack(AbilityType type)
        {
            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking"))
            {
                Moveset.UseBasicAbility(type, new object[] { _attackStrength, _attackDirection });
            }
            _chargingAttack = false;
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            //Handle movement for summons
            if (_isSummon)
            {
                if ((!_AIMovementBehaviour.ReachedDestination && _lastTrackedPanel != null) || StateMachine.CurrentState == "Attacking")
                    return;

                if (_lastTrackedPanel != _opponentMove.CurrentPanel && _followOpponent)
                {
                    UpdateTargetPanel(_opponentMove.CurrentPanel);
                    return;
                }
                else if (_lastTrackedPanel != _ownerMove.CurrentPanel && _followOwner)
                {
                    UpdateTargetPanel(_ownerMove.CurrentPanel);
                    return;
                }
            }

            //Handle attack timing
            if (GridGame.Time - _timeOfLastAttack < _attackDelay)
                return;

            //Attack based on the ability type selected
            if (_attackType == Gameplay.AbilityType.SPECIAL)
            {
                if (_lastSlot == 0)
                    _lastSlot = 1;
                else
                    _lastSlot = 0;

                Moveset.UseSpecialAbility(_lastSlot, new object[] { _attackStrength, _attackDirection });
            }
            else
            {
                Moveset.UseBasicAbility(_attackType, new object[] { _attackStrength, _attackDirection });
            }

            _timeOfLastAttack = GridGame.Time;
        }
    }
}
