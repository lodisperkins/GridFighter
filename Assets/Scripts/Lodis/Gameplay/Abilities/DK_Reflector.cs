using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_Reflector : Ability
    {
        private GameObject _shield;
        private ColliderBehaviour _shieldCollider;

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _shield = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, Owner.transform, true);
            _shieldCollider = _shield.GetComponent<ColliderBehaviour>();
            _shieldCollider.Spawner = Owner;

            _shieldCollider.AddCollisionEvent(collision =>
            {
                HitColliderBehaviour other = collision.OtherEntity.GetComponent<HitColliderBehaviour>();

                if (!other)
                    return;

                //Spawns particles after block for player feedback
                if (BlackBoardBehaviour.Instance.BlockEffect)
                    ObjectPoolBehaviour.Instance.GetObject(BlackBoardBehaviour.Instance.ReflectEffect.gameObject, other.transform.position + Vector3.up, Owner.transform.rotation);

                MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, 0.1f);
                EndAbility();
            }
           );

            OwnerKnockBackScript.SetInvincibilityByCondition(condition => !InUse || CurrentAbilityPhase == AbilityPhase.RECOVER);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);
        }

        protected override void OnEnd()
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);
        }
    }
}