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
    public class DK_EnergyTurret : SummonAbility
    {
        private GameObject _spawn;
        private GameObject _chargeEffectRef;
        private Vector3 _spawnPosition;
        private ProjectileSpawnerBehaviour _projectileSpawner;
        private GameObject _largeLaserRef;
        private GameObject _laserRef;
        private int _shotCount;
        private float _shotDelay;
        private float _shotSpeed;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _laserRef = abilityData.Effects[0];
            _largeLaserRef = abilityData.Effects[1];
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _shotCount = (int)abilityData.GetCustomStatValue("ShotCount");
            _shotDelay = abilityData.GetCustomStatValue("ShotDelay");
            _shotSpeed = abilityData.GetCustomStatValue("Speed");

            PanelBehaviour panel;
            BlackBoardBehaviour.Instance.Grid.GetPanel(OwnerMoveScript.Position + Vector2.right * OwnerMoveScript.GetAlignmentX(), out panel);

            _spawnPosition = panel.Position;

            PanelPositions[0] = _spawnPosition;
        }

        private IEnumerator FireShots()
        {

            _projectileSpawner.Owner = owner;
            GameObject Projectile = null;
            _projectileSpawner.Projectile = _laserRef;

            for (int i = 0; i < _shotCount; i++)
            {
                Projectile = _projectileSpawner.FireProjectile(_projectileSpawner.transform.forward * _shotSpeed, GetColliderData(0));

                //Fire projectile
                Projectile.name += "(" + abilityData.name + i + ")";

                yield return new WaitForSeconds(_shotDelay);
            }

            _projectileSpawner.Projectile = _largeLaserRef;
            Projectile = _projectileSpawner.FireProjectile(_projectileSpawner.transform.forward * _shotSpeed, GetColliderData(1));

            //Fire projectile
            Projectile.name += "(" + abilityData.name + "Large" + ")";
            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            _spawn = ActiveEntities[0].gameObject;

            _projectileSpawner = _spawn.GetComponentInChildren<ProjectileSpawnerBehaviour>();
            _projectileSpawner.Owner = owner;

            _projectileSpawner.StartCoroutine(FireShots());
        }

        protected override void OnMatchRestart()
        {
            if (_projectileSpawner)
                _projectileSpawner.StopAllCoroutines();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);
        }
    }
}