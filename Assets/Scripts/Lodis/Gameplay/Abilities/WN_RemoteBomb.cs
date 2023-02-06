using Lodis.GridScripts;
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
        private float _travelDistance;
        private  float _despawnTime;
        private TimedAction _despawnAction;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _travelDistance = abilityData.GetCustomStatValue("TravelDistance");
            _despawnTime = abilityData.GetCustomStatValue("DespawnTime");
            _explosionColliderData = GetColliderData(0);
        }

        private void SpawnExplosion()
        {

            HitColliderSpawner.SpawnBoxCollider(Projectile.transform.position + Vector3.up * 0.5f, Vector3.one, _explosionColliderData, owner);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called

            //Spawn remote bomb if none are out.
            if (ActiveProjectiles.Count == 0)
            {
                Projectile = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.ProjectileSpawner.transform.position, OwnerMoveset.ProjectileSpawner.transform.rotation);

                Vector2 direction = owner.transform.forward;
                //Bomb using grid movement to find the panel it should stay on. 
                //Unlike normal projectiles the bomb needs to stay in place for a short while.
                GridMovementBehaviour gridMovementBehaviour = Projectile.GetComponent<GridMovementBehaviour>();
                gridMovementBehaviour.Position = _ownerMoveScript.CurrentPanel.Position;
                gridMovementBehaviour.Speed = abilityData.GetCustomStatValue("Speed");

                gridMovementBehaviour.MoveToPanel(_ownerMoveScript.Position + direction * _travelDistance, false, GridAlignment.ANY, true, false, true);
                ActiveProjectiles.Add(Projectile);

                //Sets a new timer to explode the bomb by default.
                _despawnAction = RoutineBehaviour.Instance.StartNewTimedAction(parameters => SpawnExplosion(), TimedActionCountType.SCALEDTIME, _despawnTime);
            }
            //Otherwise the bomb is activated.
            else
            {
                RoutineBehaviour.Instance.StopAction(_despawnAction);
                SpawnExplosion();
            }

        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }

        public override void StopAbility()
        {
            base.StopAbility();
            RoutineBehaviour.Instance.StopAction(_despawnAction);
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }
    }
}