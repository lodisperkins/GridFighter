using FixedPoints;
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
    public class SS_AxeKick : Ability
    {
        private HitColliderBehaviour[] colliders;

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
            colliders = instance.GetComponentsInChildren<HitColliderBehaviour>();

            foreach (HitColliderBehaviour collider in colliders)
            {
                collider.Spawner = Owner;
                collider.ColliderInfo = GetColliderData(0);
                collider.ColliderInfo = collider.ColliderInfo.ScaleStats((Fixed32)args[0]);
                collider.ColliderInfo.OwnerAlignement = OwnerMoveScript.Alignment;
                collider.ColliderInfo.AddOnHitEvent(OnHit);
            }
        }

        private void CleanUpColliders()
        {
            if (colliders == null || colliders.Length == 0)
                return;

            foreach (HitColliderBehaviour collider in colliders)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(collider.Entity, true);
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            CleanUpColliders();
        }
    }
}