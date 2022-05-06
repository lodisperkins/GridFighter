﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.Movement;
using Lodis.GridScripts;

namespace Lodis.Gameplay
{

    public delegate void FallBreakEvent(bool onFloor);
    /// <summary>
    /// Handles all defensive options for any character on the grid.
    /// </summary>
    public class CharacterDefenseBehaviour : MonoBehaviour
    {
        private Movement.KnockbackBehaviour _knockBack;
        private Movement.GridMovementBehaviour _movement;
        private HealthBehaviour _health;
        [Tooltip("How long the object will be parrying for.")]
        [SerializeField]
        private float _parryLength;
        [Tooltip("The collision script for the parry object.")]
        [SerializeField]
        private ColliderBehaviour _parryCollider;
        [SerializeField]
        private bool _isParrying;
        [Tooltip("How long the character will be invincible for after a successful ground parry.")]
        [SerializeField]
        private float _parryInvincibilityLength;
        [Tooltip("True if the parry cooldown timer is 0.")]
        [SerializeField]
        private bool _canParry = true;
        [Tooltip("How long the character is left immobile after a failed parry.")]
        [SerializeField]
        private float _groundParryRestTime;
        [SerializeField]
        private float _parryStartUpTime;
        [Tooltip("How long in seconds is the object braced for it's fall.")]
        [SerializeField]
        private float _braceActiveTime;
        [SerializeField]
        private bool _canBrace;
        [Tooltip("How long in seconds the object has to wait before it can brace itself again.")]
        [SerializeField]
        private float _braceCooldownTime;
        [Tooltip("How long in seconds the object is invincible after catching it's fall.")]
        [SerializeField]
        private float _groundTechInvincibilityTime;
        [Tooltip("How long in seconds the object is invincible after breaking its fall on a wall.")]
        [SerializeField]
        private float _wallTechInvincibilityTime;
        [Tooltip("How long in seconds the object is going to spend breaking its fall.")]
        [SerializeField]
        private float _fallBreakLength;
        [SerializeField]
        private int _wallTechJumpDistance;
        [SerializeField]
        private float _wallTechJumpHeight;
        [SerializeField]
        private float _wallTechJumpDuration;
        [Tooltip("How long in seconds to stun an enemy after a successful parry.")]
        [SerializeField]
        private float _attackerStunTime;
        private RoutineBehaviour.TimedAction _cooldownTimedAction;
        public FallBreakEvent onFallBroken;
        private RoutineBehaviour.TimedAction _parryTimer;
        private RoutineBehaviour.TimedAction _DisableFallBreakAction;

        public bool BreakingFall { get; private set; }
        public float BraceInvincibilityTime { get => _groundTechInvincibilityTime; }
        public bool CanParry { get => _canParry; }
        public bool IsParrying { get => _isParrying; }
        public bool IsBraced { get; private set; }
        public float GroundTechLength { get => _fallBreakLength; set => _fallBreakLength = value; }
        public float WallTechJumpDuration { get => _wallTechJumpDuration; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize components
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _health = GetComponent<HealthBehaviour>();

            _knockBack.AddOnTakeDamageAction(ResetParry);
            _knockBack.AddOnKnockBackAction(EnableBrace);

            _parryCollider.OnHit += ActivateInvinciblity;
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private void ActivateGroundParry()
        {
            if (_knockBack.InHitStun)
                return;

            //Enable parry and update states
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _movement.DisableMovement(condition => _isParrying == false, true, true);
            _canParry = false;
            _knockBack.SetInvincibilityByTimer(_parryLength);

            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateGroundParry(), TimedActionCountType.SCALEDTIME, _parryLength);
        }

        private void DeactivateGroundParry()
        {
            //Disable parry
            _parryCollider.gameObject.SetActive(false);

            //Start timer for player immobility
            if (!_knockBack.IsInvincible)
            {
                RoutineBehaviour.Instance.StartNewTimedAction(args => { _isParrying = false; _canParry = true; }, TimedActionCountType.SCALEDTIME, _groundParryRestTime);
                return;
            }

            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
        }

        /// <summary>
        /// Starts a parry cororoutine. The effects of the parry changes
        /// based on whether or not the character is in hit stun.
        /// </summary>
        public void ActivateParry()
        {
            _parryTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => EnableParryByType(), TimedActionCountType.SCALEDTIME, _parryStartUpTime);
        }

