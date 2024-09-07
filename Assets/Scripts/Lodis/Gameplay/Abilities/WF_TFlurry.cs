using FixedPoints;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WF_TFlurry : ProjectileAbility
    {
        private HitColliderData _swordCollider;
        private EntityDataBehaviour _flurryRef;
        private EntityDataBehaviour _flurry;
        private FixedConditionAction _spawnAccessoryAction;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _flurryRef = Resources.Load<EntityDataBehaviour>("Projectiles/Prototype2/SwordFlurry");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _swordCollider = GetColliderData(0);
        }

        private void SpawnFlurry(Collision collision)
        {
            EntityData target = collision.Entity;
            if (!target.UnityObject.CompareTag("Player"))
                return;

            _flurry = ObjectPoolBehaviour.Instance.GetObject(_flurryRef, target.Transform.WorldPosition + FVector3.Up, Projectile.FixedTransform.WorldRotation);
            HitColliderBehaviour flurryCollider = _flurry.GetComponent<HitColliderBehaviour>();

            flurryCollider.ColliderInfo = GetColliderData(1);
            flurryCollider.Spawner = Owner;

            DisableAccessory();
            FixedPointTimer.StopAction(_spawnAccessoryAction);

            _spawnAccessoryAction = FixedPointTimer.StartNewConditionAction(EnableAccessory, condition => !_flurry.Active);

            ActiveProjectiles.Add(_flurry);
        }

        private void SpawnSword()
        {
            DisableAccessory();

            _spawnAccessoryAction =  FixedPointTimer.StartNewConditionAction(EnableAccessory, condition => !Projectile.Active);
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