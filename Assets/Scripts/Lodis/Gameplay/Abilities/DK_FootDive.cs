﻿using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
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
        private HitColliderData _fistCollider;
        private  GameObject _visualPrefabInstance;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;
        private GridBehaviour _grid;
        private float _timeForceAdded;
        private bool _forceAdded;
        private float _riseTime;
        private GridPhysicsBehaviour _opponentPhysics;
        private float _distance;
        private float _jumpHeight;
        private float _oldBounciness;
        private AnimationCurve _curve;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private GameObject _fistTrail;
        private Transform _spawnTransform;
        private HitColliderBehaviour _hitScript;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //Calculates the animation curve for the jump
            float hangTime = abilityData.GetCustomStatValue("HangTime") / (abilityData.startUpTime + abilityData.timeActive);
            _riseTime = abilityData.startUpTime / (abilityData.startUpTime + abilityData.timeActive);
            hangTime = Mathf.Clamp(hangTime, 0.1f, 0.5f) + 0.2f;
            _curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(_riseTime, .5f), new Keyframe(hangTime, .5f), new Keyframe(1, 1)); 

            _knockBackBehaviour = owner.GetComponent<KnockbackBehaviour>();
            _grid = BlackBoardBehaviour.Instance.Grid;

            //Stores the opponents physics script to make them bounce later
            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner);
            if (opponent == null) return;
            _opponentPhysics = opponent.GetComponent<GridPhysicsBehaviour>();

            //Initialize default values
            _distance = abilityData.GetCustomStatValue("TravelDistance");
            _jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            _chargeEffectRef = (GameObject)Resources.Load("Effects/RisingChargeEffect");
            _spawnTransform = _ownerMoveScript.Alignment == GridAlignment.LEFT ? OwnerMoveset.RightMeleeSpawns[1] : OwnerMoveset.LeftMeleeSpawns[1];
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart();
            //Disable character movement so the jump isn't interrupted
            _ownerMoveScript.DisableMovement(condition => !InUse, false, true);

            //Calculate the time it takes to reache the peak height
            _riseTime = abilityData.startUpTime - abilityData.GetCustomStatValue("HangTime");
            //Add the velocity to the character to make them jump
            _knockBackBehaviour.Physics.Jump((int)_distance, _jumpHeight, abilityData.startUpTime + abilityData.timeActive, true, true, GridAlignment.ANY, Vector3.up * .3f, _curve);
            //Disable bouncing so the character doesn't bounce when landing
            _knockBackBehaviour.Physics.DisablePanelBounce();

            _chargeEffect = Object.Instantiate(_chargeEffectRef, _spawnTransform);

        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Object.Destroy(_chargeEffect);
            //Create collider for character fists
            _fistCollider = GetColliderData(0);

            //Spawn particles and hitbox
            //_visualPrefabInstances.Item1 = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.MeleeHitBoxSpawnTransform);
            _visualPrefabInstance = Object.Instantiate(abilityData.visualPrefab, _spawnTransform);
            _visualPrefabInstance.transform.localPosition += Vector3.back * 0.3f;
            //Spawn a game object with the collider attached
            _hitScript = HitColliderSpawner.SpawnBoxCollider(_spawnTransform, Vector3.one / 2, _fistCollider, owner);
            _hitScript.transform.localPosition = Vector3.zero;
            _hitScript.AddCollisionEvent(EnableBounce);
            _hitScript.AddCollisionEvent(context => Object.Destroy(_visualPrefabInstance));

            GridTrackerBehaviour tracker;
            if (!_hitScript.GetComponent<GridTrackerBehaviour>())
            {
                tracker = _hitScript.gameObject.AddComponent<GridTrackerBehaviour>();
                tracker.Marker = MarkerType.DANGER;
            }
        }

        /// <summary>
        /// Makes the opponent bouncy after colliding with the ground.
        /// </summary>
        private void EnableBounce(params object[] args)
        {
            if (_opponentPhysics?.PanelBounceEnabled == true)
                return;

            float bounciness = abilityData.GetCustomStatValue("OpponentBounciness");

            //Enable the panel bounce and set the temporary bounce value using the custom bounce stat.
            _opponentPhysics.EnablePanelBounce(false);
            _oldBounciness = _opponentPhysics.Bounciness;
            _opponentPhysics.Bounciness = bounciness;
            string opponentState = BlackBoardBehaviour.Instance.GetPlayerState(_opponentPhysics.gameObject);

            //Starts a new delayed action to disable the panel bouncing after it has bounced once. 
            RoutineBehaviour.Instance.StartNewConditionAction(parameters => { _opponentPhysics.DisablePanelBounce(); _opponentPhysics.Bounciness = _oldBounciness; }, condition => _opponentPhysics.IsGrounded || opponentState != "Tumbling");
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            if (_hitScript)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitScript.gameObject);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            //Enable bouncing
            _knockBackBehaviour.Physics.DisablePanelBounce();
            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);

        }

        public override void EndAbility()
        {
            base.EndAbility();
            //Destroy particles and hit box
            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);

            if (_chargeEffect)
                Object.Destroy(_chargeEffect);
        }

        public override void StopAbility()
        {
            base.StopAbility();

            //Destroy particles and hit box
            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);

            if (_chargeEffect)
                Object.Destroy(_chargeEffect);
        }
    }
}