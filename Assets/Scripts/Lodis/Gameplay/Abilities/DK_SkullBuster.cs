using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
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
    public class DK_SkullBuster : Ability
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private Fixed32 _ownerGravity;
        private  GameObject _visualPrefabInstance;
        private (Coroutine, Coroutine) _visualPrefabCoroutines;
        private GridBehaviour _grid;
        private Fixed32 _timeForceAdded;
        private bool _forceAdded;
        private Fixed32 _riseTime;
        private GridPhysicsBehaviour _opponentPhysics;
        private Fixed32 _distance;
        private Fixed32 _jumpHeight;
        private Fixed32 _oldBounciness;
        private AnimationCurve _curve;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private GameObject _fistTrail;
        private Transform _spawnTransform;

        private HitColliderData _fistCollider;
        private HitColliderData _bodyCollider;
        private HitColliderBehaviour _fistHitScript;
        private HitColliderBehaviour _bodyHitScript;

        private TimedAction _zoomAction;
        private Fixed32 _fistColliderScale;
        private GameObject _hitEffectLoopRef;
        private GameObject _hitEffectLoopInstance;
        private TimedAction _hitLoopDespawnAction;
        private FixedAnimationCurve _fCurve;
        private GameObject _fistEffect;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);

            //Calculates the animation curve for the jump
            Fixed32 hangTime = abilityData.GetCustomStatValue("HangTime") / (abilityData.startUpTime + abilityData.timeActive);
            _riseTime = abilityData.startUpTime / (abilityData.startUpTime + abilityData.timeActive);
            //0.1, 0.5, 0.2
            hangTime = Fixed32.Clamp(hangTime, new Fixed32(6553), new Fixed32(32768)) + new Fixed32(13107);
            _curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(_riseTime, .5f), new Keyframe(hangTime, .5f), new Keyframe(1, 1));
            _fCurve = new FixedAnimationCurve(_curve);

            _knockBackBehaviour = Owner.GetComponent<KnockbackBehaviour>();
            _grid = BlackBoardBehaviour.Instance.Grid;

            //Getting the fire loop effect
            _hitEffectLoopRef = abilityData.Effects[0];

            //Stores the opponents physics script to make them bounce later
            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);

            //Getting the big fist effect
            //_fistEffect = abilityData.Effects[1];

            if (opponent == null) return;
            _opponentPhysics = opponent.GetComponent<GridPhysicsBehaviour>();
            //Initialize default values
            _distance = abilityData.GetCustomStatValue("TravelDistance");
            _jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            _fistColliderScale = abilityData.GetCustomStatValue("FistColliderScale");
            _chargeEffectRef = (GameObject)Resources.Load("Effects/RisingChargeEffect");
            _spawnTransform = OwnerMoveScript.Alignment == GridAlignment.LEFT ? OwnerMoveset.RightMeleeSpawns[1] : OwnerMoveset.LeftMeleeSpawns[1];
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart();
            //Disable character movement so the jump isn't interrupted
            OwnerMoveScript.DisableMovement(condition => !InUse, false, true);

            //Calculate the time it takes to reache the peak height
            _riseTime = abilityData.startUpTime - abilityData.GetCustomStatValue("HangTime");
            //Add the velocity to the character to make them jump
            _knockBackBehaviour.Physics.Jump(_jumpHeight, (int)_distance, abilityData.startUpTime + abilityData.timeActive, true, true, GridAlignment.ANY, FVector3.Up * .3f, _fCurve);
            //Disable bouncing so the character doesn't bounce when landing
            //_knockBackBehaviour.Physics.DisablePanelBounce();

            _chargeEffect = Object.Instantiate(_chargeEffectRef, _spawnTransform);

            //Disable ability benefits if the player is hit out of burst
            OnHit += collision =>
            {

                GameObject objectHit = collision.OtherEntity.UnityObject;

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner) || CurrentAbilityPhase != AbilityPhase.ACTIVE)
                    return;

                CameraBehaviour.Instance.ZoomAmount = 1;
                EnableBounce(collision);
                TryDestroyVisual(objectHit);

                _zoomAction = RoutineBehaviour.Instance.StartNewTimedAction(parameter => CameraBehaviour.Instance.ZoomAmount = 0, TimedActionCountType.SCALEDTIME, 0.7f);
            };

            _bodyCollider = GetColliderData(1);
            OwnerKnockBackScript.EnableSuperArmor(HealthBehaviour.ArmorType.TimeToBreak, abilityData.startUpTime);

            Fixed32 bodyColliderScale = abilityData.GetCustomStatValue("BodyColliderScale");
            _bodyHitScript = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, bodyColliderScale, bodyColliderScale, _bodyCollider, Owner);
            _bodyHitScript.FixedTransform.LocalPosition = FVector3.Zero;
            _bodyHitScript.ColliderInfo.OnHit = OnHit;
            _bodyHitScript.Entity.VisualRoot = _bodyHitScript.gameObject.transform;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Object.Destroy(_chargeEffect);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_bodyHitScript.Entity, true);
            _bodyHitScript = null;
            //Create collider for character fists
            _fistCollider = GetColliderData(0);

            //Spawn particles and hitbox
            //_visualPrefabInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.MeleeHitBoxSpawnTransform);
            _visualPrefabInstance = Object.Instantiate(abilityData.visualPrefab, _spawnTransform);
            _visualPrefabInstance.transform.localPosition += Vector3.back * 0.3f;

            ParticleColorManagerBehaviour manager = _visualPrefabInstance.GetComponent<ParticleColorManagerBehaviour>();
            manager.SetColors(OwnerMoveScript.Alignment);

            //Spawn a game object with the collider attached
            _fistHitScript = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, _fistColliderScale, _fistColliderScale, _fistCollider, Owner);
            _fistHitScript.transform.localPosition = Vector3.zero;
            _fistHitScript.ColliderInfo.OnHit = OnHit;

            GridTrackerBehaviour tracker;
            if (!_fistHitScript.GetComponent<GridTrackerBehaviour>())
            {
                tracker = _fistHitScript.gameObject.AddComponent<GridTrackerBehaviour>();
                tracker.Marker = MarkerType.DANGER;
                tracker.MarkPanelsBasedOnCollision = true;
            }
        }

        /// <summary>
        /// Makes the opponent bouncy after colliding with the ground.
        /// </summary>
        /// <summary>
        /// Makes the opponent bouncy after colliding with the ground.
        /// </summary>
        private void EnableBounce(Collision collision)
        {
            GameObject other = collision.OtherEntity.UnityObject;

            if (_opponentPhysics?.PanelBounceEnabled == true || other != _opponentPhysics.gameObject)
            {
                return;
            }

            KnockbackBehaviour opponentKnockback = _opponentPhysics.Entity.Data.GetComponent<KnockbackBehaviour>();

            if (!opponentKnockback.IsIntangible && !opponentKnockback.IsInvincible)
            {
                _opponentPhysics.SetBounceForce(new GridPhysicsBehaviour.BounceForce(1, new FVector3(0, 25, 0)));
            }

        }

        private void TryDestroyVisual(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (other != _opponentPhysics.gameObject && !other.CompareTag("Panel"))
                return;

            if (other == _opponentPhysics.gameObject)
                _hitEffectLoopInstance = ObjectPoolBehaviour.Instance.GetObject(_hitEffectLoopRef, other.transform.position, CameraBehaviour.Instance.transform.rotation);

            _hitLoopDespawnAction = RoutineBehaviour.Instance.StartNewTimedAction(arguments => ObjectPoolBehaviour.Instance.ReturnGameObject(_hitEffectLoopInstance), TimedActionCountType.UNSCALEDTIME, _fistHitScript.ColliderInfo.HitStunTime);

            Object.Destroy(_visualPrefabInstance);
        }

        private void CleanUpColliders()
        {
            if (_bodyHitScript)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_bodyHitScript.Entity, true);
            }

            if (_fistHitScript)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_fistHitScript.Entity, true);
            }
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            CleanUpColliders();

            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            //Enable bouncing
            _knockBackBehaviour.Physics.DisablePanelBounce();
            if (_visualPrefabInstance)
                Object.Destroy(_visualPrefabInstance);

            if (_chargeEffect)
                Object.Destroy(_chargeEffect);

            RoutineBehaviour.Instance.StopAction(_hitLoopDespawnAction);

            CleanUpColliders();

            if (_hitEffectLoopInstance)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitEffectLoopInstance);

            CameraBehaviour.Instance.ZoomAmount = 0;

            OwnerKnockBackScript.DisableSuperArmor();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            CleanUpColliders();
        }
    }
}