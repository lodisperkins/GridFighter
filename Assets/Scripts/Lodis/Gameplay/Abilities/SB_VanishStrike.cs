using FixedPoints;
using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SB_VanishStrike : Ability
    {
        private PanelBehaviour _spawnPanel;
        private HitColliderBehaviour _hitCollider;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);


            //Get the info and spawn the charge effect to warn the opponent.
            int posY = (int)OwnerMoveScript.Position.Y;
            int posX = 0;

            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
                posX = (int)(BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);

            BlackBoardBehaviour.Instance.Grid.GetPanel(posX, posY, out _spawnPanel);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
            GameObject teleportEffect = abilityData.Effects[0];

            OwnerMoveScript.TeleportToPanel(_spawnPanel, 0, false, teleportEffect);
            OwnerMoveScript.Move(new FVector2(2, 0) * -OwnerMoveScript.GetAlignmentX(), tempAlignment: GridAlignment.ANY, canBeOccupied: true, reservePanel: false, clampPosition: true);

            _hitCollider = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, 1, 1, GetColliderData(0), Owner);
            _hitCollider.ColliderInfo.ScaleStats((Fixed32)args[0]);
        }

        protected void ReturnHitCollider()
        {
            if (_hitCollider != null)
            {
                _hitCollider.FixedTransform.Parent = null;
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitCollider.Entity);
                _hitCollider = null;
            }
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            ReturnHitCollider();
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
            ReturnHitCollider();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            ReturnHitCollider();
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}