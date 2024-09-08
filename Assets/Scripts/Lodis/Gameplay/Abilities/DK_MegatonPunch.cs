using FixedPoints;
using Lodis.FX;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_MegatonPunch : Ability
    {
        private float _distance;
        private bool _comboStarted;
        private Movement.GridMovementBehaviour _opponentMovement;
        private AnimationClip _comboClip;
        private float _slowMotionTimeScale;
        private float _slowMotionTime;
        private TimedAction _endTimer;
        private KnockbackBehaviour _opponentKnockback;
        private bool _landedFirstHit = false;
        private bool _canCheckMiss;
        private List<HitColliderBehaviour> _colliders = new List<HitColliderBehaviour>();
        private GameObject _chargeEffect;
        private GameObject _chargeEffectRef;
        private MovesetBehaviour _opponentMoveset;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _distance = abilityData.GetCustomStatValue("Distance");
            abilityData.GetAdditionalAnimation(0, out _comboClip);


            _chargeEffectRef = abilityData.Effects[0];
            _opponentMoveset = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<MovesetBehaviour>();
            OwnerAnimationScript.AddEventListener("Punch1", () => 
            {
                SpawnCollider(0);
                OwnerVoiceScript.PlayLightAttackSound();
                CameraBehaviour.Instance.AlignmentFocus = OwnerMoveScript.Alignment;
                CameraBehaviour.Instance.ZoomAmount = 2;
                CameraBehaviour.Instance.ClampX = false;

            });

            OwnerAnimationScript.AddEventListener("Punch2", () => 
            {
                if (_landedFirstHit)
                {
                    SpawnCollider(0);
                    OwnerVoiceScript.PlayLightAttackSound();
                    CameraBehaviour.Instance.ZoomAmount = 3;
                }
                else
                    EndAbility();
            });
            OwnerAnimationScript.AddEventListener("Punch3", () =>
            {
                SpawnCollider(1);
                OwnerVoiceScript.PlayLightAttackSound();
                CameraBehaviour.Instance.ZoomAmount = 4;

            });

            _slowMotionTimeScale = abilityData.GetCustomStatValue("SlowMotionTimeScale");
            _slowMotionTime = abilityData.GetCustomStatValue("SlowMotionTime");

            OwnerAnimationScript.AddEventListener("SlowMotionStart", () =>
            {
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(false);
                SoundManagerBehaviour.Instance.PlaySound(abilityData.Sounds[0], 3);
                Transform effectSpawn = OwnerMoveScript.Alignment == GridAlignment.LEFT ? OwnerMoveset.RightMeleeSpawns[1] : OwnerMoveset.LeftMeleeSpawns[1];

                _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[1], effectSpawn, true);
                MatchManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale, 0.01f, _slowMotionTime);

                CameraBehaviour.Instance.ZoomAmount = 4.2f;
            });


            OwnerAnimationScript.AddEventListener("Punch4", () =>
            {
                SpawnCollider(2);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);
                _endTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => EndAbility(), TimedActionCountType.SCALEDTIME, abilityData.recoverTime);
                OwnerVoiceScript.PlayHeavyAttackSound();

                CameraBehaviour.Instance.ZoomAmount = 1;
                CameraBehaviour.ShakeBehaviour.ShakeRotation(1, 2, 90);
            });
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _comboStarted = false;
            _landedFirstHit = false;

            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<Movement.GridMovementBehaviour>();
            _opponentKnockback = _opponentMovement.GetComponent<KnockbackBehaviour>();
            TimeCountType = TimedActionCountType.UNSCALEDTIME;

            FXManagerBehaviour.Instance.StartSuperMoveVisual(BlackBoardBehaviour.Instance.GetIDFromPlayer(Owner), abilityData.startUpTime);
            //Spawn the the holding effect.

            Transform effectSpawn = OwnerMoveScript.Alignment == GridAlignment.LEFT ? OwnerMoveset.RightMeleeSpawns[1] : OwnerMoveset.LeftMeleeSpawns[1];

            OwnerKnockBackScript.SetIntagibilityByCondition(condition => CurrentAbilityPhase != AbilityPhase.STARTUP);

            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef, effectSpawn, true);
            OwnerKnockBackScript.IgnoreAdjustedGravity(arguments => !InUse);
            _opponentKnockback.SetDamageableAbilityID(abilityData.ID, arguments => !InUse);
            //RoutineBehaviour.Instance.StartNewConditionAction(args => ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect), condition => !InUse || CurrentAbilityPhase != AbilityPhase.STARTUP);
        }

        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);
            _chargeEffect.transform.parent = null;
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;

            OwnerMoveScript.SnapToTarget();
            OwnerMoveScript.MoveToPanel(OwnerMoveScript.Position + _distance * FVector2.Right * OwnerMoveScript.GetAlignmentX(), false, GridScripts.GridAlignment.ANY, true, false, true);

            OwnerMoveScript.AddOnMoveEndTempAction(EndAbility);
        }

        private void StartCombo()
        {
            PauseAbilityTimer();

            _comboStarted = true;
            OwnerMoveScript.DisableMovement(condition => !_comboStarted, false, true);

            OwnerAnimationScript.PlayAnimation(_comboClip, 1, true, true);

            _opponentKnockback.CancelHitStun();
            _opponentKnockback.Physics.StopVelocity();
            _opponentKnockback.Physics.CancelFreeze();
        }

        public void SpawnCollider(int colliderIndex)
        {
            FVector3 spawnPosition = Owner.FixedTransform.WorldPosition + (FVector3.Right * OwnerMoveScript.GetAlignmentX()) + FVector3.Up;
            HitColliderBehaviour hitColliderBehaviour = HitColliderSpawner.SpawnCollider(spawnPosition, 1, 1, GetColliderData(colliderIndex), Owner);

            hitColliderBehaviour.ColliderInfo.OnHit += collision =>
            {
                HealthBehaviour health = collision.OtherEntity.GetComponent<HealthBehaviour>();

                if (!health.IsInvincible && !health.IsIntangible)
                    _landedFirstHit = true;


                OwnerKnockBackScript.SetIntagibilityByCondition(condition => !InUse);
                MatchManagerBehaviour.Instance.SuperInUse = true;
            };

            if (colliderIndex == 2)
            {
                hitColliderBehaviour.ColliderInfo.OnHit += args =>
                {
                    ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
                };
            }

            _colliders.Add(hitColliderBehaviour);
        }

        private void DestroyAllColliders()
        {
            foreach (HitColliderBehaviour collider in _colliders)
            {
                if (!collider)
                    continue;

                ObjectPoolBehaviour.Instance.ReturnGameObject(collider.gameObject);
            }

            _colliders.Clear();
        }

        public override void FixedUpdate()
        {
            if (CurrentAbilityPhase != AbilityPhase.ACTIVE)
                return;

            float distance = Vector3.Distance(Owner.transform.position, _opponentMovement.transform.position);

            if (distance <= 1f && !_comboStarted && !_opponentKnockback.IsInvincible && !_opponentKnockback.IsIntangible)
            {
                StartCombo();
            }
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            _chargeEffect.transform.parent = null;
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            _comboStarted = false;
            PanelBehaviour panel;
            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(Owner.transform.position, out panel);

            OwnerMoveScript.Position = panel.Position;
            OwnerMoveScript.EnableMovement();
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            RoutineBehaviour.Instance.StopAction(_endTimer);

            if (_chargeEffect)
                _chargeEffect.transform.parent = null;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            DestroyAllColliders();

            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;
            CameraBehaviour.Instance.ClampX = true;
            MatchManagerBehaviour.Instance.SuperInUse = false;
            OwnerKnockBackScript.IsIntangible = false;
            OwnerMoveScript.CanCancelMovement = false;
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            RoutineBehaviour.Instance.StopAction(_endTimer);

            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;

            if (_chargeEffect)
                _chargeEffect.transform.parent = null;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            DestroyAllColliders();
            OwnerMoveScript.CanCancelMovement = false;
        }
    }
}