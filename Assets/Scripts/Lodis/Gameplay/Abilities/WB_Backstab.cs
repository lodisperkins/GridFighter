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

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            int posY = (int)OwnerMoveScript.Position.Y;
            int posX = 0;

            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
                posX = (int)(BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);

            PanelBehaviour panel = null;
            BlackBoardBehaviour.Instance.Grid.GetPanel(posX, posY, out panel);
            //SpawnTransform = panel.transform;

            //_chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, SpawnTransform.position + Vector3.up, SpawnTransform.rotation);
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

            Quaternion rotation = Quaternion.identity;

            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
                rotation = Quaternion.Euler(0, -90, 0);
            else if (OwnerMoveScript.Alignment == GridAlignment.RIGHT)
                rotation = Quaternion.Euler(0, 90, 0);

            //_projectileSpawner = Object.Instantiate(OwnerMoveset.ProjectileSpawner, SpawnTransform.position + Vector3.up, rotation);

            _projectileSpawner.Projectile = ProjectileRef;

            ShotDirection = (FixedPoints.FVector3)_projectileSpawner.transform.forward;

            Projectile = _projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), ProjectileColliderData, UseGravity);

            DisableAccessory();

            RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !Projectile.Active);

            //Fire projectile
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