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
    public class WB_FlareLauncher : Ability
    {
        private HitColliderBehaviour _hitColliderBehaviour;

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            EntityDataBehaviour instance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), Owner.FixedTransform.WorldPosition, FQuaternion.Identity);

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.LEFT)
                instance.FixedTransform.WorldRotation = FQuaternion.Identity;
            else
                instance.FixedTransform.WorldRotation = FQuaternion.Euler(0, 180, 0);

            _hitColliderBehaviour = instance.GetComponent<HitColliderBehaviour>();

            _hitColliderBehaviour.ColliderInfo = GetColliderData(0);
            _hitColliderBehaviour.Spawner = Owner;
        }

        protected override void OnEnd()
        {
            base.OnRecover(null);

            if (_hitColliderBehaviour)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitColliderBehaviour.gameObject);
        }
    }
}