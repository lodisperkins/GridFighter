using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_DeckRebooter : ProjectileAbility
    {
        private float _stunTime;
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _stunTime = abilityData.GetCustomStatValue("Stun Time");
        }

        private void RebootDeck(params object[] args)
        {
            //Only check knockback if a player was hit.
            GameObject other = (GameObject)args[0];
            if (!other.CompareTag("Player"))
                return;

            KnockbackBehaviour knockback = other.GetComponent<KnockbackBehaviour>();

            if (knockback.IsInvincible || knockback.IsIntangible)
                return;

            knockback.Stun(_stunTime);
            MovesetBehaviour moveset = other.GetComponent<MovesetBehaviour>();

            moveset.ManualShuffle(true);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called
            base.OnActivate(args);

            Projectile.GetComponent<HitColliderBehaviour>().AddCollisionEvent(RebootDeck);
        }
    }
}