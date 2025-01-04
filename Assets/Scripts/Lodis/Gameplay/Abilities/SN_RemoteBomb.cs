using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SN_EnergyMine : ProjectileAbility
    {
        private HitColliderData _explosionColliderData;
        private Fixed32 _travelDistance;
        private Fixed32 _damage;
        private Fixed32 _timeSpawned;
        private Fixed32 _despawnTime;
        private FixedTimeAction _despawnAction;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _damage = ProjectileColliderData.Damage;

            _travelDistance = abilityData.GetCustomStatValue("TravelDistance");
            _despawnTime = abilityData.GetCustomStatValue("DespawnTime");
            _explosionColliderData = GetColliderData(0);
        }

        private void SpawnExplosion()
        {
            HitColliderData data = _explosionColliderData.ScaleStats(_damage);
            HitColliderSpawner.SpawnCollider(Projectile.FixedTransform.WorldPosition + FVector3.Up * new Fixed32(32768), 3, 3, data, Owner);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _damage = (Fixed32)args[0];

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], OwnerMoveset.ProjectileSpawner.transform.position, Camera.main.transform.rotation);

            //The base activate func fires a single instance of the projectile when called

            CleanProjectileList();

            //Spawn remote bomb if none are out.
            if (ActiveProjectiles.Count > 0)
            {
                return;
            }

            EntityDataBehaviour projectileData = abilityData.visualPrefab.GetComponent<EntityDataBehaviour>();
            Projectile = ObjectPoolBehaviour.Instance.GetObject(projectileData, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldRotation);

            FVector2 direction = new FVector2(Owner.transform.forward.x, Owner.transform.forward.y);
            //Bomb using grid movement to find the panel it should stay on. 
            //Unlike normal projectiles the bomb needs to stay in place for a short while.
            GridMovementBehaviour gridMovementBehaviour = Projectile.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.Position = OwnerMoveScript.Position;
            gridMovementBehaviour.Speed = abilityData.GetCustomStatValue("Speed");
            gridMovementBehaviour.MoveToAlignedSideWhenStuck = false;

            gridMovementBehaviour.MoveToPanel(OwnerMoveScript.Position + direction * _travelDistance, false, GridAlignment.ANY, true, false, true);
            _timeSpawned = GridGame.Time;
            ActiveProjectiles.Add(Projectile);
            HitColliderData data = _explosionColliderData.ScaleStats(_damage);
            //data.OnHit += a => SpawnExplosion();

            HitColliderBehaviour collider = Projectile.GetComponent<HitColliderBehaviour>();
            collider.ColliderInfo = data;
            collider.ColliderInfo.TimeActive = _despawnTime;
            collider.Spawner = Owner;
            collider.Entity.Data.ClearEndEvent();
            collider.Entity.Data.OnEnd += () => _despawnAction.Stop();

            //collider.CollisionEnabled = false;
            Projectile.GetComponent<GridMovementBehaviour>().AddOnMoveEndTempAction(() => collider.CollisionEnabled = true);
            //HitColliderSpawner.SpawnCollider(Projectile.FixedTransform, 3, 3, data, Owner);
            //Sets a new timer to explode the bomb by default.
            _despawnAction = FixedPointTimer.StartNewTimedAction(SpawnExplosion, _despawnTime);
            ActiveProjectiles.Add(Projectile);

        }

        protected override void OnEnd()
        {
            base.OnEnd();
            _despawnAction.Stop();
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }
    }
}