using FixedPoints;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Spin at high speeds with a blade in hand to generate a large tornado that can hit multiple times.
    /// </summary>
    public class SS_TTornado : Ability
    {
        private EntityDataBehaviour _orbs;
        private GameObject _effectInstance;
        private GameObject _thalamusInstance;
        private Transform _heldItemSpawn;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _effectInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], Owner.transform.position, Quaternion.identity);
            _effectInstance.transform.parent = null;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Handle vfx.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_effectInstance);
            DisableAccessory();

            //Collider spawning and setup.
            _orbs = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), Owner.FixedTransform.WorldPosition, Owner.FixedTransform.WorldRotation);
            _orbs.FixedTransform.Parent = Owner.FixedTransform;
            _orbs.FixedTransform.LocalPosition = FVector3.Zero;

            HitColliderBehaviour hitColliderBehaviour = _orbs.GetComponent<HitColliderBehaviour>();

            hitColliderBehaviour.ColliderInfo = GetColliderData(0);
            hitColliderBehaviour.Spawner = Owner;

            //Disabling the player visual here so it looks like they are in the tornado.
            OwnerAnimationScript.gameObject.SetActive(false);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            //Placing the sword back in their hand.
            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            _thalamusInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true);
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _thalamusInstance.transform.localRotation = Quaternion.identity;

            FixedPointTimer.StartNewTimedAction(() =>
            _thalamusInstance.GetComponent<ColorManagerBehaviour>().SetColors((int)OwnerMoveScript.Alignment), GridGame.FixedTimeStep);

            //Removing vfx.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
            OwnerAnimationScript.gameObject.SetActive(true);


            //Forcing the animation statemachine to go to the finish pose.
            AnimationClip clip = null;
            abilityData.GetAdditionalAnimation(0, out clip);

            OwnerAnimationScript.PlayAnimation(clip, 1, false, true);
            _effectInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[1], Owner.transform.position + Vector3.up, Quaternion.identity);

        }

        protected override void OnEnd()
        {
            base.OnEnd();

            //Clean up vfx.
            ObjectPoolBehaviour.Instance.ReturnGameObject(_orbs);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_effectInstance);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_thalamusInstance);
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.DespawnEffect, _heldItemSpawn, true);

            OwnerAnimationScript.gameObject.SetActive(true);
            EnableAccessory();
        }
    }
}