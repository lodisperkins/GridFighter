using BBUnity.Actions;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_RiotShield : Ability
    {
        private GameObject _shield;
        private HealthBehaviour _shieldHealth;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
        }

        protected void OnShieldHit(Fixed32 damage)
        {
            GameObject opp = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);

            _shieldHealth.TakeDamage(opp.GetComponent<EntityDataBehaviour>().Data, damage);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            if (_shield == null)
            {
                _shield = MonoBehaviour.Instantiate(abilityData.visualPrefab, Owner.transform);
                _shieldHealth = _shield.GetComponent<HealthBehaviour>();

                OwnerKnockBackScript.AddOnArmorHitAction(OnShieldHit);
            }

            OwnerKnockBackScript.EnableSuperArmor(HealthBehaviour.ArmorType.DamageThreshold, abilityData.GetCustomStatValue("DamageThreshold"));
            OwnerKnockBackScript.AddOnArmorBrokenAction(DestroyShield);
        }

        protected void DestroyShield()
        {
            if (_shield != null)
            {
                Vector3 spawnPosition = (_shield.transform.position + Owner.transform.forward + Vector3.up) * .5f ;
                MonoBehaviour.Instantiate(abilityData.Effects[0], spawnPosition, Camera.main.transform.rotation);
                MonoBehaviour.Destroy(_shield);
            }

            OwnerKnockBackScript.RemoveOnArmorBrokenAction(DestroyShield);
            OwnerKnockBackScript.RemoveOnArmorHitAction(OnShieldHit);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            DestroyShield();
        }
    }
}