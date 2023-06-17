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

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
            UseGravity = true;
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

            PrepareBlast();
            OnHit += SpawnExplosion;
            _explosionSpawned = false;
        }

        private void SpawnExplosion(params object[] args)
        {
            //Only check knockback if a player was hit.
            GameObject other = (GameObject)args[0];
            if (!other.CompareTag("Panel") || _explosionSpawned)
                return;

            _explosionSpawned = true;
            Projectile.SetActive(false);
            float explosionColliderHeight = abilityData.GetCustomStatValue("ExplosionColliderHeight");
            float explosionColliderWidth = abilityData.GetCustomStatValue("ExplosionColliderWidth");

            BlackBoardBehaviour.Instance.Player2.GetComponent<GridPhysicsBehaviour>().StopVelocity();
            HitColliderSpawner.SpawnBoxCollider(Projectile.transform.position + Vector3.up, new Vector3(explosionColliderWidth, explosionColliderHeight, 1), GetColliderData(1), owner);
        }

        private void PrepareBlast()
        {
            float jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            Vector3 position = owner.transform.position + Vector3.up * jumpHeight;

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.DisableMovement(condition => !InUse);
            OwnerMoveScript.TeleportToLocation(position, 0, false);
            MatchManagerBehaviour.Instance.SuperInUse = true;
            IControllable controller = owner.GetComponentInParent<IControllable>();

            FXManagerBehaviour.Instance.StartSuperMoveVisual(controller.PlayerID, abilityData.startUpTime);

            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, OwnerMoveset.HeldItemSpawnLeft, true);
            RoutineBehaviour.Instance.StartNewConditionAction(args => ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect), condition => !InUse || CurrentAbilityPhase != AbilityPhase.STARTUP);
        }

        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args); 
            MatchManagerBehaviour.Instance.SuperInUse = false;
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