﻿using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Travel two panels forward, and deliver a strike that covers a 3x3 panel radius.
    /// </summary>
    public class DK_RadialStrike : Ability
    {
        private float _panelTravelDistance;
        private HitColliderBehaviour _hitCollider;
        private GameObject _visualPrefabInstance;
        private bool _inPosition = false;
        private bool _deactivated = false;
        private Vector3 _hitBoxScale;
        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _deactivated = false;
        }

        private void SpawnHitBox()
        {
            //Don't spawn the hitbox if the ability was told to deactivate
            if (_deactivated)
                return;

            //Play animation now that the character has reached the target panel
            OwnerAnimationScript.PlayAbilityAnimation();

            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;

            //Mark that the target position has been reached
            _inPosition = true;

            _hitBoxScale = new Vector3(abilityData.GetCustomStatValue("HitBoxScaleX") * BlackBoardBehaviour.Instance.Grid.PanelScale.x, abilityData.GetCustomStatValue("HitBoxScaleY"), abilityData.GetCustomStatValue("HitBoxScaleZ") * BlackBoardBehaviour.Instance.Grid.PanelScale.z);
            //Instantiate particles and hit box
            _visualPrefabInstance = Object.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            HitColliderData hitColliderRef = GetColliderData(0);

            

            Transform spawnTransform = OwnerMoveScript.Alignment == GridScripts.GridAlignment.LEFT ? OwnerMoveset.RightMeleeSpawns[0] : OwnerMoveset.LeftMeleeSpawns[0];

            //Spawn a game object with the collider attached
            _hitCollider = HitColliderSpawner.SpawnBoxCollider(spawnTransform, _hitBoxScale, hitColliderRef, owner);
            _hitCollider.transform.position += owner.transform.forward * abilityData.GetCustomStatValue("HitBoxDistanceX");
            GridTrackerBehaviour tracker;
            if (!_hitCollider.GetComponent<GridTrackerBehaviour>())
            {
                tracker = _hitCollider.gameObject.AddComponent<GridTrackerBehaviour>();
                tracker.Marker = MarkerType.DANGER;
            }    



            _hitCollider.DebuggingEnabled = true;
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Spawn a hit box when the destination has been reached
            OwnerMoveScript.AddOnMoveEndTempAction(SpawnHitBox);
            _panelTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");

            //Makes the character move until it runs into an obstacle
            for (int i = (int)_panelTravelDistance; i >= 0; i--)
            {
                Vector2 moveOffset = new Vector2(i, 0);
                if (OwnerMoveScript.MoveToPanel(OwnerMoveScript.CurrentPanel.Position + moveOffset * Mathf.RoundToInt(owner.transform.forward.x), false, GridScripts.GridAlignment.ANY, false, false))
                    break;
            }
        }

        public override void Update()
        {
            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            _deactivated = true;

            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);

            ObjectPoolBehaviour.Instance.ReturnGameObject(_hitCollider?.gameObject);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            if (OwnerMoveScript.IsMoving)
                OwnerMoveScript.CancelMovement();


            OwnerMoveScript.StopAllCoroutines();
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}