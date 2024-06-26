﻿using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Dashes forward to deliver a 
    ///precise blow, move in a different direction
    ///to deliver a second strike.
    /// </summary>
    public class DK_DoubleStrike : Ability
    {
        private Input.InputBehaviour _ownerInput;
        private float _ownerMoveSpeed;
        private HitColliderData _fistCollider;
        private GameObject _visualPrefabInstance;
        private HitColliderBehaviour _hitScript;
        private Vector2 _attackDirection;
        private bool _secondStrikeActivated;
        private Quaternion _rotation;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            //Get owner input
            _ownerInput = owner.GetComponentInParent<Input.InputBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            if (_ownerInput)
            {
                //Set initial attack direction so player can change directions immediately
                _attackDirection = _ownerInput.AttackDirection;
            }
            else
                _attackDirection = owner.transform.forward;

            //Play animation
            ChangeMoveAttributes();
            _rotation = owner.transform.rotation;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            OwnerAnimationScript.PlayAbilityAnimation();
            //Create collider for attack
            _fistCollider = GetColliderData(0);

            //Spawn particles
           _visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform, true);
            _visualPrefabInstance.transform.forward = owner.transform.forward;
            //Spawn a game object with the collider attached
            _hitScript = _visualPrefabInstance.GetComponent<HitColliderBehaviour>();
            _hitScript.ColliderInfo = _fistCollider;
            _hitScript.Owner = owner;
            _hitScript.DebuggingEnabled = true;

            //Set the direction of the attack
            Vector2 attackPosition;
            if (_attackDirection == Vector2.zero)
                _attackDirection = (Vector2)(owner.transform.forward);

            //Get the panel position based on the direction of attack and distance given
            attackPosition = OwnerMoveScript.Position + (_attackDirection * abilityData.GetCustomStatValue("TravelDistance"));

            //Clamp to be sure the player doesn't go off grid
            attackPosition.x = Mathf.Clamp(attackPosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
            attackPosition.y = Mathf.Clamp(attackPosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);

            //Equation to calculate speed of attack given the active time
            float distance = abilityData.GetCustomStatValue("TravelDistance") + (abilityData.GetCustomStatValue("TravelDistance") * BlackBoardBehaviour.Instance.Grid.PanelSpacingX);
            OwnerMoveScript.Speed = (distance * 2/ abilityData.timeActive) * BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //Change move traits to allow for free movement on the other side of the grid
            OwnerMoveScript.CanCancelMovement = true;

            //Change rotation to the direction of movement
            owner.transform.forward = new Vector3(_attackDirection.x, 0, _attackDirection.y);

            //Move towards panel
            OwnerMoveScript.MoveToPanel(attackPosition, false, GridScripts.GridAlignment.ANY, true, false);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_visualPrefabInstance, abilityData.timeActive + abilityData.timeActive / 3);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            //Despawn particles and hit box
            ResetMoveAttributes();
            owner.transform.rotation = _rotation;


            if (!_secondStrikeActivated)
            {
                EndAbility();
                onEnd?.Invoke();
                OnEnd();
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            ResetMoveAttributes();
        }

        /// <summary>
        /// Allows the player to stay on the opponents side and rotate in different directions
        /// </summary>
        private void ChangeMoveAttributes()
        {
            //Store initial move speed
            _ownerMoveSpeed = OwnerMoveScript.Speed;
            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
            OwnerMoveScript.AlwaysLookAtOpposingSide = false;
        }

        /// <summary>
        /// Resets all movement variables to their defualt values.
        /// </summary>
        private void ResetMoveAttributes()
        {
            //Reset the movement traits
            if (OwnerMoveScript.IsMoving)
                OwnerMoveScript.CancelMovement();

            OwnerMoveScript.CanCancelMovement = false;
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
            OwnerMoveScript.AlwaysLookAtOpposingSide = true;

        }

        public override void Update()
        {
            base.Update();

            if (!_ownerInput)
                return;

            //If the player is trying to change their attack direction...
            if (_ownerInput.AttackDirection.magnitude > 0 && CurrentAbilityPhase == AbilityPhase.RECOVER && !MaxActivationAmountReached)
            {
                //...restart the ability
                _attackDirection = _ownerInput.AttackDirection;

                _secondStrikeActivated = true;
                OnRecover(null);
                int activationAmount = currentActivationAmount;
                EndAbility();

                //Disable movement feeatures to allow free movement on opponent side
                ChangeMoveAttributes();
                currentActivationAmount = activationAmount;
                string[] slots = OwnerMoveset.GetAbilityNamesInCurrentSlots();

                //Check slots to be sure only doublestrike is activated again
                if (slots[0] == abilityData.abilityName)
                    OwnerMoveset.UseSpecialAbility(0, _ownerInput.AttackDirection);
                else
                    OwnerMoveset.UseSpecialAbility(1, _ownerInput.AttackDirection);
            }
        }
    }
}