using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_ChargeForwardShot : Ability
    {
        public Transform spawnTransform = null;
        private float _shotDamage = 15;
        private float _shotKnockBack = 1;
        //Used to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private List<GameObject> _activeProjectiles = new List<GameObject>();

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SF_ChargeForwardShot_Data"));

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
                    spawnerObject.transform.position.z);
            }

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            //Fire laser
            GameObject newProjectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            _activeProjectiles.Add(newProjectile);

            MonoBehaviour.Destroy(spawnerObject);
        }

        /// <summary>
        /// Removes the projectiles that have despawn fromt the active list
        /// </summary>
        public void CleanProjectileList()
        {
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i] == null)
                {
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        protected override void Activate(params object[] args)
        {
            //Initialize collider stats
            float powerScale = (float)args[0];
            _shotDamage = abilityData.GetCustomStatValue("Damage") * powerScale;
            _shotKnockBack = abilityData.GetCustomStatValue("KnockBackScale") * powerScale;
            _projectileCollider = new HitColliderBehaviour(_shotDamage, _shotKnockBack,
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.GetCustomStatValue("Lifetime"), owner, true);
            _projectileCollider.IgnoreColliders = abilityData.IgnoreColliders;
            _projectileCollider.Priority = abilityData.ColliderPriority;

            CleanProjectileList();
            
            Vector2 moveDir = owner.transform.forward;

            //If the maximum amount of instances has been reached for this owner, don't spawn a new one
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