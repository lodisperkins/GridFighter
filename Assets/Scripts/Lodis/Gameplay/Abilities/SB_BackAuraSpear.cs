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
    public class SB_BackAuraSpear : ProjectileAbility
    {
        private ProjectileSpawnerBehaviour _projectileSpawner;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _chargeEffectRef = Resources.Load<GameObject>("Effects/Charge_Darkness");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            int posY = (int)_ownerMoveScript.Position.y;
            int posX = 0;

            if (_ownerMoveScript.Alignment == GridAlignment.LEFT)
                posX = (int)(BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);

            PanelBehaviour panel = null;
            BlackBoardBehaviour.Instance.Grid.GetPanel(posX, posY, out panel);
            SpawnTransform = panel.transform;

            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, SpawnTransform.position + Vector3.up, SpawnTransform.rotation);
            _chargeEffect.GetComponent<GridTrackerBehaviour>().Marker = MarkerType.WARNING;
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

            if (_ownerMoveScript.Alignment == GridAlignment.LEFT)
                rotation = Quaternion.Euler(0, -90, 0);
            else if (_ownerMoveScript.Alignment == GridAlignment.RIGHT)
                rotation = Quaternion.Euler(0, 90, 0);

            _projectileSpawner = Object.Instantiate(OwnerMoveset.ProjectileSpawner, SpawnTransform.position + Vector3.up, rotation);

            _projectileSpawner.projectile = ProjectileRef;

            ShotDirection = _projectileSpawner.transform.forward;

            HitColliderData data = ProjectileColliderData.ScaleStats((float)args[0]);

            Projectile = _projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), data, UseGravity);

            //Fire projectile
            ActiveProjectiles.Add(Projectile);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            if (_projectileSpawner)
                Object.Destroy(_projectileSpawner.gameObject);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        public override void StopAbility()
        {
            base.StopAbility();

            if (_projectileSpawner)
                Object.Destroy(_projectileSpawner.gameObject);

            ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }
    }
}