using FixedPoints;
using Lodis.GridScripts;
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
    public class DK_SkullBuster : Ability
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
        private TimedAction _zoomAction;
        private float _colliderScale;
        private GameObject _hitEffectLoopRef;
        private GameObject _hitEffectLoopInstance;
        private TimedAction _hitLoopDespawnAction;
        private FixedAnimationCurve _fCurve;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);

            //Calculates the animation curve for the jump
            float hangTime = abilityData.GetCustomStatValue("HangTime") / (abilityData.startUpTime + abilityData.timeActive);
            _riseTime = abilityData.startUpTime / (abilityData.startUpTime + abilityData.timeActive);
            hangTime = Mathf.Clamp(hangTime, 0.1f, 0.5f) + 0.2f;
            _curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(_riseTime, .5f), new Keyframe(hangTime, .5f), new Keyframe(1, 1));
            _fCurve = new FixedAnimationCurve(_curve);

            _knockBackBehaviour = Owner.GetComponent<KnockbackBehaviour>();
            _grid = BlackBoardBehaviour.Instance.Grid;
            _hitEffectLoopRef = abilityData.Effects[0];

            //Stores the opponents physics script to make them bounce later
            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);
            if (opponent == null) return;
            _opponentPhysics = opponent.GetComponent<GridPhysicsBehaviour>();
            //Initialize default values
            _distance = abilityData.GetCustomStatValue("TravelDistance");
            _jumpHeight = abilityData.GetCustomStatValue("JumpHeight");
            _colliderScale = abilityData.GetCustomStatValue("ColliderScale");
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
            _knockBackBehaviour.Physics.Jump(_jumpHeight, (int)_distance, abilityData.startUpTime + abilityData.timeActive, true, true, GridAlignment.ANY, FVector3.Up * .3f);
            //Disable bouncing so the character doesn't bounce when landing
            _knockBackBehaviour.Physics.DisablePanelBounce();

            _chargeEffect = Object.Instantiate(_chargeEffectRef, _spawnTransform);

            //Disable ability benefits if the player is hit out of burst
            OnHit += collision =>
            {

                GameObject objectHit = collision.OtherEntity.UnityObject;

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner))
                    return;

                CameraBehaviour.Instance.ZoomAmount = 1;
                EnableBounce();
                TryDestroyVisual(objectHit);

                _zoomAction = RoutineBehaviour.Instance.StartNewTimedAction(parameter => CameraBehaviour.Instance.ZoomAmount = 0, TimedActionCountType.SCALEDTIME, 0.7f);
            };

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
            //_hitScript = HitColliderSpawner.SpawnBoxCollider(_spawnTransform, Vector3.one * _colliderScale, _fistCollider, Owner);
            _hitScript.transform.localPosition = Vector3.zero;
            _hitScript.ColliderInfo.OnHit = OnHit;

            GridTrackerBehaviour tracker;
            if (!_hitScript.GetComponent<GridTrackerBehaviour>())
            {
                tracker = _hitScript.gameObject.AddComponent<GridTrackerBehaviour>();
                tracker.Marker = MarkerType.DANGER;
                tracker.MarkPanelsBasedOnCollision = true;
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

        private void TryDestroyVisual(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (other != _opponentPhysics.gameObject && !other.CompareTag("Panel"))
                return;

            if (other == _opponentPhysics.gameObject)
                _hitEffectLoopInstance = ObjectPoolBehaviour.Instance.GetObject(_hitEffectLoopRef, other.transform.position, CameraBehaviour.Instance.transform.rotation);

            _hitLoopDespawnAction = RoutineBehaviour.Instance.StartNewTimedAction(arguments => ObjectPoolBehaviour.Instance.ReturnGameObject(_hitEffectLoopInstance), TimedActionCountType.UNSCALEDTIME, _hitScript.ColliderInfo.HitStunTime);

            Object.Destroy(_visualPrefabInstance);
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            if (_hitScript)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitScript.gameObject);

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


            if (_hitEffectLoopInstance)
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitEffectLoopInstance);
            CameraBehaviour.Instance.ZoomAmount = 0;
        }
    }
}