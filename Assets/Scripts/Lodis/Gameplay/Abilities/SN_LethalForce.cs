using FixedPoints;
using Lodis.Accessories;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SN_LethalForce : ProjectileAbility
    {
        private Transform _heldItemSpawn;
        private AccessoryEffectBehaviour _enforcerInstance;
        private FVector3 _originalPosition;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual.GetComponent<EntityDataBehaviour>(), (FVector3)_heldItemSpawn.position, (FQuaternion)_heldItemSpawn.rotation).GetComponent<AccessoryEffectBehaviour>();
            _enforcerInstance.transform.parent = _heldItemSpawn;
            _originalPosition = OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            // (1,1.7,0)
            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.WorldPosition = _enforcerInstance.ProjectileSpawnPoint.WorldPosition;
            //The base activate func fires a single instance of the projectile when called
            base.OnActivate(args);
        }

        protected override void OnEnd()
        {
            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition = _originalPosition;
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);

            base.OnEnd();

            if (_enforcerInstance != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_enforcerInstance.GetComponent<EntityDataBehaviour>());
                _enforcerInstance = null;
            }
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            OwnerMoveset.ProjectileSpawner.Entity.FixedTransform.LocalPosition = _originalPosition;
        }
    }
}