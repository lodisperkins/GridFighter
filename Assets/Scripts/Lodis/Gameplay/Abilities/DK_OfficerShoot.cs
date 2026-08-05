using Lodis.Accessories;
using Lodis.AI;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_OfficerShoot : ProjectileAbility
    {
        private Transform _heldItemSpawn;
        private AccessoryEffectBehaviour _enforcerInstance;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            NetworkAttackNPCBehaviour officer = Owner.GetComponent<NetworkAttackNPCBehaviour>();

            if (officer != null)
            {
                OwnerMoveset.ProjectileSpawner.Owner = officer.Owner;
            }
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true).GetComponent<AccessoryEffectBehaviour>();
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called
            base.OnActivate(args);
        }

        protected override void OnEnd()
        {
            base.OnEnd();


            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);

            if (_enforcerInstance != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_enforcerInstance.GetComponent<EntityDataBehaviour>(), true);
                _enforcerInstance = null;
            }
        }
    }
}