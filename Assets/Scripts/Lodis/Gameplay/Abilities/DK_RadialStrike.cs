using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_RadialStrike : Ability
    {
        private float _panelTravelDistance;
        private HitColliderBehaviour _hitCollider;
        private GameObject _visualPrefabInstance;
        private bool _inPosition = false;
        private bool _deactivated = false;

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
            //Disable character movement so the ability isn't interrupted
            _ownerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER, false, true);
            //Mark that the target position has been reached
            _inPosition = true;
            //Instantiate particles and hit box
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform);
            Vector3 hitBoxDimensions = new Vector3(abilityData.GetCustomStatValue("HitBoxScaleX"), abilityData.GetCustomStatValue("HitBoxScaleY"), abilityData.GetCustomStatValue("HitBoxScaleZ"));
           HitColliderBehaviour hitCollider = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstance.transform, hitBoxDimensions, abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner);

            hitCollider.debuggingEnabled = true;

            //Set hitbox position
            _visualPrefabInstance.transform.position = owner.transform.position + (owner.transform.forward * abilityData.GetCustomStatValue("HitBoxDistanceZ") +
                (owner.transform.right * abilityData.GetCustomStatValue("HitBoxDistanceX")));
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
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * owner.transform.forward.x, false, GridScripts.GridAlignment.ANY))
                    break;
            }
        }

        public override void Update()
        {
            //Rotate the hitbox around when the ability is active
            if (CurrentAbilityPhase == AbilityPhase.ACTIVE && _inPosition)
                _visualPrefabInstance.transform.RotateAround(owner.transform.position, Vector3.up, abilityData.GetCustomStatValue("RotationSpeed") * Time.deltaTime);
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            MonoBehaviour.Destroy(_visualPrefabInstance);
            _deactivated = true;
        }

        public override void EndAbility()
        {
            base.EndAbility();
            if (_ownerMoveScript.IsMoving)
                _ownerMoveScript.CancelMovement();
            _ownerMoveScript.StopAllCoroutines();
        }
    }
}