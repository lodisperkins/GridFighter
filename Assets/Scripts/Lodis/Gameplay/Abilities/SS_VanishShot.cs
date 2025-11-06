using FixedPoints;
using Lodis.Accessories;
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
    public class SS_VanishShot : ProjectileAbility
    {
        private FixedAction _shotAction;
        private Transform _heldItemSpawn;
        private AccessoryEffectBehaviour _enforcerInstance;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.MoveToPanel(OwnerMoveScript.CurrentPanel, true);


            _heldItemSpawn = OwnerMoveset.HeldItemSpawnLeft;
            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                _heldItemSpawn = OwnerMoveset.HeldItemSpawnRight;

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);
            _enforcerInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.Visual, _heldItemSpawn, true).GetComponent<AccessoryEffectBehaviour>();
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called
            base.OnActivate(args);

            PanelBehaviour movePanel;

            FVector2 panelOffset = (FVector2)args[1];
            FVector2 destination = GridBehaviour.Instance.ClampPanelPosition(OwnerMoveScript.Position + (panelOffset * 2), GridAlignment.ANY);

            if (!GridBehaviour.Instance.GetPanel(destination, out movePanel))
            {
                movePanel = OwnerMoveScript.CurrentPanel;
            }

            OwnerMoveScript.CancelMovement();
            GameObject teleportEffect = abilityData.Effects[0];

            OwnerMoveScript.TeleportToPanel(movePanel, abilityData.timeActive / 3, false, teleportEffect);
            _shotAction = FixedPointTimer.StartNewTimedAction(() => base.OnActivate(args), abilityData.timeActive);
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            ObjectPoolBehaviour.Instance.GetObject(abilityData.Accessory.SpawnEffect, _heldItemSpawn, true);

            if (_enforcerInstance != null)
            {
                GridGame.RemoveEntityFromGame(_enforcerInstance.GetComponent<EntityDataBehaviour>(), true);
                _enforcerInstance = null;
            }
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            if (_shotAction != null)
            {
                _shotAction.Stop();
                _shotAction = null;
            }
        }
    }
}