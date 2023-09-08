using Lodis.FX;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
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
        private ConditionAction _spawnAccessoryAction;
        private GridMovementBehaviour _opponentMovement;
        private MovesetBehaviour _opponentMoveset;
        private CharacterFeedbackBehaviour _characterFeedback;
        private CharacterFeedbackBehaviour _opponentFeedback;
        private float _slowMotionTimeScale;
        private float _slowMotionTime;
        private GameObject _thalamusInstance;
        private int _thalamusLayer;
        private Transform _heldItemSpawn;
        private GameObject _axeKick;
        private Vector3 _defaultCameraMoveSpeed;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];

            _slowMotionTimeScale = abilityData.GetCustomStatValue("SlowMotionTimeScale");
            _slowMotionTime = abilityData.GetCustomStatValue("SlowMotionTime");
            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<GridMovementBehaviour>();
            _opponentMoveset = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<MovesetBehaviour>();

            _characterFeedback = owner.GetComponentInChildren<CharacterFeedbackBehaviour>();
            _opponentFeedback = _opponentMovement.gameObject.GetComponentInChildren<CharacterFeedbackBehaviour>();

            _defaultCameraMoveSpeed = CameraBehaviour.Instance.CameraMoveSpeed;

            //Kick animation events
            OwnerAnimationScript.AddEventListener("ElectroKickWindUp", () =>
            {
                GameObject effect = abilityData.Effects[1];
                CameraBehaviour.Instance.CameraMoveSpeed *= 600;

                OwnerVoiceScript.PlayLightAttackSound();

                _axeKick = ObjectPoolBehaviour.Instance.GetObject(effect, owner.transform.position, Quaternion.Euler(owner.transform.rotation.x, -owner.transform.rotation.y, owner.transform.rotation.z));
            });

            OwnerAnimationScript.AddEventListener("ElectroKick", () =>
            {
                HitColliderBehaviour kickCollider = HitColliderSpawner.SpawnBoxCollider(_opponentMovement.transform.position, Vector3.one, GetColliderData(3), owner);
            });

            OwnerAnimationScript.AddEventListener("ChargeElectroBomb", PrepareBlast);


            //Throw bomb animation events
            OwnerAnimationScript.AddEventListener("StartElectroBombSlowMotion", () =>
            {
                MatchManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale, 0.01f, _slowMotionTime);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(false);
                SoundManagerBehaviour.Instance.PlaySound(abilityData.Sounds[0], 3);
            });

            OwnerAnimationScript.AddEventListener("ThrowElectroBomb", () =>
            {
                ProjectileColliderData = GetColliderData(0);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);
                UseGravity = true;

                ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);

                OwnerVoiceScript.PlayHeavyAttackSound();

                ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
                projectileSpawner.Projectile = abilityData.Effects[2];
                SpawnTransform = _heldItemSpawn;
                ShotDirection = projectileSpawner.transform.forward;

                HitColliderData data = ProjectileColliderData;

                Projectile = projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("OrbSpeed"), data, UseGravity);

                //Fire projectile
                Projectile.name += "(" + abilityData.name + ")";

                Projectile.transform.position += owner.transform.forward;

                ActiveProjectiles.Add(Projectile);

                DelayedAction action = RoutineBehaviour.Instance.StartNewConditionAction(SpawnExplosion, condition => Projectile.transform.position.y <= 0 && !_explosionSpawned);
                RoutineBehaviour.Instance.StartNewConditionAction(parameters => RoutineBehaviour.Instance.StopAction(action), condition => !Projectile.activeInHierarchy);

                CameraBehaviour.Instance.ZoomAmount = 0;

                RoutineBehaviour.Instance.StartNewTimedAction(arguments => UnpauseAbilityTimer(), TimedActionCountType.SCALEDTIME, 1);
                //MatchManagerBehaviour.Instance.SuperInUse = false;
            });
        }

        private void SpawnSword()
        {
            DisableAccessory();

            _spawnAccessoryAction = RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !Projectile.activeInHierarchy);
        }

        private IEnumerator SetTimeUnscaled()
        {
            yield return new WaitUntil(() => !MatchManagerBehaviour.Instance.SuperInUse);
            TimeCountType = TimedActionCountType.UNSCALEDTIME;

        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            DisableAccessory();

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
            _thalamusLayer = _thalamusInstance.layer;

            _thalamusInstance.layer = LayerMask.NameToLayer("BattleOverlayEffect");

            _thalamusInstance.transform.localRotation = Quaternion.identity;

            ProjectileColliderData = GetColliderData(2);

            TimeCountType = TimedActionCountType.UNSCALEDTIME;
            RoutineBehaviour.Instance.StartNewTimedAction(StartSuperEffect, TimedActionCountType.FRAME, 2);

            _explosionSpawned = false;
            UseGravity = false;
        }

        private void SpawnExplosion(params object[] args)
        {
            _explosionSpawned = true;
            Projectile.SetActive(false);
            float explosionColliderHeight = abilityData.GetCustomStatValue("ExplosionColliderHeight");
            float explosionColliderWidth = abilityData.GetCustomStatValue("ExplosionColliderWidth");

            BlackBoardBehaviour.Instance.Player2.GetComponent<GridPhysicsBehaviour>().StopVelocity();
            HitColliderSpawner.SpawnBoxCollider(Projectile.transform.position + Vector3.up, new Vector3(explosionColliderWidth, explosionColliderHeight, 1), GetColliderData(1), owner);
        }

        private void StartSuperEffect(params object[] args)
        {

            IControllable controller = owner.GetComponentInParent<IControllable>();

            FXManagerBehaviour.Instance.StartSuperMoveVisual(controller.PlayerID, abilityData.startUpTime);
        }

        private void PrepareBlast()
        {
            OwnerKnockBackScript.Physics.IgnoreForces = true;
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            float jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            _characterFeedback.SetCharacterUIEnabled(false);
            _opponentFeedback.SetCharacterUIEnabled(false);

            float xOffset = (GetColliderData(3).BaseKnockBack * -OwnerMoveScript.GetAlignmentX());

            Vector3 position = OwnerMoveScript.transform.position + Vector3.right * xOffset  +  (jumpHeight * Vector3.up);

            position.x = Mathf.Clamp(position.x, 0, BlackBoardBehaviour.Instance.Grid.Width);

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);
            CameraBehaviour.Instance.ZoomAmount = 0;
            //MatchManagerBehaviour.Instance.SuperInUse = true;
            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, OwnerMoveset.HeldItemSpawnLeft, true);
            CameraBehaviour.Instance.ClampY = false;
            AnimationClip throwClip = null;
            abilityData.GetAdditionalAnimation(1, out throwClip);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_axeKick);

            OwnerVoiceScript.PlayLightAttackSound();

            RoutineBehaviour.Instance.StartNewTimedAction(args => OwnerAnimationScript.PlayAnimation(throwClip, 1, true), TimedActionCountType.FRAME, 1);
        }

        private void StartCombo(params object[] args)
        {
            GameObject target = (GameObject)args[0];
            if (!target.CompareTag("Player"))
                return;

            
            HealthBehaviour opponentHealthBehaviour = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<HealthBehaviour>();

            if (opponentHealthBehaviour.IsInvincible)
                return;

            MatchManagerBehaviour.Instance.SuperInUse = true;
            PauseAbilityTimer();

            OwnerKnockBackScript.SetIntagibilityByCondition(condition => !InUse);

            PanelBehaviour opponentPanel = null;
            if (_opponentMovement.Position.x == BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1 || _opponentMovement.Position.x == 0)
            {

                BlackBoardBehaviour.Instance.Grid.GetPanel(_opponentMovement.Position + Vector2.right * -OwnerMoveScript.GetAlignmentX(), out opponentPanel);

                _opponentMovement.transform.position = opponentPanel.transform.position + Vector3.up * _opponentMovement.HeightOffset;
            }

            opponentHealthBehaviour.Stun(2);

            PanelBehaviour landingPanel = null; 
            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_opponentMovement.transform.position, out opponentPanel);

            Vector2 panelPosition = BlackBoardBehaviour.Instance.Grid.ClampPanelPosition(opponentPanel.Position + Vector2.right * OwnerMoveScript.GetAlignmentX(), GridAlignment.ANY);

            BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out landingPanel);

            Vector3 position = landingPanel.transform.position + Vector3.up * OwnerMoveScript.HeightOffset;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
            _thalamusInstance.transform.localRotation = Quaternion.identity;
            _thalamusInstance.layer = LayerMask.NameToLayer("Default");

            CameraBehaviour.Instance.ZoomAmount = 4;
            CameraBehaviour.Instance.ClampX = false;

            AnimationClip kickClip;

            abilityData.GetAdditionalAnimation(0, out kickClip);

            RoutineBehaviour.Instance.StartNewTimedAction(arguments => OwnerAnimationScript.PlayAnimation(kickClip, 1, true), TimedActionCountType.FRAME, 1);

            CameraBehaviour.Instance.AlignmentFocus = OwnerMoveScript.Alignment;

        }

        protected override void OnActivate(params object[] args)
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            CleanProjectileList();

            ProjectileColliderData.OnHit += StartCombo;
            base.OnActivate();

            SpawnSword();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;

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

            OwnerKnockBackScript.Physics.IgnoreForces = false;
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ClampY = true;

            CameraBehaviour.Instance.CameraMoveSpeed = _defaultCameraMoveSpeed;
            _characterFeedback.SetCharacterUIEnabled(true);
            _opponentFeedback.SetCharacterUIEnabled(true);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;

            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);

            if (_thalamusInstance)
                _thalamusInstance.layer = _thalamusLayer;

            OwnerKnockBackScript.Physics.IgnoreForces = false;
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ClampY = true;
            CameraBehaviour.Instance.CameraMoveSpeed = _defaultCameraMoveSpeed;
            _characterFeedback.SetCharacterUIEnabled(true);
            _opponentFeedback.SetCharacterUIEnabled(true);
        }
    }
}