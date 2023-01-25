﻿using Lodis.Movement;
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
        private GameObject _explosionEffect;
        private GameObject _explosionEffectInstance;
        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _explosionEffect = (GameObject)Resources.Load("Effects/SmallExplosion");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            Projectile = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called
            GridMovementBehaviour gridMovementBehaviour = Projectile.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.CanBeWalkedThrough = true;

            Vector2 direction = owner.transform.forward;
            float distance = abilityData.GetCustomStatValue("Travel Distance");

            if (ActiveProjectiles.Count == 0)
                gridMovementBehaviour.MoveToPanel(_ownerMoveScript.Position + direction * distance, false, GridScripts.GridAlignment.ANY, true, false);
            else
            {
                Projectile.GetComponent<Collider>().enabled = true;
                _explosionEffectInstance = ObjectPoolBehaviour.Instance.GetObject(_explosionEffect);
            }

        }
    }
}