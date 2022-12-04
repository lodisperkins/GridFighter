using Lodis.Movement;
using Lodis.Utility;
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
        private HitColliderData _shockWaveCollider;
        private GameObject _visualPrefabInstance;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;
        private bool _wavesSpawned;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _knockBackBehaviour = owner.GetComponent<KnockbackBehaviour>();
        }

        protected override void Start(params object[] args)
        {
            base.Start();
            _knockBackBehaviour.Physics.Jump(2, 0, abilityData.startUpTime, false, true, GridScripts.GridAlignment.ANY, default, DG.Tweening.Ease.InSine);

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
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.GetComponent<GridMovementBehaviour>();
            movementBehaviour.CanBeWalkedThrough = true;

            //Set default traits for shockwave
            movementBehaviour.Position = _ownerMoveScript.Position;
            movementBehaviour.canCancelMovement = true;
            movementBehaviour.MoveOnStart = false;
            movementBehaviour.Speed = abilityData.GetCustomStatValue("ShockwaveTravelSpeed");

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

            if (!_wavesSpawned)
                movementBehaviour.AddOnMoveEndAction(() => ObjectPoolBehaviour.Instance.ReturnGameObject(movementBehaviour.gameObject));
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Create collider for shockwaves
            _shockWaveCollider = GetColliderData(0);

            //Instantiate the first shockwave and attach a hit box to it
            _visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);

            HitColliderBehaviour hitScript = _visualPrefabInstance.GetComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = _shockWaveCollider;
            hitScript.Owner = owner;
            hitScript.ColliderInfo.OwnerAlignement = _ownerMoveScript.Alignment;
            
            //Move first shockwave
            MoveHitBox(_visualPrefabInstance, owner.transform.forward);

            //Instantiate the second shockwave and attack a hit box to it
            _visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            _visualPrefabInstance.transform.forward = -owner.transform.forward;

            hitScript = _visualPrefabInstance.GetComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = _shockWaveCollider;
            hitScript.Owner = owner;
            hitScript.ColliderInfo.OwnerAlignement = _ownerMoveScript.Alignment;

            //Move second shockwave
            MoveHitBox(_visualPrefabInstance, -owner.transform.forward);

            hitScript.DebuggingEnabled = true;
            CameraBehaviour.ShakeBehaviour.ShakeRotation();
            _wavesSpawned = true;
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            //Stop shockwaves from moving
            if (_visualPrefabCoroutines.Item1 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item1);
            if (_visualPrefabCoroutines.Item2 != null)
                _ownerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item2);

            _knockBackBehaviour.Physics.RB.isKinematic = true;
        }
    }
}