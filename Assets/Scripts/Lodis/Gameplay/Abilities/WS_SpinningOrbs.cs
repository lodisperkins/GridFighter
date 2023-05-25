using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    ///  Two energy orbs orbits around the character damaging anything in range. 
    /// </summary>
    public class WS_SpinningOrbs : Ability
    {
        private GameObject _orbs;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            DisableAccessory();
            _orbs = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform, true);
            _orbs.transform.position = new Vector3( _orbs.transform.position.x, abilityData.GetCustomStatValue("OrbHeight"), _orbs.transform.position.z);

            HitColliderBehaviour hitColliderBehaviour = _orbs.GetComponent<HitColliderBehaviour>();

            hitColliderBehaviour.ColliderInfo = GetColliderData(0);
            hitColliderBehaviour.Owner = owner;

            RotationBehaviour rotation = _orbs.GetComponent<RotationBehaviour>();

            rotation.RotateOnSelf = true;
            rotation.Speed = abilityData.GetCustomStatValue("RotationSpeed");

            OwnerAnimationScript.gameObject.SetActive(false);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
            OwnerAnimationScript.gameObject.SetActive(true);
            AnimationClip clip = null;
            abilityData.GetAdditionalAnimation(0, out clip);

            OwnerAnimationScript.PlayAnimation(clip);
            EnableAccessory();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }

        public override void StopAbility()
        {
            base.StopAbility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);

            OwnerAnimationScript.gameObject.SetActive(true);

        }
    }
}