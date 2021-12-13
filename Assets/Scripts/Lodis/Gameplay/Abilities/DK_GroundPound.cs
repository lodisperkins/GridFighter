using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Strike the ground to send a 
    ///shockwave that travels up to
    ///5 panels away.Enemies caught
    ///in the shockwave will be launched upwards.
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
            //Store default gravity
            _ownerGravity = _knockBackBehaviour.Physics.Gravity;
            //Add force to character to make them jump
            _knockBackBehaviour.Physics.ApplyVelocityChange(Vector3.up * abilityData.GetCustomStatValue("JumpForce"));

            //Disable movement to prevent the ability being interrupted
            _ownerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER, false, true);

            //Calculate what gravity should be to get the character to fall down in the given start up time
            _knockBackBehaviour.Physics.Gravity = ((abilityData.GetCustomStatValue("JumpForce")) / 0.5f) / abilityData.startUpTime;

            //Disable bouncing to prevent bouncing on landing
            _knockBackBehaviour.Physics.PanelBounceEnabled = false;
        }


        /// <summary>
        /// Moves a hit box for the amount of travel distance given
        /// </summary>
        /// <param name="visualPrefabInstance"></param>
        /// <param name="direction"></param>
        private void MoveHitBox(GameObject visualPrefabInstance, Vector2 direction)
        {
            //Give the shockwave the ability to move
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.AddComponent<GridMovementBehaviour>();

            //Set default traits for shockwave
            movementBehaviour.Position = _ownerMoveScript.Position;
            movementBehaviour.canCancelMovement = true;
            movementBehaviour.MoveOnStart = false;
            movementBehaviour.Speed = 5;

            //Caluclate move position based on the travel distance and character facing
            int travelDistance = (int)abilityData.GetCustomStatValue("ShockwaveTravelDistance");
            Vector2 offset = direction * travelDistance;
            Vector2 movePosition = _ownerMoveScript.Position + offset;

            //Clamp the position to be within the grid dimensions
            movePosition.x = Mathf.Clamp(movePosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
            movePosition.x = Mathf.Round(movePosition.x);
            movePosition.y = Mathf.Clamp(movePosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);
            movePosition.y = Mathf.Round(movePosition.y);

            //Move shockwave
            movementBehaviour.MoveToPanel(movePosition, false, GridScripts.GridAlignment.ANY, true);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Create collider for shockwaves
            _shockWaveCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), false, abilityData.timeActive, owner, false, false, true);
            _shockWaveCollider.IgnoreColliders = abilityData.IgnoreColliders;

            //Instantiate the first shockwave and attach a hit box to it
            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item1.transform, _visualPrefabInstances.Item1.transform.localScale, _shockWaveCollider);

            //Move first shockwave
            MoveHitBox(_visualPrefabInstances.Item1, owner.transform.forward);
            hitScript.debuggingEnabled = true;

            //Instantiate the second shockwave and attack a hit box to it
            _visualPrefabInstances.Item2 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item2.transform, _visualPrefabInstances.Item2.transform.localScale, _shockWaveCollider);

            //Move second shockwave
            MoveHitBox(_visualPrefabInstances.Item2, -owner.transform.forward);

            hitScript.debuggingEnabled = true;
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            //Stop shockwaves from moving
            if (_visualPrefabCoroutines.Item1 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item1);
            if (_visualPrefabCoroutines.Item2 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item2);

            //Destroy shockwaves
            DestroyBehaviour.Destroy(_visualPrefabInstances.Item1);
            DestroyBehaviour.Destroy(_visualPrefabInstances.Item2);

            //Reset gravity
            _knockBackBehaviour.Physics.Gravity = _ownerGravity;
        }

        protected override void End()
        {
            base.End();
            _knockBackBehaviour.Physics.PanelBounceEnabled = true;
        }
    }
}