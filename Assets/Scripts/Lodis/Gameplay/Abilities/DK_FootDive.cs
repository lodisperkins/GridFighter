using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Leap into the air and deliver 
    ///a powerful descending strike that
    ///can spike opponents to the ground.
    ///If the attack lands, spiked
    ///opponents crack the panel they land on.
    /// </summary>
    public class DK_FootDive : Ability
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private float _ownerGravity;
        private HitColliderBehaviour _fistCollider;
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

            //Calculate the time it takes to reache the peak height
            _riseTime = abilityData.startUpTime - abilityData.GetCustomStatValue("HangTime");
            //Add the velocity to the character to make them jump
            _knockBackBehaviour.Physics.ApplyVelocityChange(AddForce(true), false);
            //Disable bouncing so the character doesn't bounce when landing
            _knockBackBehaviour.Physics.PanelBounceEnabled = false;

            //Disable character movement so the jump isn't interrupted
            _ownerMoveScript.DisableMovement(condition => !InUse, false, true);
        }

        private Vector3 AddForce(bool yPositive)
        {
            //Find the position of the target panel
            Vector2 targetPanelPos = _ownerMoveScript.Position + ((Vector2)owner.transform.forward * abilityData.GetCustomStatValue("TravelDistance"));
            targetPanelPos.x = Mathf.Clamp(targetPanelPos.x, 0, _grid.Dimensions.x - 1);
            targetPanelPos.y = Mathf.Clamp(targetPanelPos.y, 0, _grid.Dimensions.y - 1);

            //Get a reference to the panel at the position found
            PanelBehaviour panel;
            _grid.GetPanel(targetPanelPos, out panel, true);

            //Find the displacement
            float displacement = Vector3.Distance(_ownerMoveScript.CurrentPanel.transform.position, panel.transform.position) / 2;

            //Get float representing y direction based on argument given
            int yDirection = yPositive ? 1 : -1;

            //Find y velocity 
            _ownerGravity = _knockBackBehaviour.Physics.Gravity;
            Vector3 velocityY;
            velocityY.y = abilityData.GetCustomStatValue("JumpHeight") + (0.5f * _knockBackBehaviour.Physics.Gravity * _riseTime);

            //Find x velocity
            Vector3 velocityX;
            velocityX.x = (displacement / abilityData.startUpTime) * owner.transform.forward.x;

            //Apply force
            return new Vector3(velocityX.x, velocityY.y * yDirection, 0);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Create collider for character fists
            _fistCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);

            //Spawn particles and hitbox
            _visualPrefabInstances.Item1 = MonoBehaviour.Instantiate(abilityData.visualPrefab, ownerMoveset.MeleeHitBoxSpawnTransform);
            HitColliderBehaviour hitScript = HitColliderSpawner.SpawnBoxCollider(_visualPrefabInstances.Item1.transform, _visualPrefabInstances.Item1.transform.localScale / 2, _fistCollider);
            hitScript.DebuggingEnabled = true;

            //Apply downward force
            Vector3 spikeVelocity = AddForce(false).normalized * abilityData.GetCustomStatValue("DownwardSpeed");
            _knockBackBehaviour.Physics.ApplyVelocityChange(spikeVelocity, false);
            _knockBackBehaviour.Physics.Gravity = _ownerGravity * abilityData.GetCustomStatValue("DownwardGravityMultiplier");
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            //Destroy particles and hit box
            MonoBehaviour.Destroy(_visualPrefabInstances.Item1);

            //Reset character gravity to default
            _knockBackBehaviour.Physics.Gravity = _ownerGravity;

            //Stop momentum if the character isn't somehow in knock back
            if (!_knockBackBehaviour.IsTumbling)
                _knockBackBehaviour.Physics.StopVelocity();
        }

        protected override void End()
        {
            base.End();
            //Enable bouncing
            _knockBackBehaviour.Physics.PanelBounceEnabled = true;
        }

        public override void Update()
        {
            base.Update();

            //If the ability is active and the amount of time it takes to reach the peak has passed...
            if (_forceAdded && Time.time - _timeForceAdded >= _riseTime && CurrentAbilityPhase != AbilityPhase.ACTIVE)
            {
                //...freeze it in air
                _knockBackBehaviour.Physics.StopAllForces();
                _knockBackBehaviour.Physics.Gravity = 0;
            }
        }
    }
}