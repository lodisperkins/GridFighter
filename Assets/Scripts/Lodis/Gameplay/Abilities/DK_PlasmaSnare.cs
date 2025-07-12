using CustomEventSystem;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FixedPoints;
using Types;
using System.IO;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_PlasmaSnare : Ability
    {
        private FVector3 _spawnPosition;
        private FTransform _opponentTransform;
        private FTransform _panelTransform;
        private KnockbackBehaviour _opponentKnockback;
        private EntityDataBehaviour _auraSphere;
        private HitColliderBehaviour _collider;
        private Fixed32 _holdTime;
        private bool _opponentCaptured;
        private FixedTimeAction _despawnTimer;
        private FixedConditionAction _despawnCondition;
        private FixedConditionAction _liftCondition;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private FTransform _opponentParent;
        private int _originalChildCount;
        private GridPhysicsBehaviour _physics;
        private GameEventListener _returnToPool;
        private EntityDataBehaviour _opponent;
        private Fixed32 _liftHeight;
        private Fixed32 _moveSpeed;
        private Fixed32 _maxX;
        private Fixed32 _ySpeed;
        private Fixed32 _spawnDistance;
        private bool _targetFound;
        private FQuaternion _targetOriginalRotation;

        protected override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            bw.Write(_opponentCaptured);
            bw.Write(_targetFound);
        }

        protected override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            _opponentCaptured = br.ReadBoolean();
            _targetFound = br.ReadBoolean();
        }

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<EntityDataBehaviour>();
            _opponentParent = _opponent.FixedTransform.Parent;
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _spawnDistance = abilityData.GetCustomStatValue("SpawnDistance");
            _opponentTransform = _opponent.FixedTransform;
            _opponentKnockback = _opponentTransform.EntityData.GetComponent<KnockbackBehaviour>();
            _targetFound = false;

            _spawnPosition = GetTarget();

            //Get component info from opponent to use later.

            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, (Vector3)_spawnPosition, Camera.main.transform.rotation);
            //ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect, 1);

            GridTrackerBehaviour tracker = _chargeEffect.GetComponent<GridTrackerBehaviour>();

            if (!tracker)
                tracker = _chargeEffect.AddComponent<GridTrackerBehaviour>();

            tracker.Marker = MarkerType.DANGER;
            

            //Cache stat values to avoid repetitive calls.
            _liftHeight = abilityData.GetCustomStatValue("LiftHeight");
            _holdTime = abilityData.GetCustomStatValue("HoldTime");
            _moveSpeed = abilityData.GetCustomStatValue("MoveSpeed");
            _returnToPool?.ClearActions();
            _ySpeed = abilityData.GetCustomStatValue("ProjectileSpeed");

            _maxX = BlackBoardBehaviour.Instance.Grid.Width - BlackBoardBehaviour.Instance.Grid.PanelScale.x;
        }


        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private FVector3 GetTarget()
        {
            FTransform transform = null;
            PanelBehaviour targetPanel = null;
            FVector2 position = FVector2.Zero;

            if (OwnerMoveScript.Position.Y == _opponentKnockback.MovementBehaviour.Position.Y)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_opponentKnockback.transform.position, out targetPanel);
                _targetFound = true;
            }

            if (!targetPanel)
            {
                position = OwnerMoveScript.Position + (FVector2.Right * OwnerMoveScript.GetAlignmentX()) * _spawnDistance;
                BlackBoardBehaviour.Instance.Grid.GetPanel(position, out targetPanel);
                _targetFound = true;
            }
            //Fixed 32 is 0.5
            return targetPanel.FixedWorldPosition + FVector3.Up * new Fixed32(32768);
        }

        private void LiftOpponent(Collision collision)
        {
            //Only check knockback if a player was hit.
            GameObject other = collision.OtherEntity.UnityObject;
            if (!other.CompareTag("Player"))
                return;

            _liftCondition = FixedPointTimer.StartNewConditionAction(BeginLift, c => !_opponentKnockback.Physics.IsFrozen);
        }

        private void BeginLift()
        {
            _physics.StopVelocity();

            _auraSphere.transform.GetChild(0).gameObject.SetActive(true);
            _auraSphere.FixedTransform.WorldPosition += FVector3.Up * _liftHeight;

            //Attaches opponent to sphere and activate lifting. 
            _opponentTransform.Parent = _auraSphere.FixedTransform;
            _opponentKnockback.Physics.IsKinematic = true;
            _opponentKnockback.Physics.StopAllForces();

            _opponentTransform.LocalPosition = FVector3.Zero;
            _targetOriginalRotation = _opponentTransform.WorldRotation;
            _opponentTransform.WorldRotation = FQuaternion.Euler(0, 90 * -OwnerMoveScript.GetAlignmentX(), 0);
            _opponentCaptured = true;

            //Pause the timer so the player can hold the ability for as long as they want.
            PauseAbilityTimer();

            //Check if the opponent should be let go 
            _opponentKnockback.AddOnKnockBackStartAction(DespawnSphere);
            _despawnTimer = FixedPointTimer.StartNewTimedAction(DespawnSphere, _holdTime);
            _despawnCondition = FixedPointTimer.StartNewConditionAction(DespawnSphere, c => !_opponentCaptured || _auraSphere.FixedTransform.ChildCount == 0 || _opponentKnockback.CurrentAirState != AirState.TUMBLING || _opponentKnockback.IsInvincible);
        }

        private void DespawnSphere()
        {
            if (!_auraSphere || !_auraSphere.Data.Active)
                return;

            _despawnTimer?.Stop();
            UnpauseAbilityTimer();
            _opponentKnockback.RemoveOnKnockBackStartAction(DespawnSphere);

            if (_opponentCaptured)
            {
                //Reset the opponent values to set them free.
                _opponentTransform.Parent = _opponentParent;
                _opponentTransform.WorldPosition = _auraSphere.FixedTransform.WorldPosition;
                _opponentTransform.WorldRotation = FQuaternion.Euler(0, 90 * -OwnerMoveScript.GetAlignmentX(), 0);
                _opponentKnockback.Physics.IsKinematic = false;
                _opponentKnockback.Physics.UseGravity = true;
                _opponentCaptured = false;
            }
            //Handle vfx cleanup.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_auraSphere);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            _collider.ColliderInfo.OnHit -= LiftOpponent;

            EnableAccessory();
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The opponent should only be captured if they are allowed to move at a valid location.
            if (!_targetFound)
            {
                DespawnSphere();
                return;
            }

            //Spawn the new sphere and set its effect to inactive by default. The effect should only appear when the opponent is lifted.
            _auraSphere = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), _spawnPosition, Owner.FixedTransform.WorldRotation);
            _auraSphere.transform.GetChild(0).gameObject.SetActive(false);

            //Handle resetting opponent parent when despawned.
            _returnToPool = _auraSphere.GetComponent<GameEventListener>();
            _returnToPool.AddAction(() =>
            {
                if (_opponentTransform == null)
                {
                    return;
                }

                _opponentTransform.Parent = _opponentParent;
            });
            _returnToPool.IntendedSender = _auraSphere.gameObject;

            _originalChildCount = _auraSphere.FixedTransform.ChildCount;

            _physics = _auraSphere.GetComponent<GridPhysicsBehaviour>();

            _physics.Velocity = FVector3.Up * _ySpeed;

            //Initialize new collider for this attack.
            _collider = _auraSphere.GetComponent<HitColliderBehaviour>();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            _collider.ColliderInfo = GetColliderData(0);
            _collider.Spawner = Owner;
            _collider.ColliderInfo.OnHit += LiftOpponent;
            
            DisableAccessory();
        }


        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            if (!_opponentCaptured || _originalChildCount == _auraSphere.FixedTransform.ChildCount)
                DespawnSphere();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            DespawnSphere();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        protected override void OnMatchRestart()
        {
            DespawnSphere();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            _liftCondition?.Stop();
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            if (!_opponentCaptured)
                return;

            //Handle player movement input.
            int index = OwnerMoveset.GetSpecialAbilityIndex(this);

            //The ability should stop when they aren't pressing anything.
            if (OwnerInput?.GetSpecialButton(index + 1) == false)
            {
                UnpauseAbilityTimer();
            }
            else if (_auraSphere && OwnerInput)
            {
                FVector3 direction = OwnerInput.AttackDirection;

                //Calculate the new position based on input.
                FVector3 position = _auraSphere.FixedTransform.WorldPosition + direction * dt * _moveSpeed;
                //Clamping so they can't push the opponent out of bounds.
                position.X = Fixed32.Clamp(position.X, 0, _maxX);
                position.Y = Fixed32.Clamp(position.Y, 1, GridMovementBehaviour.MaxYPosition);
                _auraSphere.FixedTransform.WorldPosition = position;
            }
        }
    }
}