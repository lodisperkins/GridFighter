using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a stronger, slow moving shot.
    /// The shot travels for 2 panels before dissipating.
    /// </summary>
    public class WF_ForwardShot : ProjectileAbility
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 2;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WF_ForwardShot_Data"));
            owner = newOwner;
            _projectileCollider = new HitColliderBehaviour(1, 1, 0.2f, true, 1.5f, owner, true);

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

        public void SpawnProjectile()
        {
            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            if (spawnerObject.transform.position.y > BlackBoardBehaviour.Instance.projectileHeight)
            {
                spawnerObject.transform.position = new Vector3
                    (spawnerObject.transform.position.x,
                    BlackBoardBehaviour.Instance.projectileHeight,
                    owner.transform.position.z);
            }

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            //Fire laser
            GameObject newProjectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            _activeProjectiles.Add(newProjectile);

            MonoBehaviour.Destroy(spawnerObject);
        }

        protected override void Activate(params object[] args)
        {
            _projectileCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("KnockBackScale"),
                 abilityData.GetCustomStatValue("HitAngle"), true, abilityData.GetCustomStatValue("Lifetime"), owner, true, false, true, abilityData.GetCustomStatValue("HitStun"));
            _projectileCollider.IgnoreColliders = abilityData.IgnoreColliders;
            _projectileCollider.Priority = abilityData.ColliderPriority;

            CleanProjectileList();
            
            Vector2 moveDir = owner.transform.forward;

            if (_activeProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            {
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + moveDir))
                    _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
        }
    }
}