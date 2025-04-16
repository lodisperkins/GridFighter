using FixedPoints;
using Lodis.FX;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    ///Make sword appear on super close up. Add some particle effect.
    ///FIx camera on slow motion.
    ///Make sword disappear after teleport
    ///Test what happens when opponent is on last panel
    ///Fix panel danger warningl

    /// <summary>
    /// Unleashes a devastating ball of energy that does massive damage and knockback.
    /// </summary>
    public class DK_ElectroBomb : ProjectileAbility
    {
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private bool _explosionSpawned;
        private FixedConditionAction _spawnAccessoryAction;
        private GridMovementBehaviour _opponentMovement;
        private KnockbackBehaviour _opponentKnockback;
        private CharacterFeedbackBehaviour _characterFeedback;
        private CharacterFeedbackBehaviour _opponentFeedback;
        private Fixed32 _slowMotionTimeScale; // 0.05
        private Fixed32 _slowMotionTime; // 1.2
        private GameObject _thalamusInstance;
        private int _thalamusLayer;
        private Transform _heldItemSpawn;
        private GameObject _axeKick;
        private Vector3 _defaultCameraMoveSpeed;
        private bool _threwBlast;
        private FixedPoints.MoveAction _moveAction;

        protected override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            bw.Write(_explosionSpawned);
            bw.Write(_threwBlast);
        }

        protected override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            _explosionSpawned = br.ReadBoolean();
            _threwBlast = br.ReadBoolean();
        }

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
            //0.05
            _slowMotionTimeScale = new Fixed32(3276);
            //1.2
            _slowMotionTime = new Fixed32(78643);
            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<GridMovementBehaviour>();
            _opponentKnockback = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<KnockbackBehaviour>();

            _characterFeedback = Owner.GetComponentInChildren<CharacterFeedbackBehaviour>();
            _opponentFeedback = _opponentMovement.gameObject.GetComponentInChildren<CharacterFeedbackBehaviour>();

            _defaultCameraMoveSpeed = CameraBehaviour.Instance.CameraMoveSpeed;
        }

        private void SpawnSword()
        {
            DisableAccessory();

            _spawnAccessoryAction = FixedPointTimer.StartNewConditionAction(EnableAccessory, condition => !Projectile.Active);
        }

        private IEnumerator SetTimeUnscaled()
        {
            yield return new WaitUntil(() => !MatchManagerBehaviour.Instance.SuperInUse);
            TimeUnit = FixedTimeAction.UnitOfTime.PauseScaled;
        }

        protected override void OnStart(params object[] args)
        {

            base.OnStart(args);

            DisableAccessory();

            //Store the held item spawn so it can be easily used to hold the sword in the right hand.
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
            {
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;
            }
            else
            {
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            }

            //Place the sword in their hand.
            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
            _thalamusLayer = _thalamusInstance.layer;
            _thalamusInstance.GetComponent<ColorManagerBehaviour>().SetColors((int)OwnerMoveScript.Alignment);

            _thalamusInstance.transform.rotation = _thalamusInstance.transform.parent.rotation;

            ProjectileColliderData = GetColliderData(2);

            TimeUnit = FixedTimeAction.UnitOfTime.PauseScaled;

            StartSuperEffect();

            //Set flags so the super can play without being interuppted by physics.
            _explosionSpawned = false;
            UseGravity = false;
            OwnerKnockBackScript.IgnoreAdjustedGravity(arguments => !InUse);
            _opponentKnockback.SetDamageableAbilityID(abilityData.ID, arguments => !InUse);
            _threwBlast = false;
            AddAnimationEvents();
        }

        private void AddAnimationEvents()
        {
            //Kick animation events
            OwnerAnimationScript.AddEventListener("ElectroKickWindUp", () =>
            {
                //Spawns the effect for the the kick.

                GameObject effect = abilityData.Effects[1];
                CameraBehaviour.Instance.CameraMoveSpeed *= 600;

                OwnerVoiceScript.PlayLightAttackSound();

                _axeKick = ObjectPoolBehaviour.Instance.GetObject(effect, Owner.transform.position, Quaternion.Euler(Owner.transform.rotation.x, -Owner.transform.rotation.y, Owner.transform.rotation.z));
            });

            OwnerAnimationScript.AddEventListener("ElectroKick", () =>
            {
                HitColliderBehaviour kickCollider = HitColliderSpawner.SpawnCollider(_opponentMovement.FixedTransform.WorldPosition, 1, 1, GetColliderData(3), Owner);
            });

            OwnerAnimationScript.AddEventListener("ChargeElectroBomb", PrepareBlast);

            //Throw bomb animation events
            OwnerAnimationScript.AddEventListener("StartElectroBombSlowMotion", () =>
            {
                //Set up super move vfx.
                //0.01
                MatchManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale, new Fixed32(655), _slowMotionTime);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(false);
                SoundManagerBehaviour.Instance.PlaySound(abilityData.Sounds[0], 3);
            });

            OwnerAnimationScript.AddEventListener("ThrowElectroBomb", () =>
            {
                //Undo some super move vfx and restore gravity so they fall.
                ProjectileColliderData = GetColliderData(0);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);
                UseGravity = true;

                ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);

                OwnerVoiceScript.PlayHeavyAttackSound();
                _threwBlast = true;

                //Throw the blast.
                ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
                projectileSpawner.Projectile = abilityData.Effects[2].GetComponent<EntityDataBehaviour>();

                ShotDirection = projectileSpawner.FixedTransform.Forward;

                HitColliderData data = ProjectileColliderData;

                Projectile = projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("OrbSpeed"), data, UseGravity);

                Projectile.name += "(" + abilityData.name + ")";

                Projectile.FixedTransform.WorldPosition += Owner.FixedTransform.Forward;

                ActiveProjectiles.Add(Projectile);

                //Handles spawning the explosion once the blast hits the ground.
                FixedConditionAction action = FixedPointTimer.StartNewConditionAction(SpawnExplosion, condition => Projectile.FixedTransform.WorldPosition.Y <= 1 && !_explosionSpawned);
                FixedPointTimer.StartNewConditionAction(() => action.Stop(), condition => !Projectile.Data.Active);

                //Set up camera to focus on the blast by making it focus on the opponent.
                CameraBehaviour.Instance.ZoomAmount = 1;

                GridAlignment oppCamAlignemnt = OwnerMoveScript.Alignment == GridAlignment.LEFT ? GridAlignment.RIGHT : GridAlignment.LEFT;
                CameraBehaviour.Instance.AlignmentFocus = oppCamAlignemnt;

                UnpauseAbilityTimer();
                //MatchManagerBehaviour.Instance.SuperInUse = false;
            });
        }

        private void SpawnExplosion()
        {
            _explosionSpawned = true;
            Projectile.RemoveFromGame();
            Fixed32 explosionColliderHeight = abilityData.GetCustomStatValue("ExplosionColliderHeight");
            Fixed32 explosionColliderWidth = abilityData.GetCustomStatValue("ExplosionColliderWidth");

            BlackBoardBehaviour.Instance.Player2.GetComponent<GridPhysicsBehaviour>().StopVelocity();
            HitColliderSpawner.SpawnCollider(Projectile.FixedTransform.WorldPosition + FVector3.Up, explosionColliderWidth, explosionColliderHeight, GetColliderData(1), Owner);

            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;
        }

        private void StartSuperEffect()
        {

            IControllable controller = Owner.GetComponentInParent<IControllable>();

            FXManagerBehaviour.Instance.StartSuperMoveVisual(controller.PlayerID, 2, _thalamusInstance);
        }

        private void PrepareBlast()
        {
            //Kinematic so they don't move while in the air.
            OwnerKnockBackScript.Physics.IsKinematic = true;
            //Make the sword disappear while charging blast.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);

            //Disable UI so they don't clip through it while this is happening.
            _characterFeedback.SetCharacterUIEnabled(false);
            _opponentFeedback.SetCharacterUIEnabled(false);

            //Calculate where to put the player to charger the blast.
            Fixed32 jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            Fixed32 xOffset = (GetColliderData(3).BaseKnockBack * -OwnerMoveScript.GetAlignmentX());
            FVector3 position = OwnerMoveScript.FixedTransform.WorldPosition + FVector3.Right * xOffset  +  (jumpHeight * FVector3.Up);
            position.X = Fixed32.Clamp(position.X, 0, BlackBoardBehaviour.Instance.Grid.Width);

            //Move the player to the charging area.
            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation((FixedPoints.FVector3)position, 0, false);
            
            //MatchManagerBehaviour.Instance.SuperInUse = true;

            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef, OwnerMoveset.HeldItemSpawnLeft, true);
            CameraBehaviour.Instance.ClampY = false;
            abilityData.GetAdditionalAnimation(1, out AnimationClip throwClip);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_axeKick);

            OwnerVoiceScript.PlayLightAttackSound();

            Debug.Log(InUse);
            FixedPointTimer.StartNewTimedAction(() => SetUpToss(throwClip),  GridGame.FixedTimeStep * 2);
        }

        private void SetUpToss(AnimationClip throwClip)
        {
            OwnerAnimationScript.PlayAnimation(throwClip, 1, true);
            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = OwnerMoveScript.Alignment;
            _opponentKnockback.Physics.StopAllForces();

            if (_moveAction == null)
            {
                _moveAction = (FixedPoints.MoveAction)FixedLerp.DoMove(_opponentMovement.FixedTransform, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition, new Fixed32(98304) * 2, id: "Opponent Move");
            }
            else
            {
                _moveAction.Rewind();
                _moveAction.ChangeValues(_opponentMovement.FixedTransform.WorldPosition, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition);
            }

            //_moveAction = (FixedPoints.MoveAction)FixedLerp.DoMove(_opponentMovement.FixedTransform, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition, new Fixed32(98304) * 2, id: "Opponent Move");
            FixedPointTimer.StartNewConditionAction(() =>
            {
                _opponentKnockback.Physics.UseGravity = true; 
                _moveAction.Kill();
            }, c => !InUse || _threwBlast);

        }
        private void StartCombo(Collision collision)
        {
            //Don't start the combo if we didn't collide we may have missed.
            GameObject target = collision.OtherEntity.UnityObject;
            if (!target.CompareTag("Player"))
            {
                return;
            }

            
            HealthBehaviour opponentHealthBehaviour = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner).GetComponent<HealthBehaviour>();

            if (opponentHealthBehaviour.IsInvincible)
            {
                return;
            }

            MatchManagerBehaviour.Instance.SuperInUse = true;
            PauseAbilityTimer();

            OwnerKnockBackScript.SetIntagibilityByCondition(condition => !InUse);

            PanelBehaviour opponentPanel = null;
            if (_opponentMovement.Position.X == BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1 || _opponentMovement.Position.X == 0)
            {

                BlackBoardBehaviour.Instance.Grid.GetPanel(_opponentMovement.Position + FVector2.Right * -OwnerMoveScript.GetAlignmentX(), out opponentPanel);

                if (!opponentPanel)
                    opponentPanel = OwnerMoveScript.CurrentPanel;

                _opponentMovement.FixedTransform.WorldPosition = opponentPanel.FixedWorldPosition + FVector3.Up * _opponentMovement.HeightOffset;
            }

            opponentHealthBehaviour.Stun(2);

            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_opponentMovement.transform.position, out opponentPanel);

            FVector2 panelPosition = BlackBoardBehaviour.Instance.Grid.ClampPanelPosition(opponentPanel.Position + FVector2.Right * OwnerMoveScript.GetAlignmentX(), GridAlignment.ANY);

            BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out PanelBehaviour landingPanel);

            FVector3 position = landingPanel.FixedWorldPosition + FVector3.Up * OwnerMoveScript.HeightOffset;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
            {
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;
            }

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
            _thalamusInstance.transform.localRotation = Quaternion.identity;
            _thalamusInstance.layer = LayerMask.NameToLayer("Default");

            CameraBehaviour.Instance.ZoomAmount = 10;
            CameraBehaviour.Instance.ClampX = false;


            abilityData.GetAdditionalAnimation(0, out AnimationClip kickClip);

            RoutineBehaviour.Instance.StartNewTimedAction(arguments => OwnerAnimationScript.PlayAnimation(kickClip, 1, true), TimedActionCountType.FRAME, 1);

            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.LEFT;
        }

        private void ClearAnimationEvents()
        {
            OwnerAnimationScript.RemoveEventListener("ElectroKickWindUp");
            OwnerAnimationScript.RemoveEventListener("ElectroKick");
            OwnerAnimationScript.RemoveEventListener("ChargeElectroBomb");
            OwnerAnimationScript.RemoveEventListener("ThrowElectroBomb");
        }

        protected override void OnActivate(params object[] args)
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            CleanProjectileList();

            ProjectileColliderData.OnHit += StartCombo;
            base.OnActivate();

            SpawnSword();
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            if (_threwBlast)
            {
                _opponentMovement.FixedTransform.WorldPosition = Projectile.FixedTransform.WorldPosition;
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            TimeUnit = FixedTimeAction.UnitOfTime.Scaled;

            if (!MatchManagerBehaviour.Instance.PlayerOutOfRing)
            {
                FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
                CameraBehaviour.Instance.ZoomAmount = 0;
                CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;
            }

            MatchManagerBehaviour.Instance.SuperInUse = false;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);

            if (_thalamusInstance)
                _thalamusInstance.layer = _thalamusLayer;

            OwnerKnockBackScript.Physics.IsKinematic = false;
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ClampY = true;

            CameraBehaviour.Instance.CameraMoveSpeed = _defaultCameraMoveSpeed;
            _characterFeedback.SetCharacterUIEnabled(true);
            _opponentFeedback.SetCharacterUIEnabled(true);

            ClearAnimationEvents();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            TimeUnit = FixedTimeAction.UnitOfTime.Scaled;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;

            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);

            if (_thalamusInstance)
                _thalamusInstance.layer = _thalamusLayer;

            OwnerKnockBackScript.Physics.IsKinematic = false;
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ClampY = true;
            CameraBehaviour.Instance.CameraMoveSpeed = _defaultCameraMoveSpeed;
            _characterFeedback.SetCharacterUIEnabled(true);
            _opponentFeedback.SetCharacterUIEnabled(true);

            ClearAnimationEvents();
        }
    }
}