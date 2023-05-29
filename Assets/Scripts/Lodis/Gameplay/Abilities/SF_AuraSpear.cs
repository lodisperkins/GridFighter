using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_AuraSpear : ProjectileAbility
    {
        private HitColliderData _swordCollider;
        private GameObject _flurryRef;
        private GameObject _flurry;
        private ConditionAction _spawnAccessoryAction;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _flurryRef = Resources.Load<GameObject>("Projectiles/Prototype2/ChargeSwordFlurry");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _swordCollider = GetColliderData(0);
        }

        private void SpawnFlurry(params object[] args)
        {
            GameObject target = (GameObject)args[0];
            if (!target.CompareTag("Player"))
                return;

            _flurry = ObjectPoolBehaviour.Instance.GetObject(_flurryRef, target.transform.position + Vector3.up, Projectile.transform.rotation);
            HitColliderBehaviour flurryCollider = _flurry.GetComponent<HitColliderBehaviour>();

            flurryCollider.ColliderInfo = GetColliderData(1);
            flurryCollider.Owner = owner;

            DisableAccessory();
            RoutineBehaviour.Instance.StopAction(_spawnAccessoryAction);

            _spawnAccessoryAction = RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !_flurry.activeInHierarchy);

            ActiveProjectiles.Add(_flurry);
        }

        private void SpawnSword()
        {
            DisableAccessory();

            _spawnAccessoryAction = RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !Projectile.activeInHierarchy);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            CleanProjectileList();

            //Only fire if there aren't two many instances of this object active
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            ProjectileColliderData.OnHit += SpawnFlurry;

            if (OwnerMoveScript.IsMoving)
            {
                OwnerMoveScript.AddOnMoveEndTempAction(() =>
                {
                    base.OnActivate(args);
                    SpawnSword();
                });
            }
            else
            {
                base.OnActivate(args);
                SpawnSword();
            }



        }
    }
}
