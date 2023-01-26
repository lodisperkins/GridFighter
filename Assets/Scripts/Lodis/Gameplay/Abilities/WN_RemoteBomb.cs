using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WN_RemoteBomb : ProjectileAbility
    {
        private HitColliderData _explosionColliderData;
        private float _damage;
        private float _baseKnockback;
        private float _damageIncreaseRate;
        private float _knockbackIncreaseRate;
        private float _timeSpawned;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _damage = ProjectileColliderData.Damage;
            _baseKnockback = ProjectileColliderData.BaseKnockBack;

            _damageIncreaseRate = abilityData.GetCustomStatValue("DamageIncreaseRate");
            _knockbackIncreaseRate = abilityData.GetCustomStatValue("KnockbackIncreaseRate");
            ProjectileColliderData = new HitColliderData();

            ProjectileColliderData.OnHit += collisionInfo =>
            {
                GameObject other = (GameObject)collisionInfo[0];
                if (other.CompareTag("Structure"))
                    DestroyActiveProjectiles();

            };

            _explosionColliderData = GetColliderData(0);
        }


        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called

            Vector2 direction = owner.transform.forward;

            if (ActiveProjectiles.Count == 0)
            {
                base.OnActivate(args);
                _timeSpawned = Time.time;
            }
            else
            {
                float timeElapsed = Time.time - _timeSpawned;

                HitColliderData scaledData = _explosionColliderData;

                scaledData.Damage = _damage + _damageIncreaseRate * timeElapsed;
                scaledData.BaseKnockBack = _baseKnockback + _knockbackIncreaseRate * timeElapsed;

                HitColliderSpawner.SpawnBoxCollider(Projectile.transform.position, Vector3.one, scaledData, owner);

                ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
            }

        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }
    }
}