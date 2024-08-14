using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_AuraCyclone : ProjectileAbility
    {
        private GameObject _thalamusInstance;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(Owner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            DisableAccessory();
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, OwnerMoveset.HeldItemSpawnLeft, true);
            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, OwnerMoveset.HeldItemSpawnLeft, true);
            _thalamusInstance.transform.localRotation = Quaternion.identity;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !Projectile.activeInHierarchy);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            EnableAccessory();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            EnableAccessory();
        }
    }
}