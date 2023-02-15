using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_EnergyTurret : ProjectileAbility
    {
        private GameObject _spawn;
        private GameObject _chargeEffectRef;
        private Vector3 _spawnPosition;
        private ProjectileSpawnerBehaviour _projectileSpawner;
        private GameObject _largeLaserRef;
        private int _shotCount;
        private float _shotDelay;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = Resources.Load<GameObject>("Effects/Charge_Darkness");
            _largeLaserRef = Resources.Load<GameObject>("Projectiles/Prototype2/LargeBlueLaser");
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(() => ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn));
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _shotCount = (int)abilityData.GetCustomStatValue("ShotCount");
            _shotDelay = abilityData.GetCustomStatValue("ShotDelay");
            PanelBehaviour panel;
            BlackBoardBehaviour.Instance.Grid.GetPanel(_ownerMoveScript.Position + Vector2.right * _ownerMoveScript.GetAlignmentX(), out panel);

            _spawnPosition = panel.transform.position + Vector3.up;
            _spawn = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, _spawnPosition, owner.transform.rotation);
            _spawn.GetComponent<GridTrackerBehaviour>().Marker = MarkerType.WARNING;
        }

        private IEnumerator FireShots()
        {

                _projectileSpawner.Owner = owner;
            for (int i = 0; i < _shotCount; i++)
            {
                Projectile = _projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), GetColliderData(0), UseGravity);

                //Fire projectile
                Projectile.name += "(" + abilityData.name + i + ")";
                ActiveProjectiles.Add(Projectile);

                yield return new WaitForSeconds(_shotDelay);
            }

            _projectileSpawner.projectile = _largeLaserRef;
            Projectile = _projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), GetColliderData(1), UseGravity);

            //Fire projectile
            Projectile.name += "(" + abilityData.name + "Large" + ")";
            ActiveProjectiles.Add(Projectile);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            _projectileSpawner = _spawn.GetComponent<ProjectileSpawnerBehaviour>();

            if (!_projectileSpawner)
                _projectileSpawner = _spawn.AddComponent<ProjectileSpawnerBehaviour>();

            _projectileSpawner.projectile = ProjectileRef;
            SpawnTransform = _projectileSpawner.transform;
            ShotDirection = _projectileSpawner.transform.forward;

            _projectileSpawner.StartCoroutine(FireShots());
        }

        public override void StopAbility()
        {
            base.StopAbility();
            if (_projectileSpawner)
                _projectileSpawner.StopAllCoroutines();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);

        }
    }
}