using Lodis.FX;
using Lodis.Input;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Unleashes a devastating ball of energy that does massive damage and knockback.
    /// </summary>
    public class DK_ElectroBomb : ProjectileAbility
    {
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private bool _explosionSpawned;
        private ConditionAction _spawnAccessoryAction;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
            UseGravity = true;
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

            TimeCountType = TimedActionCountType.UNSCALEDTIME;
            RoutineBehaviour.Instance.StartNewTimedAction(StartSuperEffect, TimedActionCountType.FRAME, 2);

            _explosionSpawned = false;
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
            float jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            Vector3 position = owner.transform.position + Vector3.up * jumpHeight;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);
            //MatchManagerBehaviour.Instance.SuperInUse = true;
            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, OwnerMoveset.HeldItemSpawnLeft, true);
            RoutineBehaviour.Instance.StartNewConditionAction(args => ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect), condition => !InUse || CurrentAbilityPhase != AbilityPhase.STARTUP);
        }

        private void StartCombo(params object[] args)
        {
            GameObject target = (GameObject)args[0];
            if (!target.CompareTag("Player"))
                return;

            GridMovementBehaviour movement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<GridMovementBehaviour>();

            Vector3 position = movement.CurrentPanel.transform.position += owner.transform.forward;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);

            AnimationClip kickClip;

            abilityData.GetAdditionalAnimation(0, out kickClip);

            OwnerAnimationScript.PlayAnimation(kickClip, 1, true);

            _flurry = ObjectPoolBehaviour.Instance.GetObject(_flurryRef, target.transform.position + Vector3.up, Projectile.transform.rotation);
            HitColliderBehaviour flurryCollider = _flurry.GetComponent<HitColliderBehaviour>();

            flurryCollider.ColliderInfo = GetColliderData(1);
            flurryCollider.Owner = owner;

            DisableAccessory();
            RoutineBehaviour.Instance.StopAction(_spawnAccessoryAction);

            _spawnAccessoryAction = RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !_flurry.activeInHierarchy);

            ActiveProjectiles.Add(_flurry);
        }

        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);
            DelayedAction action = RoutineBehaviour.Instance.StartNewConditionAction(SpawnExplosion, condition => Projectile.transform.position.y <= 0 && !_explosionSpawned);
            RoutineBehaviour.Instance.StartNewConditionAction(parameters => RoutineBehaviour.Instance.StopAction(action), condition => !Projectile.activeInHierarchy);
            //MatchManagerBehaviour.Instance.SuperInUse = false;

            CleanProjectileList();

            //Only fire if there aren't two many instances of this object active
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            ProjectileColliderData.OnHit += StartCombo;

            SpawnSword();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            TimeCountType = TimedActionCountType.CHARACTERSCALEDTIME;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;
        }
    }
}