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

        private void SpawnHitBox()
        {
            //Don't spawn the hitbox if the ability was told to deactivate
            if (_deactivated)
                return;

            //Play animation now that the character has reached the target panel
            EnableAnimation();

            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;

            //Mark that the target position has been reached
            _inPosition = true;

            _hitBoxScale = new Vector3(abilityData.GetCustomStatValue("HitBoxScaleX") * BlackBoardBehaviour.Instance.Grid.PanelScale.x, abilityData.GetCustomStatValue("HitBoxScaleY"), abilityData.GetCustomStatValue("HitBoxScaleZ") * BlackBoardBehaviour.Instance.Grid.PanelScale.z);
            //Instantiate particles and hit box
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform);
            HitColliderData hitColliderRef = GetColliderData(0);

           HitColliderBehaviour hitCollider = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstance.transform, _hitBoxScale, hitColliderRef, owner);

            hitCollider.DebuggingEnabled = true;

            //Set hitbox position
            _visualPrefabInstance.transform.position = owner.transform.position + (owner.transform.forward * (abilityData.GetCustomStatValue("HitBoxDistanceZ") + BlackBoardBehaviour.Instance.Grid.PanelSpacingZ) +
                (owner.transform.right * (abilityData.GetCustomStatValue("HitBoxDistanceX") + BlackBoardBehaviour.Instance.Grid.PanelSpacingX)));
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Spawn a hit box when the destination has been reached
            _ownerMoveScript.AddOnMoveEndTempAction(SpawnHitBox);
            _panelTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");

            //Makes the character move until it runs into an obstacle
            for (int i = (int)_panelTravelDistance; i >= 0; i--)
            {
                Vector2 moveOffset = new Vector2(i, 0);
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * Mathf.RoundToInt(owner.transform.forward.x), false, GridScripts.GridAlignment.ANY, false, false))
                    break;
            }
        }

        public override void Update()
        {
            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            //Rotate the hitbox around when the ability is active
            if (CurrentAbilityPhase == AbilityPhase.ACTIVE && _inPosition)
                _visualPrefabInstance.transform.RotateAround(owner.transform.position, Vector3.up, abilityData.GetCustomStatValue("RotationSpeed") * Time.deltaTime);
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            Object.Destroy(_visualPrefabInstance);
            _deactivated = true;
        }

        public override void EndAbility()
        {
            base.EndAbility();
            //Stop the user from dashing 
            if (_ownerMoveScript.IsMoving)
                _ownerMoveScript.CancelMovement();


            _ownerMoveScript.StopAllCoroutines();
        }

        protected override void End()
        {
            base.End();
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}