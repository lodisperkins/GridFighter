using FixedPoints;
using Lodis.Accessories;
using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_WarpBullet : ProjectileAbility
    {
        private HitColliderBehaviour _hitCollider;
        private Transform _heldItemSpawn;
        private AccessoryEffectBehaviour _enforcerInstance;
        private FixedTimeAction _kickTimer;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            //OwnerAnimationScript.AddEventListener("DK_WarpBullet_Teleport", SpawnKickHitBox);
            ObjectPoolBehaviour.Instance.OnReturnToPool.AddListener(RemoveHitColliderAsChild);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            OnHit += OnCollision;
            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true).GetComponent<AccessoryEffectBehaviour>();
        }

        private void SpawnKickHitBox()
        {
            _hitCollider = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, 2, 2, GetColliderData(1), Owner);
        }

        private void RemoveHitColliderAsChild(GameObject g)
        {
            if (_hitCollider == null || g != _hitCollider.gameObject)
                return;

            _hitCollider.FixedTransform.Parent = null;
        }

        private void OnCollision(Collision collision)
        {
            if (collision.OtherEntity.UnityObject.CompareTag("RingBarrier") && currentActivationAmount == 1)
            {
                EndAbility();
            }
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Debug.Log("DK_WarpBullet: " + currentActivationAmount);
            if (currentActivationAmount == 1)
            {
                //The base activate func fires a single instance of the projectile when called
                base.OnActivate(args);
                return;
            }
            
            PanelBehaviour projectilePanel;

            FVector3 projectilePos = Projectile.FixedTransform.WorldPosition;

            GridBehaviour.Instance.GetPanelAtLocationInWorld((Vector3)projectilePos, out projectilePanel);

            if (!projectilePanel)
                return;

            GameObject teleportEffect = abilityData.Effects[0];

            OwnerMoveScript.TeleportToPanel(projectilePanel, 0, false, teleportEffect);

            AnimationClip clip;

            if (!abilityData.GetAdditionalAnimation(0, out clip))
            {
                Debug.LogError("DK_WarpBullet: Could not find return animation!");
            }
            else
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);

                OwnerMoveScript.MoveToAlignedSideWhenStuck = false;

                Fixed32 kickTime = abilityData.GetCustomStatValue("KickTime");
                PauseAbilityTimer();

                if (_kickTimer == null)
                    _kickTimer = FixedPointTimer.StartNewTimedAction(UnpauseAbilityTimer, kickTime);
                else
                    _kickTimer.Reset();

                OwnerAnimationScript.PlayAnimation(kickTime, clip);
                FixedPointTimer.StartNewTimedAction(SpawnKickHitBox, GridGame.FixedTimeStep); 
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            _kickTimer?.Stop();
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);

            if (_enforcerInstance != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_enforcerInstance.GetComponent<EntityDataBehaviour>(), true);
                _enforcerInstance = null;
            }
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            currentActivationAmount = 0;
            _kickTimer?.Stop();
        }
    }
}