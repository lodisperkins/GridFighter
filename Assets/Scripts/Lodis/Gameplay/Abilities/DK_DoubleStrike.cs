using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
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
            _ownerInput = owner.GetComponent<Input.InputBehaviour>();
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);
            _attackDirection = _ownerInput.AttackDirection;
            EnableAnimation();
            _ownerMoveSpeed = _ownerMoveScript.Speed;
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _fistCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner, false, false, true);

            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, ownerMoveset.MeleeHitBoxSpawnTransform);

            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(owner.transform, new Vector3(1, 0.5f, 0.2f), _fistCollider);
            hitScript.debuggingEnabled = true;

            Vector2 attackPosition;
            if (_attackDirection == Vector2.zero)
                _attackDirection = (Vector2)(owner.transform.forward);
            
            attackPosition = _ownerMoveScript.Position + (_attackDirection * abilityData.GetCustomStatValue("TravelDistance"));

            attackPosition.x = Mathf.Clamp(attackPosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
            attackPosition.y = Mathf.Clamp(attackPosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);

            _ownerMoveScript.canCancelMovement = true;
            float distance = abilityData.GetCustomStatValue("TravelDistance") + (abilityData.GetCustomStatValue("TravelDistance") * BlackBoardBehaviour.Instance.Grid.PanelSpacing);
            _ownerMoveScript.Speed = (distance * 2/ abilityData.timeActive) * BlackBoardBehaviour.Instance.Grid.PanelSpacing;

            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            _ownerMoveScript.AlwaysLookAtOpposingSide = false;
            owner.transform.forward = new Vector3(_attackDirection.x, 0, _attackDirection.y);
            _ownerMoveScript.MoveToPanel(attackPosition, false, GridScripts.GridAlignment.ANY, true);

            
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            _ownerMoveScript.Speed = _ownerMoveSpeed;
            _ownerMoveScript.canCancelMovement = false;
            MonoBehaviour.Destroy(_visualPrefabInstance);
        }

        protected override void End()
        {
            base.End();
            _ownerMoveScript.MoveToAlignedSideWhenStuck = true;
            _ownerMoveScript.AlwaysLookAtOpposingSide = true;
        }

        public override void Update()
        {
            base.Update();
            if (_ownerInput.AttackDirection.magnitude > 0 && !_usedFirstStrike && CurrentAbilityPhase == AbilityPhase.RECOVER)
            {
                Deactivate();
                ownerMoveset.StopAbilityRoutine();
                _attackDirection = _ownerInput.AttackDirection;
                UseAbility();
                _usedFirstStrike = true;
            }
        }
    }
}