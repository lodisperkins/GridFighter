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
    public class DK_BlackHole : ProjectileAbility
    {
        private GameObject _blackHoleRef;
        private GameObject _blackHoleInstance;
        private float _travelDistance;
        private float _despawnTime;
        private TimedAction _despawnAction;

        //Called when ability is created
        public override void Init(GameObject newOwner)
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

            HitColliderSpawner.SpawnBoxCollider(Projectile.transform.position + Vector3.up * 0.5f, Vector3.one * 3, launchColliderData, owner);

            HitColliderBehaviour[] hitColliders = _blackHoleInstance.GetComponentsInChildren<HitColliderBehaviour>();

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].ColliderInfo = GetColliderData(i);
                hitColliders[i].Owner = owner;

                hitColliders[i].gameObject.SetActive(true);

                RoutineBehaviour.Instance.StartNewTimedAction(args =>
                ObjectPoolBehaviour.Instance.ReturnGameObject(_blackHoleInstance), TimedActionCountType.SCALEDTIME, _despawnTime + 0.2f);
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called

            //Spawn black hole.
            Projectile = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.ProjectileSpawner.transform.position, OwnerMoveset.ProjectileSpawner.transform.rotation);

            Vector2 direction = owner.transform.forward;

            //Unlike normal projectiles the black hole needs to stay in place for a short while. So grid movement is used instead of projectile physics.
            GridMovementBehaviour gridMovementBehaviour = Projectile.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.Position = _ownerMoveScript.Position;
            gridMovementBehaviour.Speed = abilityData.GetCustomStatValue("Speed");

            gridMovementBehaviour.MoveToPanel(_ownerMoveScript.Position + direction * _travelDistance, false, GridAlignment.ANY, true, false, true);
            ActiveProjectiles.Add(Projectile);
            gridMovementBehaviour.AddOnMoveEndTempAction(SpawnBlackHole);
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
            ObjectPoolBehaviour.Instance.ReturnGameObject(_blackHoleInstance);
        }
    }
}