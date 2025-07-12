using FixedPoints;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WS_AxeKick : Ability
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
            EntityDataBehaviour instance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), Owner.FixedTransform);

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.LEFT)
                instance.FixedTransform.WorldRotation = FQuaternion.Identity;
            else
                instance.FixedTransform.WorldRotation = FQuaternion.Euler(0, 180, 0);

            _hitColliderBehaviour = instance.GetComponent<CollisionGroupBehaviour>();

            _hitColliderBehaviour.SetHitCollisionInfo(GetColliderData(0), Owner);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            if (_hitColliderBehaviour)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitColliderBehaviour.Entity, true);

            }
        }
    }
}