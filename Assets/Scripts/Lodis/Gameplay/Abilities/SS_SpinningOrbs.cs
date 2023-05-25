using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Two stronger energy orbs orbits around the character damaging anything in range. 
    /// </summary>
    public class SS_SpinningOrbs : Ability
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
            _orbs.transform.position = new Vector3(_orbs.transform.position.x, abilityData.GetCustomStatValue("OrbHeight"), _orbs.transform.position.z);

            HitColliderBehaviour hitColliderBehaviour = _orbs.GetComponent<HitColliderBehaviour>();

            HitColliderData data = GetColliderData(0).ScaleStats((float)args[0]);

            hitColliderBehaviour.ColliderInfo = data;
            hitColliderBehaviour.Owner = owner;

            RotationBehaviour rotation = _orbs.GetComponent<RotationBehaviour>();

            rotation.RotateOnSelf = true;
            rotation.Speed = abilityData.GetCustomStatValue("RotationSpeed");
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
        }

        public override void StopAbility()
        {
            base.StopAbility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);

        }
    }
}