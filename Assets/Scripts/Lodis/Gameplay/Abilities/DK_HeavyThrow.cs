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

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        private void PrepareThrow(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (!other.CompareTag("Player"))
                return;

            if (_opponentPhysics.GetComponent<KnockbackBehaviour>().IsInvincible)
                return;

            _opponentPhysics = other.GetComponent<GridPhysicsBehaviour>();

            _opponentPhysics.StopAllForces();

            _opponentCaptured = true;

            _ownerKnockBackScript.Physics.UseGravity = false;
            _opponentPhysics.IgnoreForces = true;

            float throwHeight = abilityData.GetCustomStatValue("ThrowHeight");
            Vector3 position = owner.transform.position + Vector3.up * throwHeight;

            _ownerMoveScript.CancelMovement();
            _ownerMoveScript.DisableMovement(condition => !InUse);
            _ownerMoveScript.TeleportToLocation(position);
            _ownerAnimationScript.PlayAbilityAnimation();

            _throwAction = RoutineBehaviour.Instance.StartNewTimedAction(info => ThrowOpponent(),TimedActionCountType.SCALEDTIME, 0.1f);
        }

        private void ThrowOpponent()
        {
            _ownerAnimationScript.PlayAbilityAnimation();

            EnableBounce();
            float throwDelay = abilityData.GetCustomStatValue("ThrowDelay");

            _throwAction = RoutineBehaviour.Instance.StartNewTimedAction(
                info =>
                {
                    //Calculates the angle and magnitude of the force to be applied.
                    float radians = abilityData.GetCustomStatValue("ThrowAngle");
                    float magnitude = abilityData.GetCustomStatValue("ThrowForce");
                    Vector3 force = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * magnitude;
                    _opponentPhysics.UseGravity = true;
                    _opponentCaptured = false;

                    _opponentPhysics.IgnoreForces = false;
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
            _opponentPhysics.EnablePanelBounce(false);

            //Starts a new delayed action to disable the panel bouncing after it has bounced once. 
            RoutineBehaviour.Instance.StartNewConditionAction(parameters => { _opponentPhysics.DisablePanelBounce();}, condition => _opponentPhysics.IsGrounded);
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
            _collider.AddCollisionEvent(PrepareThrow);
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

        public override void Update()
        {
            base.Update();

            if (_opponentCaptured)
                _opponentPhysics.transform.position = OwnerMoveset.LeftMeleeSpawns[1].position;
        }
    }
}