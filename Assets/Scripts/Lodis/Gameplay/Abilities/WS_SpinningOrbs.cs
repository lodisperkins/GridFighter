using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WS_SpinningOrbs : Ability
    {
        private GameObject _orbs;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _orbs = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform, true);
            _orbs.transform.position = new Vector3( _orbs.transform.position.x, abilityData.GetCustomStatValue("OrbHeight"), _orbs.transform.position.z);

            HitColliderBehaviour hitColliderBehaviour = _orbs.GetComponent<HitColliderBehaviour>();

            hitColliderBehaviour.ColliderInfo = GetColliderData(0);
            hitColliderBehaviour.Owner = owner;

            RotationBehaviour rotation = _orbs.GetComponent<RotationBehaviour>();

            rotation.RotateOnSelf = true;
            rotation.Speed = abilityData.GetCustomStatValue("RotationSpeed");
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
        }

        public override void StopAbility()
        {
            base.StopAbility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);

        }
    }
}