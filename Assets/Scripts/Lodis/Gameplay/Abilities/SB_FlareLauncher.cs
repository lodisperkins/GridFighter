using FixedPoints;
using Lodis.FX;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SB_FlareLauncher : Ability
    {
        private CollisionGroupBehaviour _hitColliderBehaviour;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            FXManagerBehaviour.Instance.SpawnExplosion(Owner.transform.position - Vector3.up * 0.5f, 2);

            EntityDataBehaviour instance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), Owner.FixedTransform.WorldPosition, FQuaternion.Identity);

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.LEFT)
                instance.FixedTransform.WorldRotation = FQuaternion.Identity;
            else
                instance.FixedTransform.WorldRotation = FQuaternion.Euler(0, 180, 0);

            _hitColliderBehaviour = instance.GetComponent<CollisionGroupBehaviour>();

            HitColliderData colliderData = GetColliderData(0);
            colliderData.ScaleStats((Fixed32)args[0]);
            _hitColliderBehaviour.SetHitCollisionInfo(colliderData, Owner);
        }

        protected override void OnEnd()
        {
            base.OnRecover(null);

            if (_hitColliderBehaviour)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitColliderBehaviour.Entity);
        }
    }
}