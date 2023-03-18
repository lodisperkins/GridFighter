using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_HeavyThrow : Ability
    {
        private HitColliderBehaviour _collider;
        private GridPhysicsBehaviour _opponentPhysics;
        private DelayedAction _throwAction;
        private Transform _oldOpponentParent;
        private bool _opponentCaptured;
        private GameObject _whirlEffect;
        private GameObject _releaseEffect;
        private Quaternion _oldRotation;
        private float _oldZ;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _whirlEffect = abilityData.visualPrefab;
            _releaseEffect = Resources.Load<GameObject>("Effects/ThrowReleaseBurst");
            _oldOpponentParent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform.parent;
            _oldRotation = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform.rotation;
        }


        private void PrepareThrow(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (!other.CompareTag("Player"))
                return;


            _opponentPhysics = other.GetComponent<GridPhysicsBehaviour>();
            if (_opponentPhysics.GetComponent<KnockbackBehaviour>().IsInvincible)
                return;

            _opponentPhysics.MovementBehaviour.CancelMovement();
            _opponentPhysics.StopAllForces();

            _opponentCaptured = true;
            
            _ownerKnockBackScript.Physics.UseGravity = false;
            _opponentPhysics.IgnoreForces = true;
            _oldZ = _opponentPhysics.transform.position.z;

            float throwHeight = abilityData.GetCustomStatValue("ThrowHeight");
            Vector3 position = owner.transform.position + Vector3.up * throwHeight;

            _ownerMoveScript.CancelMovement();
            _ownerMoveScript.DisableMovement(condition => !InUse);
            _ownerMoveScript.TeleportToLocation(position);

            _throwAction = RoutineBehaviour.Instance.StartNewTimedAction(info => ThrowOpponent(),TimedActionCountType.SCALEDTIME, 0.1f);
        }

        private void ThrowOpponent()
        {

            _opponentPhysics.transform.parent = OwnerMoveset.LeftMeleeSpawns[1];
            _opponentPhysics.transform.localPosition = Vector3.left / 2;
            _opponentPhysics.transform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);

            //Play wind up animation.
            AnimationClip clip;
            if (!abilityData.GetAdditionalAnimation(0, out clip))
                Debug.LogError("Additional throw animations missing from " + abilityData.abilityName + "ability.");

            float throwDelay = abilityData.GetCustomStatValue("ThrowDelay");
            _ownerAnimationScript.PlayAnimation(clip);

            GameObject whirlInstance = MonoBehaviour.Instantiate(_whirlEffect, owner.transform.position, owner.transform.rotation);
            MonoBehaviour.Destroy(whirlInstance, throwDelay);

            EnableBounce();

            _throwAction = RoutineBehaviour.Instance.StartNewTimedAction(
                info =>
                {
                    //Play release animation and effects.
                    if (!abilityData.GetAdditionalAnimation(1, out clip))
                        Debug.LogError("Throw release animation missing from " + abilityData.abilityName + " ability.");

                    _ownerAnimationScript.PlayAnimation(abilityData.recoverTime + throwDelay, clip);

                    MonoBehaviour.Instantiate(_releaseEffect, owner.transform.position, owner.transform.rotation);

                    //Calculates the angle and magnitude of the force to be applied.
                    float radians = abilityData.GetCustomStatValue("ThrowAngle");
                    float magnitude = abilityData.GetCustomStatValue("ThrowForce");
                    Vector3 force = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * magnitude;
                    force.x *= owner.transform.forward.x;

                    //Reset physics attributes.
                    _opponentPhysics.UseGravity = true;
                    _opponentCaptured = false;
                    _opponentPhysics.IgnoreForces = false;

                    //Set proper position and orientation after throw.
                    _opponentPhysics.transform.rotation = _oldRotation;
                    _opponentPhysics.transform.parent = _oldOpponentParent;
                    _opponentPhysics.transform.position = new Vector3(_opponentPhysics.transform.position.x, _opponentPhysics.transform.position.y, _oldZ);

                    _opponentPhysics.ApplyVelocityChange(force);

                    UnpauseAbilityTimer();
                }, TimedActionCountType.SCALEDTIME, throwDelay);
        }

        /// <summary>
        /// Makes the opponent bouncy after colliding with the ground.
        /// </summary>
        private void EnableBounce(params object[] args)
        {
            if (_opponentPhysics?.PanelBounceEnabled == true)
                return;

            //Enable the panel bounce and set the temporary bounce value using the custom bounce stat.
            _opponentPhysics.EnablePanelBounce(true);
            string opponentState = BlackBoardBehaviour.Instance.GetPlayerState(_opponentPhysics.gameObject);
            //Starts a new delayed action to disable the panel bouncing after it has bounced once. 
            RoutineBehaviour.Instance.StartNewConditionAction(parameters => { _opponentPhysics.DisablePanelBounce();}, condition => _opponentPhysics.IsGrounded || opponentState != "Tumbling");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            AnimationClip clip;
            if (!abilityData.GetCustomAnimation(out clip))
                Debug.LogError("Heavy throw ability missing starting custom animation.");

            _ownerAnimationScript.PlayAnimation(abilityData.startUpTime, clip);
            OnHit += PrepareThrow;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _opponentCaptured = false;
            float direction = _ownerMoveScript.Alignment == GridScripts.GridAlignment.LEFT ? 1 : -1;
            Vector2 offset = Vector2.right * abilityData.GetCustomStatValue("TravelDistance") * direction;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offset, false, GridScripts.GridAlignment.ANY, true, false, true);

            _collider = HitColliderSpawner.SpawnBoxCollider(owner.transform, Vector3.one, GetColliderData(0), owner);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            _ownerKnockBackScript.Physics.UseGravity = true;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;

            if (_collider)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_collider.gameObject);
        }

        public override void StopAbility()
        {
            base.StopAbility();

            RoutineBehaviour.Instance.StopAction(_throwAction);


            _ownerKnockBackScript.Physics.UseGravity = true;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;

            if (_collider)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_collider.gameObject);

            if (!_opponentPhysics)
                return;

            _opponentPhysics.UseGravity = true;
            _opponentPhysics.DisablePanelBounce();
            _opponentPhysics.transform.parent = _oldOpponentParent;
        }
    }
}