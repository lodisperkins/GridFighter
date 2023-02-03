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
        private TimedAction _throwAction;
        private Transform _oldOpponentParent;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            OnHit += ThrowOpponent;
        }

        private void ThrowOpponent(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (!other.CompareTag("Player"))
                return;

            _oldOpponentParent = other.transform.parent;

            other.transform.parent = OwnerMoveset.LeftMeleeSpawns[1];

            _opponentPhysics = other.GetComponent<GridPhysicsBehaviour>();

            _opponentPhysics.StopAllForces();
            float throwHeight = abilityData.GetCustomStatValue("ThrowHeight");
            Vector3 position = owner.transform.position + Vector3.up * throwHeight;

            _ownerMoveScript.TeleportToLocation(position);

            _ownerMoveScript.DisableMovement(condition => !InUse, false);

            _ownerKnockBackScript.Physics.UseGravity = false;

            EnableAnimation();
            PauseAbilityTimer();

            float throwDelay = abilityData.GetCustomStatValue("ThrowDelay");

            _throwAction = RoutineBehaviour.Instance.StartNewTimedAction(
                info =>
                {
                    //Calculates the angle and magnitude of the force to be applied.
                    float radians = abilityData.GetCustomStatValue("ThrowAngle");
                    float magnitude = abilityData.GetCustomStatValue("ThrowForce");
                    Vector3 force = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * magnitude;

                    _opponentPhysics.transform.parent = _oldOpponentParent;
                    _opponentPhysics.UseGravity = true;
                    _opponentPhysics.EnablePanelBounce();
                    _opponentPhysics.ApplyImpulseForce(force, true);
                    UnpauseAbilityTimer();
                }, TimedActionCountType.SCALEDTIME, throwDelay);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            DisableAnimation();
            
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            float direction = _ownerMoveScript.Alignment == GridScripts.GridAlignment.LEFT ? 1 : -1;
            Vector2 offset = Vector2.right * abilityData.GetCustomStatValue("TravelDistance") * direction;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offset, false, GridScripts.GridAlignment.ANY, true, false, true);

            _collider = HitColliderSpawner.SpawnBoxCollider(owner.transform, Vector3.one, GetColliderData(0), owner);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            _opponentPhysics.transform.parent = _oldOpponentParent;
            _ownerKnockBackScript.Physics.UseGravity = true;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_collider.gameObject);
        }

        public override void StopAbility()
        {
            base.StopAbility();

            RoutineBehaviour.Instance.StopAction(_throwAction);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_collider.gameObject);

            _opponentPhysics.UseGravity = true;
            _opponentPhysics.DisablePanelBounce();
            _opponentPhysics.transform.parent = _oldOpponentParent;

            _ownerKnockBackScript.Physics.UseGravity = true;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}