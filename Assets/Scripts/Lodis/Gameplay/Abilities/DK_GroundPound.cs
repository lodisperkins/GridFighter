﻿using Lodis.Movement;
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
            _knockBackBehaviour.Physics.Jump(0, 2, abilityData.startUpTime, false, true, GridScripts.GridAlignment.ANY, default, DG.Tweening.Ease.InSine);

            //Disable movement to prevent the ability being interrupted
            _ownerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
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
            movementBehaviour.CanBeWalkedThrough = true;

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
            movementBehaviour.AddOnMoveEndAction(() => Object.Destroy(movementBehaviour.gameObject));
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Create collider for shockwaves
            _shockWaveCollider = GetColliderBehaviourCopy(0);

            //Instantiate the first shockwave and attach a hit box to it
            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item1.transform, _visualPrefabInstances.Item1.transform.localScale, _shockWaveCollider);
            hitScript.ColliderInfo.OwnerAlignement = _ownerMoveScript.Alignment;
            
            //Move first shockwave
            MoveHitBox(_visualPrefabInstances.Item1, owner.transform.forward);
            hitScript.DebuggingEnabled = true;

            //Instantiate the second shockwave and attack a hit box to it
            _visualPrefabInstances.Item2 = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            _visualPrefabInstances.Item2.transform.forward = -owner.transform.forward;

            hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item2.transform, _visualPrefabInstances.Item2.transform.localScale, _shockWaveCollider);

            //Move second shockwave
            MoveHitBox(_visualPrefabInstances.Item2, -owner.transform.forward);

            hitScript.DebuggingEnabled = true;
            CameraBehaviour.ShakeBehaviour.ShakeRotation();
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
            Object.Destroy(_visualPrefabInstances.Item1);
            Object.Destroy(_visualPrefabInstances.Item2);
            _knockBackBehaviour.Physics.RB.isKinematic = true;
        }
    }
}