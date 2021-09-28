using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_GroundPound : Ability
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private float _ownerGravity;
        private HitColliderBehaviour _shockWaveCollider;
        private (GameObject, GameObject) _visualPrefabInstances;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _knockBackBehaviour = owner.GetComponent<KnockbackBehaviour>();
        }

        protected override void Start(params object[] args)
        {
            base.Start();
            _ownerGravity = _knockBackBehaviour.Gravity;
            _knockBackBehaviour.ApplyVelocityChange(Vector3.up * abilityData.GetCustomStatValue("JumpForce"));
            _knockBackBehaviour.Gravity = ((abilityData.GetCustomStatValue("JumpForce")) / 0.5f) / abilityData.startUpTime;
            _knockBackBehaviour.PanelBounceEnabled = false;
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
            _shockWaveCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), false, abilityData.timeActive, owner, false, false, true);

            _ownerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER, false, true);

            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item1.transform, _visualPrefabInstances.Item1.transform.localScale, _shockWaveCollider);
            MoveHitBox(_visualPrefabInstances.Item1, owner.transform.forward);
            hitScript.debuggingEnabled = true;

            _visualPrefabInstances.Item2 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item2.transform, _visualPrefabInstances.Item2.transform.localScale, _shockWaveCollider);
            MoveHitBox(_visualPrefabInstances.Item2, -owner.transform.forward);

            hitScript.debuggingEnabled = true;
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

        protected override void End()
        {
            base.End();
            _knockBackBehaviour.PanelBounceEnabled = true;
        }
    }
}