        private void EnableParryByType()
        {
            if (_canParry && _knockBack.CheckIfIdle())
                ActivateGroundParry();
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void ResetParry()
        {
            if (!_isParrying)
                return;

            //_knockBack.DisableInvincibility();

            if (_knockBack.CheckIfIdle())
                DeactivateGroundParry();

            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _canParry = true;
        }

        /// <summary>
        /// Enables the ability to brace and stops the cooldown timer.
        /// </summary>
        private void EnableBrace()
        {
            IsBraced = false;
            _canBrace = true;

            RoutineBehaviour.Instance.StopTimedAction(_cooldownTimedAction);
        }

        /// <summary>
        /// Braces the object so it may catch it's fall.
        /// </summary>
        public void Brace()
        {
            if (!_knockBack.IsTumbling || !_canBrace)
                return;

            ActivateBrace();
        }

        /// <summary>
        /// Braces the object based on the brace active time.
        /// Starts the cooldown when done.
        /// </summary>
        /// <returns></returns>
        private void ActivateBrace()
        {
            IsBraced = true;
            _canBrace = false;
            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateBrace(), TimedActionCountType.SCALEDTIME, _braceActiveTime);
        }

        /// <summary>
        /// Disables the brace and starts the cooldown
        /// </summary>
        private void DeactivateBrace()
        {
            IsBraced = false;
            _cooldownTimedAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _canBrace = true, TimedActionCountType.SCALEDTIME, _braceCooldownTime);
        }

        /// <summary>
        /// Makes the character invincible after a collision occurs
        /// </summary>
        /// <param name="args">The collision arguments</param>
        private void ActivateInvinciblity(params object[] args)
        {
            GameObject other = (GameObject)args[0];
            //Get collider and rigidbody to check the owner and add the force
            ColliderBehaviour otherHitCollider = null;
            //Return if the object collided with doesn't have a collider script attached
            if (args.Length > 0)
            {
                otherHitCollider = other.GetComponentInParent<ColliderBehaviour>();

                if (!otherHitCollider)
                    return;
            }

            if (otherHitCollider.Owner == _parryCollider.Owner)
                return;

            //Deactivate the parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _canParry = true;

            //Make the character invincible for a short amount of time if they're on the ground
            _knockBack.SetInvincibilityByTimer(_parryInvincibilityLength);
        }

        private void OnDrawGizmos()
        {
            if (!_knockBack)
                return;

            if (_knockBack.IsTumbling)
            {
                Collider bounceCollider = _knockBack.Physics.BounceCollider;
                Gizmos.DrawCube(bounceCollider.gameObject.transform.position, bounceCollider.bounds.extents * 1.5f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsBraced || !(other.CompareTag("Structure") || other.CompareTag("Panel")) || BreakingFall)
                return;

            BreakingFall = true;

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            if (panel?.Alignment != _movement.Alignment && other.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByCondition(condition => 
                {
                    BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, true);
                    return panel.Alignment == _movement.Alignment; 
                } );
            else if (other.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
            else
                _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);

            _knockBack.Physics.StopVelocity();
            _knockBack.Physics.IgnoreForces = true;


            if (other.gameObject.CompareTag("Structure") && _DisableFallBreakAction == null)
            {
                _DisableFallBreakAction = RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, WallTechJumpDuration);
                _knockBack.Physics.Jump(_wallTechJumpDistance, _wallTechJumpHeight, _wallTechJumpDuration, true, false, _movement.Alignment);
                onFallBroken?.Invoke(false);
                return;
            }

            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopTimedAction(_parryTimer);

            RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, GroundTechLength);
            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            DeactivateGroundParry();

            onFallBroken?.Invoke(true);
            _knockBack.TryStartLandingLag();
            return;
            //Debug.Log("Collided with " + other.name);
        }

        private void DisableFallBreaking(params object[] args)
        {
            BreakingFall = false;
            _knockBack.Physics.IgnoreForces = false;
            _DisableFallBreakAction = null;
            if (!_knockBack.Physics.IsGrounded) _knockBack.InFreeFall = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsBraced || !(collision.gameObject.CompareTag("Structure") || collision.gameObject.CompareTag("Panel")) || BreakingFall)
                return;

            BreakingFall = true;

            PanelBehaviour panel = collision.gameObject.GetComponent<PanelBehaviour>();

            if (panel?.Alignment != _movement.Alignment && collision.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByCondition(condition => !_movement.IsMoving && _movement.CurrentPanel.Alignment == _movement.Alignment);
            else if (collision.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
            else
                _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);

            _knockBack.Physics.StopVelocity();
            _knockBack.Physics.IgnoreForces = true;

            if (collision.gameObject.CompareTag("Structure") && _DisableFallBreakAction == null)
            {
                _DisableFallBreakAction = RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, _wallTechJumpDuration);
                _knockBack.Physics.Jump(_wallTechJumpDistance, 0, _wallTechJumpDuration, true, false, _movement.Alignment, Vector3.up * _wallTechJumpHeight, DG.Tweening.Ease.OutQuart);
                onFallBroken?.Invoke(false);
                return;
            }

            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopTimedAction(_parryTimer);

            RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, GroundTechLength);
            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            DeactivateGroundParry();

            onFallBroken?.Invoke(true);
            _knockBack.TryStartLandingLag();
            //Debug.Log("Collided with " + other.name);

        }
    }
}
