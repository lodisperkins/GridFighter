using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_FootDive : Ability
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private float _ownerGravity;
        private HitColliderBehaviour _shockWaveCollider;
        private (GameObject, GameObject) _visualPrefabInstances;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;
        private GridBehaviour _grid;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _knockBackBehaviour = owner.GetComponent<KnockbackBehaviour>();
        }

        protected override void Start(params object[] args)
        {
            base.Start();
            _grid = BlackBoardBehaviour.Instance.Grid;
            //VoX = x - 1/2aXT;

            AddForce(true);
        }

        private void AddForce(bool yPositive)
        {
            //Find displacement
            Vector2 targetPanelPos = _ownerMoveScript.Position + ((Vector2)owner.transform.forward * abilityData.GetCustomStatValue("TravelDistance"));
            targetPanelPos.x = Mathf.Clamp(targetPanelPos.x, 0, _grid.Dimensions.x);
            targetPanelPos.y = Mathf.Clamp(targetPanelPos.y, 0, _grid.Dimensions.y);
            PanelBehaviour panel;
            _grid.GetPanel(targetPanelPos, out panel, true);
            float displacement = Vector3.Distance(_ownerMoveScript.CurrentPanel.transform.position, panel.transform.position) / 2;

            //Get x velocity
            Vector3 velocityX;
            velocityX.x = displacement - (0.5f * abilityData.GetCustomStatValue("HorizontalAcceleration") * abilityData.startUpTime);

            //Get y velocity
            Vector3 velocityY;

            int yDirection =  yPositive?  1 :  -1;

            velocityY.y = yDirection * (abilityData.GetCustomStatValue("JumpHeight") - (0.5f * _knockBackBehaviour.Gravity * abilityData.startUpTime));

            //Apply force
            _knockBackBehaviour.ApplyImpulseForce(new Vector3(velocityX.x, velocityY.y, 0));
        }

        private void MoveHitBox(GameObject visualPrefabInstance, Vector2 direction)
        {
            visualPrefabInstance.AddComponent<GridMovementBehaviour>();
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.GetComponent<GridMovementBehaviour>();
            movementBehaviour.Position = _ownerMoveScript.Position;
            movementBehaviour.canCancelMovement = true;
            movementBehaviour.MoveOnStart = false;
            movementBehaviour.Speed = 5;

            int travelDistance = (int)abilityData.GetCustomStatValue("ShockwaveTravelDistance");
            Vector2 offset = direction * travelDistance;
            Vector2 movePosition = _ownerMoveScript.Position + offset;

            movePosition.x = Mathf.Clamp(movePosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x);
            movePosition.x = Mathf.Round(movePosition.x);
            movePosition.y = Mathf.Clamp(movePosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y);
            movePosition.y = Mathf.Round(movePosition.y);

            movementBehaviour.MoveToPanel(movePosition, false, GridScripts.GridAlignment.ANY, true);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            AddForce(false);
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            if (_visualPrefabCoroutines.Item1 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item1);

            if (_visualPrefabCoroutines.Item2 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item2);

            DestroyBehaviour.Destroy(_visualPrefabInstances.Item1);
            DestroyBehaviour.Destroy(_visualPrefabInstances.Item2);
            _knockBackBehaviour.Gravity = _ownerGravity;

        }
    }
}