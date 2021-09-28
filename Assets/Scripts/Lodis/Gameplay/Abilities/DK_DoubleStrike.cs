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
        private HitColliderBehaviour _fistCollider;
        private GameObject _visualPrefabInstance;
        private Vector2 _attackDirection;
        private bool _usedFirstStrike;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            //Get owner input
            _ownerInput = owner.GetComponent<Input.InputBehaviour>();
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);
            //Set initial attack direction so player can change directions immediately
            _attackDirection = _ownerInput.AttackDirection;
            //Play animation
            EnableAnimation();
            //Store initial move speed
            _ownerMoveSpeed = _ownerMoveScript.Speed;
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Create collider for attack
            _fistCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner, false, false, true);

            //Spawn particles
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, ownerMoveset.MeleeHitBoxSpawnTransform);

            //Spawn a game object with the collider attached
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(owner.transform, new Vector3(1, 0.5f, 0.2f), _fistCollider);
            hitScript.debuggingEnabled = true;

            //Set the direction of the attack
            Vector2 attackPosition;
            if (_attackDirection == Vector2.zero)
                _attackDirection = (Vector2)(owner.transform.forward);

            //Get the panel position based on the direction of attack and distance given
            attackPosition = _ownerMoveScript.Position + (_attackDirection * abilityData.GetCustomStatValue("TravelDistance"));

            //Clamp to be sure the player doesn't go off grid
            attackPosition.x = Mathf.Clamp(attackPosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
            attackPosition.y = Mathf.Clamp(attackPosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);

            //Equation to calculate speed of attack given the active time
            float distance = abilityData.GetCustomStatValue("TravelDistance") + (abilityData.GetCustomStatValue("TravelDistance") * BlackBoardBehaviour.Instance.Grid.PanelSpacing);
            _ownerMoveScript.Speed = (distance * 2/ abilityData.timeActive) * BlackBoardBehaviour.Instance.Grid.PanelSpacing;

            //Change move traits to allow for free movement on the other side of the grid
            _ownerMoveScript.canCancelMovement = true;
            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            _ownerMoveScript.AlwaysLookAtOpposingSide = false;

            //Change rotation to the direction of movement
            owner.transform.forward = new Vector3(_attackDirection.x, 0, _attackDirection.y);

            //Move towards panel
            _ownerMoveScript.MoveToPanel(attackPosition, false, GridScripts.GridAlignment.ANY, true);
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            //Reset the movement traits
            _ownerMoveScript.Speed = _ownerMoveSpeed;
            _ownerMoveScript.canCancelMovement = false;

            //Despawn particles and hit box
            MonoBehaviour.Destroy(_visualPrefabInstance);
        }

        protected override void End()
        {
            base.End();
            //Reset the movement traits
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;
            _ownerMoveScript.AlwaysLookAtOpposingSide = true;
        }

        public override void Update()
        {
            base.Update();
            //If the player is trying to change their attack direction...
            if (_ownerInput.AttackDirection.magnitude > 0 && !_usedFirstStrike && CurrentAbilityPhase == AbilityPhase.RECOVER)
            {
                //...restart the ability
                Deactivate();
                ownerMoveset.StopAbilityRoutine();
                _attackDirection = _ownerInput.AttackDirection;
                UseAbility();
                _usedFirstStrike = true;
            }
        }
    }
}