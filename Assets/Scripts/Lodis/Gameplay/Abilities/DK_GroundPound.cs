
using FixedPoints;
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
        private GameObject _explosionEffect;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(Owner);
            _explosionEffect = (GameObject)Resources.Load("Effects/MediumExplosion");
            _knockBackBehaviour = Owner.GetComponent<KnockbackBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart();
            _knockBackBehaviour.Physics.Jump(2, 0, abilityData.startUpTime);

            //Disable movement to prevent the ability being interrupted
            OwnerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
        }


        /// <summary>
        /// Moves a hit box for the amount of travel distance given
        /// </summary>
        /// <param name="visualPrefabInstance"></param>
        /// <param name="direction"></param>
        private void MoveHitBox(GameObject visualPrefabInstance, FVector2 direction)
        {
            //Give the shockwave the ability to move
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.GetComponent<GridMovementBehaviour>();
            movementBehaviour.CanBeWalkedThrough = true;

            //Set default traits for shockwave
            movementBehaviour.Position = OwnerMoveScript.Position;
            movementBehaviour.CanCancelMovement = true;
            movementBehaviour.MoveOnStart = false;
            movementBehaviour.Speed = abilityData.GetCustomStatValue("ShockwaveTravelSpeed");

            //Caluclate move position based on the travel distance and character facing
            int travelDistance = (int)abilityData.GetCustomStatValue("ShockwaveTravelDistance");
            FVector2 offset = direction * travelDistance;
            FVector2 movePosition = OwnerMoveScript.Position + offset;

            //Clamp the position to be within the grid dimensions
            movePosition.X = Mathf.Clamp(movePosition.X, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
            movePosition.X = Mathf.Round(movePosition.X);
            movePosition.X = Mathf.Clamp(movePosition.X, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);
            movePosition.Y = Mathf.Round(movePosition.Y);
            //Move shockwave
            movementBehaviour.MoveToPanel(movePosition, false, GridScripts.GridAlignment.ANY, true);

            if (!_wavesSpawned)
                movementBehaviour.AddOnMoveEndAction(() => ObjectPoolBehaviour.Instance.ReturnGameObject(movementBehaviour.gameObject));
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Create collider for shockwaves
            _shockWaveCollider = GetColliderData(0);

            //Instantiate the first shockwave and attach a hit box to it
            _visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, Owner.transform.position, Owner.transform.rotation);


            HitColliderBehaviour hitScript = _visualPrefabInstance.GetComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = _shockWaveCollider;
            hitScript.Owner = Owner;
            hitScript.ColliderInfo.OwnerAlignement = OwnerMoveScript.Alignment;

            Object.Instantiate(_explosionEffect, OwnerMoveScript.CurrentPanel.transform.position, Camera.main.transform.rotation);

            //Move first shockwave
            MoveHitBox(_visualPrefabInstance, new FVector2(Owner.transform.forward.x, Owner.transform.forward.y));

            //Instantiate the second shockwave and attack a hit box to it
            _visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, Owner.transform.position, Owner.transform.rotation);
            _visualPrefabInstance.transform.forward = -Owner.transform.forward;

            hitScript = _visualPrefabInstance.GetComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = _shockWaveCollider;
            hitScript.Owner = Owner;
            hitScript.ColliderInfo.OwnerAlignement = OwnerMoveScript.Alignment;

            //Move second shockwave
            MoveHitBox(_visualPrefabInstance, new FVector2(-Owner.transform.forward.x, -Owner.transform.forward.y));

            hitScript.DebuggingEnabled = true;
            CameraBehaviour.ShakeBehaviour.ShakeRotation();
            _wavesSpawned = true;
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            //Stop shockwaves from moving
            if (_visualPrefabCoroutines.Item1 != null)
                OwnerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item1);
            if (_visualPrefabCoroutines.Item2 != null)
                OwnerMoveScript.StopCoroutine(_visualPrefabCoroutines.Item2);

            //_knockBackBehaviour.Physics.RB.isKinematic = true;
        }
    }
}