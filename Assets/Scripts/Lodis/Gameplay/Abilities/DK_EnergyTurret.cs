using BBUnity.Actions;
using FixedPoints;
using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Types;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_EnergyTurret : SummonAbility
    {
        private EntityDataBehaviour _spawn;
        private FVector3 _spawnPosition;
        private ProjectileSpawnerBehaviour _projectileSpawner;
        private EntityDataBehaviour _largeLaserRef;
        private EntityDataBehaviour _laserRef;
        private int _shotCount;
        private Fixed32 _shotDelay;
        private Fixed32 _shotSpeed;
        private int _currentShotCount;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _laserRef = abilityData.Effects[0].GetComponent<EntityDataBehaviour>();
            _largeLaserRef = abilityData.Effects[1].GetComponent<EntityDataBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _shotCount = (int)abilityData.GetCustomStatValue("ShotCount");
            _shotDelay = abilityData.GetCustomStatValue("ShotDelay");
            _shotSpeed = abilityData.GetCustomStatValue("Speed");

            BlackBoardBehaviour.Instance.Grid.GetPanel(OwnerMoveScript.Position + FVector2.Right * OwnerMoveScript.GetAlignmentX(), out PanelBehaviour panel);

            _spawnPosition = panel.Position;

            PanelPositions[0] = _spawnPosition;
        }

        private void FireSmallShot()
        {

            _projectileSpawner.Owner = Owner;
            _projectileSpawner.Projectile = _laserRef;

            EntityDataBehaviour Projectile;
            Transform effect = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[2], _projectileSpawner.transform.position, Camera.main.transform.rotation).transform;

            effect.localScale /= 2;

            Projectile = _projectileSpawner.FireProjectile(_projectileSpawner.FixedTransform.Forward * _shotSpeed, GetColliderData(0));
            Projectile.FixedTransform.WorldPosition += FVector3.Up / 2;

            //Fire projectile
            Projectile.name += "(" + abilityData.name + _currentShotCount + ")";
            _currentShotCount++;
        }

        protected void FireLastShot()
        {
            _projectileSpawner.Projectile = _largeLaserRef;
            EntityDataBehaviour Projectile = _projectileSpawner.FireProjectile(_projectileSpawner.FixedTransform.Forward * _shotSpeed, GetColliderData(1));
            Projectile.FixedTransform.WorldPosition += FVector3.Up / 2;

            //Fire projectile
            Projectile.name += "(" + abilityData.name + "Large" + ")";
            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[2], OwnerMoveset.ProjectileSpawner.transform.position, Camera.main.transform.rotation);
            _spawn = ActiveEntities[0].Entity;

            _projectileSpawner = _spawn.GetComponentInChildren<ProjectileSpawnerBehaviour>();
            _projectileSpawner.Owner = Owner;

            FixedPointTimer.StartNewTimedAction(FireSmallShot, _shotDelay).Loop(_shotCount).OnComplete += FireLastShot;
        }

        protected override void OnMatchRestart()
        {
            if (_projectileSpawner)
                _projectileSpawner.StopAllCoroutines();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_spawn);
        }
    }
}