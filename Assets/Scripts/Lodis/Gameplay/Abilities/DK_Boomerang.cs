using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_Boomerang : ProjectileAbility
    {
        private Vector3 _originalTravelDirection;
        private int _reboundCount;
        private ColliderBehaviour _reboundCollider;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            projectile = (GameObject)Resources.Load("Projectiles/CrossProjectile");
            DestroyOnHit = false;
            IsMultiHit = true;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            base.Activate(args);
            projectileCollider.HitFrames = 1;
            _reboundCollider = _activeProjectiles[0].AddComponent<ColliderBehaviour>();

            _reboundCollider.Init(false, 0, owner, false, true);
            _reboundCollider.OnHit += TryRedirectProjectile;

        }

        public void TryRedirectProjectile(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (_reboundCount == abilityData.GetCustomStatValue("MaxRebounds"))
                MonoBehaviour.Destroy(_activeProjectiles[0]);

            Rigidbody projectile = _activeProjectiles[0].GetComponent<Rigidbody>();

            if ((other.CompareTag("Player") || other.CompareTag("Entity")))
            {
                projectile.AddForce(-projectile.velocity * 2, ForceMode.VelocityChange);
                _reboundCount++;
                _reboundCollider.ColliderOwner = other;
            }
            else if(other.CompareTag("Structure"))
            {
                MonoBehaviour.Destroy(_reboundCollider.gameObject);
            }
        }
    }
}