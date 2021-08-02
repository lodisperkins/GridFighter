using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots an arcing shot upwards that travels two panels.
    /// Can move over obstacles on the panel in front of it.
    /// </summary>
    public class WB_LobShot : Ability
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 20;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private float _shotDistance = 1;
        private Movement.GridMovementBehaviour _ownerMoveScript;
        private List<GameObject> _activeProjectiles = new List<GameObject>();

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WB_LobShot_Data"));
            owner = newOwner;
            
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/LobShot");
        }

        private Vector3 CalculateProjectileForce()
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacing;

            //Clamps hit angle to prevent completely horizontal movement
            float dot = Vector3.Dot(owner.transform.forward, Vector3.right);
            float shotAngle = 0;

            if (dot < 0)
                shotAngle = 2;
            else
                shotAngle = 1;

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * _shotDistance) + (panelSpacing * (_shotDistance - 1));
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
            return new Vector3(Mathf.Cos(shotAngle), Mathf.Sin(shotAngle)) * magnitude;
        }

        private void CleanProjectileList()
        {
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i] == null)
                {
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _projectileCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("KnockBackScale"),
                 abilityData.GetCustomStatValue("HitAngle"), true, abilityData.GetCustomStatValue("Lifetime"), owner, true);

            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.name + " could not be found.");
                return;
            }

            CleanProjectileList();

            if (_activeProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            Vector2 offSet = new Vector2(1, 0) * -owner.transform.forward;
            offSet.x = Mathf.RoundToInt(offSet.x);
            offSet.y = Mathf.RoundToInt(offSet.y);

            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offSet);
            //Fire laser
            spawnScript.FireProjectile(CalculateProjectileForce(), _projectileCollider);

            MonoBehaviour.Destroy(spawnerObject);
        }
    }
}