using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots an arcing shot upwards that travels two panels.
    /// Can move over obstacles on the panel in front of it.
    /// On impact, four smaller lob shots spawn and travel to the nearest
    /// panel in all cardinal directions.
    /// </summary>
    public class SB_ChargeLobShot : Ability
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 20;
        //Usd to store a reference to the laser prefab
        private GameObject _strongProjectile;
        private GameObject _weakProjectile;
        private HitColliderBehaviour _strongProjectileCollider;
        //The collider attached to the laser
        private HitColliderBehaviour _weakProjectileCollider;
        private float _strongShotDistance = 1;
        private float _strongShotDamage = 10;
        private float _strongShotKnockBackScale = 1;
        private float _strongForceIncreaseRate = .05f;
        private float _weakShotDistance = 1;
        private float _weakShotDamage = 5;
        private float _weakShotForce = 1;
        private Movement.GridMovementBehaviour _ownerMoveScript;
        private Transform _weakSpawnTransform;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SB_ChargeLobShot_Data"));
            owner = newOwner;
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();

            //Load the projectile prefab
            _strongProjectile = (GameObject)Resources.Load("Projectiles/ChargeLobShot");
            _weakProjectile = (GameObject)Resources.Load("Projectiles/LobShot");
        }

        private void AddUpwardForce(params object[] args)
        {
            GameObject target = (GameObject)args[0];

            Movement.KnockbackBehaviour knockBackScript = target.GetComponent<Movement.KnockbackBehaviour>();
            
            if (!knockBackScript)
                return;

            knockBackScript.ApplyImpulseForce(Vector3.up * _weakShotForce);
        }

        private Vector3 CalculateProjectileForce(Vector3 axis, float shotDistance)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacing;

            Vector3 moveDir = new Vector3();

            if (axis == Vector3.right || axis == Vector3.left)
                moveDir = owner.transform.forward;
            else if (axis == Vector3.forward || axis == Vector3.back)
                moveDir = owner.transform.right;

            moveDir.Scale(axis);
            moveDir.Normalize();
            
            //Clamps hit angle to prevent completely horizontal movement
            float dot = Vector3.Dot(moveDir, axis);
            float shotAngle = 0;

            if (dot < 0)
                shotAngle = 2*Mathf.PI / 3;
            else
                shotAngle = Mathf.PI/3;

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * shotDistance) + (panelSpacing * (shotDistance - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * Physics.gravity.magnitude;
            float val2 = Mathf.Sin(2 * shotAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
            {
                return new Vector3();
            }

            //Return the knockback force
            return (moveDir * Mathf.Cos(shotAngle) + new Vector3(0, Mathf.Sin(shotAngle))) * magnitude;
        }

        private void SpawnWeakShot(Vector3 axis)
        {
            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = _weakSpawnTransform;
            spawnerObject.transform.localPosition = Vector3.up / 2;
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _weakProjectile;

            //Fire laser
            spawnScript.FireProjectile(CalculateProjectileForce(axis, _weakShotDistance), _weakProjectileCollider);

            MonoBehaviour.Destroy(spawnerObject);
        }

        private void SpawnWeakShots(params object[] args)
        {
            SpawnWeakShot(new Vector3(1, 0, 0));
            SpawnWeakShot(new Vector3(-1, 0, 0));
            SpawnWeakShot(new Vector3(0, 0, 1));
            SpawnWeakShot(new Vector3(0, 0, -1));
            _strongProjectileCollider.onHit = null;
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_strongProjectile)
            {
                Debug.LogError("Projectile for " + abilityData.name + " could not be found.");
                return;
            }

            float powerScale = (float)args[0];
            _weakShotDamage *= powerScale;
            _strongShotDamage *= powerScale;
            _strongShotKnockBackScale *= powerScale;
            _strongShotDistance += (powerScale - 1) / _strongForceIncreaseRate;

            _weakProjectileCollider = new HitColliderBehaviour(_weakShotDamage, 0, 0, true, 5, owner, true);
            _weakProjectileCollider.onHit += AddUpwardForce;
            _strongProjectileCollider = new HitColliderBehaviour(_strongShotDamage, _strongShotKnockBackScale, 2.5f, true, 5, owner, true);

            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _strongProjectile;

            Vector2 offSet = new Vector2(1, 0) * -owner.transform.forward;
            offSet.x = Mathf.RoundToInt(offSet.x);
            offSet.y = Mathf.RoundToInt(offSet.y);

            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offSet);

            _strongProjectileCollider.onHit += SpawnWeakShots;
            //Fire laser
            _weakSpawnTransform = spawnScript.FireProjectile(CalculateProjectileForce(owner.transform.forward, _strongShotDistance), _strongProjectileCollider).transform;

            MonoBehaviour.Destroy(spawnerObject);

            _weakShotDamage /= powerScale;
            _strongShotDamage /= powerScale;
            _strongShotKnockBackScale /= powerScale;
            _strongShotDistance -= (powerScale - 1) / _strongForceIncreaseRate;
        }
    }
}