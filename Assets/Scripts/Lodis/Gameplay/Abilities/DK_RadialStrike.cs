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

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            //_panelTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");
        }

        private void SpawnHitBox()
        {
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform);
            Vector3 hitBoxDimensions = new Vector3(abilityData.GetCustomStatValue("HitBoxScaleX"), abilityData.GetCustomStatValue("HitBoxScaleY"), abilityData.GetCustomStatValue("HitBoxScaleZ"));
            HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstance.transform, hitBoxDimensions, abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner);

            _visualPrefabInstance.transform.position = owner.transform.position + (owner.transform.forward * abilityData.GetCustomStatValue("HitBoxDistance"));
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _ownerMoveScript.AddOnMoveEndAction(SpawnHitBox);

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
            if (CurrentAbilityPhase == AbilityPhase.ACTIVE)
                _visualPrefabInstance.transform.RotateAround(owner.transform.position, Vector3.up, abilityData.GetCustomStatValue("RotationSpeed") * Time.deltaTime);
        }
    }
}