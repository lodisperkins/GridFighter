using Lodis.Utility;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a single powerful charge shot down the row the character is facing.
    /// </summary>
    public class SN_Jab : ProjectileAbility
    {
        public Transform spawnTransform = null;

        //Usd to store a reference to the laser prefab
        private EntityDataBehaviour _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;
        private GameObject _smokeTrailRef;
        private GameObject _chargeEffectRef;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SN_Jab_Data"));
            _chargeEffectRef = Resources.Load<GameObject>("Effects/RisingChargeEffect");
            _smokeTrailRef = Resources.Load<GameObject>("Effects/GroundWindTrail");

            Owner = newOwner;

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab.GetComponent<EntityDataBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            Transform spawnTransform = null;

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.LEFT)
                spawnTransform = OwnerMoveset.RightMeleeSpawns[1];
            else
                spawnTransform = OwnerMoveset.LeftMeleeSpawns[1];

            GameObject chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef, spawnTransform, true);
            GameObject smokeTrail = ObjectPoolBehaviour.Instance.GetObject(_smokeTrailRef, Owner.transform.position - Vector3.up / 2, Owner.transform.rotation);

            RoutineBehaviour.Instance.StartNewConditionAction(arguments =>
            {

                ObjectPoolBehaviour.Instance.ReturnGameObject(chargeEffect);
                ObjectPoolBehaviour.Instance.ReturnGameObject(smokeTrail);

            },condition => !InUse || CurrentAbilityPhase != AbilityPhase.STARTUP);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            CameraBehaviour.ShakeBehaviour.ShakeRotation(0.5f);

            //Initialize collider stats
            Fixed32 powerScale = (Fixed32)args[0];
            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(powerScale);

            //Initialize and attach spawn script
            OwnerMoveset.ProjectileSpawner.Projectile = _projectile;

            //Fire laser
            ActiveProjectiles.Add(OwnerMoveset.ProjectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider));
        }
    }
}