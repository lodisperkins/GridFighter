using Lodis.Input;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_EnergyShield : Ability
    {
        private GameObject _shield;
        private ColliderBehaviour _shieldCollider;
        private float _shieldDrainValue;

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _shieldDrainValue = abilityData.GetCustomStatValue("EnergyDrainAmount");
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _shield = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, Owner.transform, true);
            _shieldCollider = _shield.GetComponent<ColliderBehaviour>();
            _shieldCollider.Spawner = Owner;

            _shieldCollider.AddCollisionEvent(collision =>
            {
                HitColliderBehaviour other = collision.Entity.GetComponent<HitColliderBehaviour>();

                if (!other)
                    return;

                //Spawns particles after block for player feedback
                if (BlackBoardBehaviour.Instance.BlockEffect)
                    ObjectPoolBehaviour.Instance.GetObject(BlackBoardBehaviour.Instance.BlockEffect.gameObject, other.transform.position + Vector3.up, Owner.transform.rotation);
            }
           );

            OwnerKnockBackScript.SetInvincibilityByCondition(condition => !InUse || CurrentAbilityPhase == AbilityPhase.RECOVER);
            PauseAbilityTimer();
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);

            OwnerMoveset.EnergyChargeEnabled = true;
        }

        protected override void OnEnd()
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);
            OwnerMoveset.EnergyChargeEnabled = true;
        }

        public override void Update()
        {
            base.Update();
            if (CurrentAbilityPhase != AbilityPhase.ACTIVE)
            {
                OwnerMoveset.EnergyChargeEnabled = true;
                return;
            }

            OwnerMoveset.EnergyChargeEnabled = false;
            int index = OwnerMoveset.GetSpecialAbilityIndex(this);

            if (!OwnerMoveset.TryUseEnergy(_shieldDrainValue * Time.deltaTime) || OwnerInput?.GetSpecialButton(index + 1) == false)
                UnpauseAbilityTimer();
        }
    }
}