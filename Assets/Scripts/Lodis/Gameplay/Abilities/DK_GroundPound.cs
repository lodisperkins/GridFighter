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
            _ownerGravity = _knockBackBehaviour.Gravity;
            _knockBackBehaviour.ApplyVelocityChange(Vector3.up * abilityData.GetCustomStatValue("JumpForce"));
            _knockBackBehaviour.Gravity = abilityData.GetCustomStatValue("JumpForce") / abilityData.startUpTime;
        }

        private IEnumerator MoveHitBox(GameObject visualPrefabInstance, Vector2 direction)
        {
            visualPrefabInstance.AddComponent<GridMovementBehaviour>();
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.GetComponent<GridMovementBehaviour>();
            movementBehaviour.Position = _ownerMoveScript.Position;

            int travelDistance = (int)abilityData.GetCustomStatValue("ShockWaveTravelDistance");

            for (int i = 0; i < travelDistance; i++)
            {
                movementBehaviour.MoveToPanel(movementBehaviour.Position + direction, true);
                yield return new WaitForSeconds(abilityData.GetCustomStatValue("ShockWaveProgressionDelay"));
            }
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _shockWaveCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner, false, false, true);

            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, null);
            _visualPrefabCoroutines.Item1 = _ownerMoveScript.StartCoroutine(MoveHitBox(_visualPrefabInstances.Item1, owner.transform.forward));

            _visualPrefabInstances.Item2 = MonoBehaviour.Instantiate(abilityData.visualPrefab, null);
            _visualPrefabCoroutines.Item2 = _ownerMoveScript.StartCoroutine(MoveHitBox(_visualPrefabInstances.Item2, -owner.transform.forward));
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            if (_visualPrefabCoroutines.Item1 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item1);

            if (_visualPrefabCoroutines.Item2 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item2);

            _knockBackBehaviour.Gravity = _ownerGravity;
        }
    }
}