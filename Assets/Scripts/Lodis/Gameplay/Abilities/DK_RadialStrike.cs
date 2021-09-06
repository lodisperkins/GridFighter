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

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        private void SpawnHitBox()
        {
            PlayAnimation();
            _ownerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER, false, true);
            _inPosition = true;
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform);
            Vector3 hitBoxDimensions = new Vector3(abilityData.GetCustomStatValue("HitBoxScaleX"), abilityData.GetCustomStatValue("HitBoxScaleY"), abilityData.GetCustomStatValue("HitBoxScaleZ"));
           HitColliderBehaviour hitCollider = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstance.transform, hitBoxDimensions, abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner);

            hitCollider.debuggingEnabled = true;

            _visualPrefabInstance.transform.position = owner.transform.position + (owner.transform.forward * abilityData.GetCustomStatValue("HitBoxDistanceZ") +
                (owner.transform.right * abilityData.GetCustomStatValue("HitBoxDistanceX")));
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _ownerMoveScript.AddOnMoveEndTempAction(SpawnHitBox);
            _panelTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");

            //Makes the link move until it runs into an obstacle
            for (int i = (int)_panelTravelDistance; i >= 0; i--)
            {
                Vector2 moveOffset = new Vector2(i, 0);
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * owner.transform.forward.x, false, GridScripts.GridAlignment.ANY))
                    break;
            }
        }

        public override void Update()
        {
            if (CurrentAbilityPhase == AbilityPhase.ACTIVE && _inPosition)
                _visualPrefabInstance.transform.RotateAround(owner.transform.position, Vector3.up, abilityData.GetCustomStatValue("RotationSpeed") * Time.deltaTime);
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            MonoBehaviour.Destroy(_visualPrefabInstance);
        }
    }
}