using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_FootDive : Ability
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private float _ownerGravity;
        private HitColliderBehaviour _shockWaveCollider;
        private (GameObject, GameObject) _visualPrefabInstances;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;
        private GridBehaviour _grid;
        private float _timeForceAdded;
        private bool _forceAdded;
        private float _riseTime;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _knockBackBehaviour = owner.GetComponent<KnockbackBehaviour>();
            _grid = BlackBoardBehaviour.Instance.Grid;
        }

        protected override void Start(params object[] args)
        {
            base.Start();

            _riseTime = abilityData.startUpTime - abilityData.GetCustomStatValue("HangTime");
            _knockBackBehaviour.ApplyVelocityChange(AddForce(true));
            _knockBackBehaviour.PanelBounceEnabled = false;
        }

        private Vector3 AddForce(bool yPositive)
        {
            //Find displacement
            Vector2 targetPanelPos = _ownerMoveScript.Position + ((Vector2)owner.transform.forward * abilityData.GetCustomStatValue("TravelDistance"));
            targetPanelPos.x = Mathf.Clamp(targetPanelPos.x, 0, _grid.Dimensions.x);
            targetPanelPos.y = Mathf.Clamp(targetPanelPos.y, 0, _grid.Dimensions.y);
            PanelBehaviour panel;
            _grid.GetPanel(targetPanelPos, out panel, true);
            float displacement = Vector3.Distance(_ownerMoveScript.CurrentPanel.transform.position, panel.transform.position) / 2;

            ////Get x velocity
            //Vector3 velocityX;
            //velocityX.x = displacement - (0.5f * abilityData.GetCustomStatValue("HorizontalAcceleration") * abilityData.startUpTime);

            ////Get y velocity
            //Vector3 velocityY;

            int yDirection = yPositive ? 1 : -1;

            //velocityY.y = yDirection * (abilityData.GetCustomStatValue("JumpHeight") - (0.5f * _knockBackBehaviour.Gravity * abilityData.startUpTime));

            //Find y velocity 
            _ownerGravity = _knockBackBehaviour.Gravity;
            Vector3 velocityY;
            velocityY.y = abilityData.GetCustomStatValue("JumpHeight") + (0.5f * _knockBackBehaviour.Gravity * _riseTime);

            //Find x velocity
            Vector3 velocityX;
            velocityX.x = displacement / abilityData.startUpTime;

            //Apply force
            return new Vector3(velocityX.x, velocityY.y * yDirection, 0);

            _timeForceAdded = Time.time;
            _forceAdded = true;
        }

        private void MoveHitBox(GameObject visualPrefabInstance, Vector2 direction)
        {
            visualPrefabInstance.AddComponent<GridMovementBehaviour>();
            GridMovementBehaviour movementBehaviour = visualPrefabInstance.GetComponent<GridMovementBehaviour>();
            movementBehaviour.Position = _ownerMoveScript.Position;
            movementBehaviour.canCancelMovement = true;
            movementBehaviour.MoveOnStart = false;
            movementBehaviour.Speed = 5;

            int travelDistance = (int)abilityData.GetCustomStatValue("ShockwaveTravelDistance");
            Vector2 offset = direction * travelDistance;
            Vector2 movePosition = _ownerMoveScript.Position + offset;

            movePosition.x = Mathf.Clamp(movePosition.x, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x);
            movePosition.x = Mathf.Round(movePosition.x);
            movePosition.y = Mathf.Clamp(movePosition.y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y);
            movePosition.y = Mathf.Round(movePosition.y);

            movementBehaviour.MoveToPanel(movePosition, false, GridScripts.GridAlignment.ANY, true);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _shockWaveCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("Knockback"),
                abilityData.GetCustomStatValue("HitAngle"), false, abilityData.timeActive, owner, false, false, true);

            _ownerMoveScript.DisableMovement(condition => !InUse, false, true);

            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, ownerMoveset.MeleeHitBoxSpawnTransform);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item1.transform, _visualPrefabInstances.Item1.transform.localScale, _shockWaveCollider, owner);
            hitScript.debuggingEnabled = true;

            Vector3 spikeVelocity = AddForce(false).normalized * abilityData.GetCustomStatValue("DownwardSpeed");
            _knockBackBehaviour.ApplyVelocityChange(spikeVelocity);
            _knockBackBehaviour.Gravity = _ownerGravity * abilityData.GetCustomStatValue("DownwardGravityMultiplier");
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            DestroyBehaviour.Destroy(_visualPrefabInstances.Item1);
            _knockBackBehaviour.Gravity = _ownerGravity;

            if (!_knockBackBehaviour.InHitStun)
                _knockBackBehaviour.StopVelocity();
        }

        protected override void End()
        {
            base.End();
            _knockBackBehaviour.PanelBounceEnabled = true;
        }

        public override void Update()
        {
            base.Update();

            if (_forceAdded && Time.time - _timeForceAdded >= _riseTime && CurrentAbilityPhase != AbilityPhase.ACTIVE)
            {
                _knockBackBehaviour.StopAllForces();
                _knockBackBehaviour.Gravity = 0;
            }
        }
    }
}