using Lodis.FX;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.Movement;
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
        private float _slowMotionTimeScale;
        private float _slowMotionTime;
        private GameObject _thalamusInstance;
        private Transform _heldItemSpawn;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];

            _slowMotionTimeScale = abilityData.GetCustomStatValue("SlowMotionTimeScale");
            _slowMotionTime = abilityData.GetCustomStatValue("SlowMotionTime");
            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<GridMovementBehaviour>();

            //Kick animation events
            OwnerAnimationScript.AddEventListener("ElectroKickWindUp", () =>
            {
                GameObject effect = abilityData.Effects[1];

                ObjectPoolBehaviour.Instance.GetObject(effect, owner.transform.position, Quaternion.Euler(owner.transform.rotation.x, -owner.transform.rotation.y, owner.transform.rotation.z));
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

                CameraBehaviour.Instance.ZoomAmount = 4.2f;
            });

            OwnerAnimationScript.AddEventListener("ThrowElectroBomb", () =>
            {
                ProjectileColliderData = GetColliderData(0);

                UseGravity = true;

                ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
                projectileSpawner.Projectile = abilityData.Effects[2];
                SpawnTransform = projectileSpawner.transform;
                ShotDirection = projectileSpawner.transform.forward;

                HitColliderData data = ProjectileColliderData;

                Projectile = projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("OrbSpeed"), data, UseGravity);

                //Fire projectile
                Projectile.name += "(" + abilityData.name + ")";
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

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
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

            float xOffset = (GetColliderData(3).BaseKnockBack * -OwnerMoveScript.GetAlignmentX());

            xOffset = Mathf.Clamp(xOffset, -BlackBoardBehaviour.Instance.Grid.Dimensions.x, BlackBoardBehaviour.Instance.Grid.Dimensions.x);

            Vector3 position = OwnerMoveScript.transform.position + Vector3.right * xOffset  +  (jumpHeight * Vector3.up);

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);
            //MatchManagerBehaviour.Instance.SuperInUse = true;
            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, OwnerMoveset.HeldItemSpawnLeft, true);
            RoutineBehaviour.Instance.StartNewConditionAction(args => ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect), condition => !InUse || CurrentAbilityPhase != AbilityPhase.STARTUP);

            AnimationClip throwClip = null;
            abilityData.GetAdditionalAnimation(1, out throwClip);

            RoutineBehaviour.Instance.StartNewTimedAction(args => OwnerAnimationScript.PlayAnimation(throwClip, 1, true), TimedActionCountType.FRAME, 1);
        }

        private void StartCombo(params object[] args)
        {
            GameObject target = (GameObject)args[0];
            if (!target.CompareTag("Player"))
                return;

            MatchManagerBehaviour.Instance.SuperInUse = true;
            PauseAbilityTimer();


            Vector3 position = _opponentMovement.CurrentPanel.transform.position + owner.transform.forward * OwnerMoveScript.GetAlignmentX() + Vector3.up * 0.5f;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);

            CameraBehaviour.Instance.ZoomAmount = 3;
            CameraBehaviour.Instance.ClampX = false;

            AnimationClip kickClip;

            abilityData.GetAdditionalAnimation(0, out kickClip);

            RoutineBehaviour.Instance.StartNewTimedAction(arguments => OwnerAnimationScript.PlayAnimation(kickClip, 1, true), TimedActionCountType.FRAME, 1);


        }

        protected override void OnActivate(params object[] args)
        {
            CleanProjectileList();

            ProjectileColliderData.OnHit += StartCombo;
            base.OnActivate();

            SpawnSword();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;
            MatchManagerBehaviour.Instance.SuperInUse = false;
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            OwnerKnockBackScript.Physics.IgnoreForces = false;
            CameraBehaviour.Instance.ClampX = true;
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;
            CameraBehaviour.Instance.ZoomAmount = 0;
            CameraBehaviour.Instance.AlignmentFocus = GridAlignment.ANY;
            MatchManagerBehaviour.Instance.SuperInUse = false;
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            OwnerKnockBackScript.Physics.IgnoreForces = false;
            CameraBehaviour.Instance.ClampX = true;
        }
    }
}