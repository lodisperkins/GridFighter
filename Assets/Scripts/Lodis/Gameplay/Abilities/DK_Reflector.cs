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
            _shield = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform, true);
            _shieldCollider = _shield.GetComponent<ColliderBehaviour>();
            _shieldCollider.Owner = owner;

            _shieldCollider.AddCollisionEvent(parameters =>
            {
                if (parameters.Length < 2)
                    return;

                HitColliderBehaviour other = parameters[1] as HitColliderBehaviour;

                if (!other)
                    return;

                //Spawns particles after block for player feedback
                if (BlackBoardBehaviour.Instance.BlockEffect)
                    ObjectPoolBehaviour.Instance.GetObject(BlackBoardBehaviour.Instance.ReflectEffect.gameObject, other.transform.position + Vector3.up, owner.transform.rotation);

                MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, 0.1f);
                EndAbility();
            }
           );

            OwnerKnockBackScript.SetInvincibilityByCondition(condition => !InUse || CurrentAbilityPhase == AbilityPhase.RECOVER);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);
        }

        public override void EndAbility()
        {
            base.EndAbility();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_shield);
        }
    }
}