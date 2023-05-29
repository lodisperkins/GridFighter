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
        private GameObject _effectInstance;
        private GameObject _thalamusInstance;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _effectInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], owner.transform.position, Quaternion.identity);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_effectInstance);
            DisableAccessory();
            _orbs = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform, true);
            _orbs.transform.position = new Vector3(_orbs.transform.position.x, abilityData.GetCustomStatValue("OrbHeight"), _orbs.transform.position.z);

            HitColliderBehaviour hitColliderBehaviour = _orbs.GetComponent<HitColliderBehaviour>();

            hitColliderBehaviour.ColliderInfo = GetColliderData(0);
            hitColliderBehaviour.Owner = owner;

            OwnerAnimationScript.gameObject.SetActive(false);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, OwnerMoveset.HeldItemSpawnLeft, true);
            _thalamusInstance.transform.localRotation = Quaternion.identity;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
            OwnerAnimationScript.gameObject.SetActive(true);
            AnimationClip clip = null;
            abilityData.GetAdditionalAnimation(0, out clip);

            OwnerAnimationScript.PlayAnimation(clip);
            _effectInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[1], owner.transform.position + Vector3.up, Quaternion.identity);

        }

        protected override void OnEnd()
        {
            base.OnEnd();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_effectInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);

            OwnerAnimationScript.gameObject.SetActive(true);
            EnableAccessory();
        }

        public override void StopAbility()
        {
            base.StopAbility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_effectInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);

            OwnerAnimationScript.gameObject.SetActive(true);

            EnableAccessory();
        }
    }
}