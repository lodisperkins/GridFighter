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
            PlayAnimation();
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _fistCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), false, abilityData.timeActive, owner, false, false, true);

            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, ownerMoveset.MeleeHitBoxSpawnTransform);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstance.transform, _visualPrefabInstance.transform.localScale, _fistCollider, owner);
            hitScript.debuggingEnabled = true;

            Vector2 attackPosition;
            if (_ownerInput.AttackDirection == Vector2.zero)
                attackPosition = _ownerMoveScript.Position + (Vector2)(owner.transform.forward * abilityData.GetCustomStatValue("TravelDistance"));
            else
                attackPosition = _ownerMoveScript.Position + (_ownerInput.AttackDirection * abilityData.GetCustomStatValue("TravelDistance"));

            _ownerMoveScript.canCancelMovement = true;
            _ownerMoveSpeed = _ownerMoveScript.Speed;
            _ownerMoveScript.Speed = (abilityData.GetCustomStatValue("TravelDistance") * 2) / abilityData.timeActive;

            _ownerMoveScript.MoveToAlignedSideWhenStuck = false;
            _ownerMoveScript.AlwaysLookAtOpposingSide = false;
            _ownerMoveScript.MoveToPanel(attackPosition, false, GridScripts.GridAlignment.ANY);
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
            if (_ownerInput.AttackDirection.magnitude > 0)
            {
                Deactivate();
                UseAbility();
            }
        }
    }
}