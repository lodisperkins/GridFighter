using FixedPoints;
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
    public class WB_Backstab : ProjectileAbility
    {
        private ProjectileSpawnerBehaviour _projectileSpawner;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private FVector3 _spawnPosition;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            //Get the info and spawn the charge effect to warn the opponent.
            int posY = (int)OwnerMoveScript.Position.Y;
            int posX = 0;

            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
                posX = (int)(BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);

            PanelBehaviour panel = null;
            BlackBoardBehaviour.Instance.Grid.GetPanel(posX, posY, out panel);
            _spawnPosition = panel.FixedWorldPosition;

            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, (Vector3)_spawnPosition + Vector3.up, Quaternion.identity);
            _chargeEffect.AddComponent<GridTrackerBehaviour>().Marker = MarkerType.WARNING;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);

            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Make the projectile face the person who spawned it based on alignment.
            FQuaternion rotation = FQuaternion.Identity;
            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
                rotation = FQuaternion.Euler(0, 270, 0);
            else if (OwnerMoveScript.Alignment == GridAlignment.RIGHT)
                rotation = FQuaternion.Euler(0, 90, 0);

            //Spawn and orient the projectile.
            _projectileSpawner = GridGame.SpawnEntity(OwnerMoveset.ProjectileSpawner, _spawnPosition + FVector3.Up,  FVector3.One, rotation);

            _projectileSpawner.Projectile = ProjectileRef;

            ShotDirection = _projectileSpawner.FixedTransform.Forward;

            //Fire projectile.
            Projectile = _projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), ProjectileColliderData, UseGravity);

            DisableAccessory(c => !Projectile.gameObject.activeInHierarchy);

            ActiveProjectiles.Add(Projectile);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            if (_projectileSpawner)
                Object.Destroy(_projectileSpawner.gameObject);

            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            if (_projectileSpawner)
                Object.Destroy(_projectileSpawner.gameObject);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }
    }
}