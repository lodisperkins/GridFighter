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
    public class DK_BlackHole : ProjectileAbility
    {
        private GameObject _blackHoleRef;
        private GameObject _blackHoleInstance;
        private float _travelDistance;
        private float _despawnTime;
        private TimedAction _despawnAction;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            _blackHoleRef = Resources.Load<GameObject>("Projectiles/Prototype2/BlackHole");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _travelDistance = abilityData.GetCustomStatValue("TravelDistance");
            _despawnTime = abilityData.GetCustomStatValue("DespawnTime");
        }

        private void SpawnBlackHole()
        {
            _blackHoleInstance = ObjectPoolBehaviour.Instance.GetObject(_blackHoleRef, Projectile.transform.position, _blackHoleRef.transform.rotation);

            HitColliderData launchColliderData = GetColliderData(3);
            launchColliderData.TimeActive = _despawnTime;

            //HitColliderSpawner.SpawnCollider(Projectile.transform.position + Vector3.up * 0.5f,3, 3, launchColliderData, Owner.Data);

            HitColliderBehaviour[] hitColliders = _blackHoleInstance.GetComponentsInChildren<HitColliderBehaviour>();

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].ColliderInfo = GetColliderData(i);
                hitColliders[i].Owner = Owner.Data;

                hitColliders[i].gameObject.SetActive(true);

                _despawnAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                ObjectPoolBehaviour.Instance.ReturnGameObject(_blackHoleInstance), TimedActionCountType.SCALEDTIME, _despawnTime + 0.2f);
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called

            //Spawn black hole.
            EntityDataBehaviour projectileData = abilityData.visualPrefab.GetComponent<EntityDataBehaviour>();
            Projectile = ObjectPoolBehaviour.Instance.GetObject(projectileData, OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition, OwnerMoveset.FixedTransform.WorldRotation);

            FVector2 direction = new FVector2(Owner.transform.forward.x, Owner.transform.forward.y);

            //Unlike normal projectiles the black hole needs to stay in place for a short while. So grid movement is used instead of projectile physics.
            GridMovementBehaviour gridMovementBehaviour = Projectile.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.Position = OwnerMoveScript.Position;
            gridMovementBehaviour.Speed = abilityData.GetCustomStatValue("Speed");

            gridMovementBehaviour.MoveToPanel(OwnerMoveScript.Position + direction * (Fixed32)_travelDistance, false, GridAlignment.ANY, true, false, true);
            ActiveProjectiles.Add(Projectile);
            gridMovementBehaviour.AddOnMoveEndTempAction(SpawnBlackHole);
        }

        protected override void OnMatchRestart()
        {
            RoutineBehaviour.Instance.StopAction(_despawnAction);
            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_blackHoleInstance);
        }
    }
}