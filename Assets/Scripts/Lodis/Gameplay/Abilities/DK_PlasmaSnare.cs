using CustomEventSystem;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_PlasmaSnare : Ability
    {
        private Vector3 _spawnPosition;
        private Transform _opponentTransform;
        private Transform _panelTransform;
        private KnockbackBehaviour _opponentKnockback;
        private GameObject _auraSphere;
        private HitColliderBehaviour _collider;
        private float _holdTime;
        private bool _opponentCaptured;
        private TimedAction _despawnTimer;
        private ConditionAction _despawnCondition;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private Transform _opponentParent;
        private int _originalChildCount;
        private Rigidbody _rigidBody;
        private GameEventListener _returnToPool;
        private float _liftHeight;
        private float _moveSpeed;
        private float _maxX;
        private float _ySpeed;
        private float _spawnDistance;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = abilityData.Effects[0];
            _opponentParent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform.parent;
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _spawnDistance = abilityData.GetCustomStatValue("SpawnDistance");
            _opponentTransform = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform;
            _opponentKnockback = _opponentTransform.GetComponent<KnockbackBehaviour>();

            _panelTransform = GetTarget();

            _spawnPosition = _panelTransform.position + Vector3.up * 0.5f;

            //Get component info from opponent to use later.

            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, _spawnPosition, Camera.main.transform.rotation);
            //ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect, 1);

            GridTrackerBehaviour tracker = _chargeEffect.GetComponent<GridTrackerBehaviour>();

            if (!tracker)
                tracker = _chargeEffect.AddComponent<GridTrackerBehaviour>();

            tracker.Marker = MarkerType.DANGER;
            

            //Cache stat values to avoid repetitive calls.
            _liftHeight = abilityData.GetCustomStatValue("LiftHeight");
            _holdTime = abilityData.GetCustomStatValue("HoldTime");
            _moveSpeed = abilityData.GetCustomStatValue("MoveSpeed");
            _returnToPool?.ClearActions();
            _ySpeed = abilityData.GetCustomStatValue("ProjectileSpeed");

            _maxX = BlackBoardBehaviour.Instance.Grid.Width - BlackBoardBehaviour.Instance.Grid.PanelScale.x;
        }


        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private Transform GetTarget()
        {
            Transform transform = null;
            PanelBehaviour targetPanel = null;
            Vector2 position = Vector2.zero;

            if (OwnerMoveScript.Position.y == _opponentKnockback.MovementBehaviour.Position.y)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_opponentKnockback.transform.position, out targetPanel);
            }

            if (!targetPanel)
            {
                position = OwnerMoveScript.Position + (Vector2.right * OwnerMoveScript.GetAlignmentX()) * _spawnDistance;
                BlackBoardBehaviour.Instance.Grid.GetPanel(position, out targetPanel);
            }

            transform = targetPanel.transform;

            return transform;
        }

        private void LiftOpponent(params object[] args)
        {
            //Only check knockback if a player was hit.
            GameObject other = (GameObject)args[0];
            if (!other.CompareTag("Player"))
                return;

            _rigidBody.velocity = Vector3.zero;

            _auraSphere.transform.GetChild(0).gameObject.SetActive(true);
            _auraSphere.transform.position += Vector3.up * _liftHeight;

            //Attaches opponent to sphere and activate lifting. 
            _opponentTransform.parent = _auraSphere.transform;
            _opponentKnockback.Physics.IgnoreForces = true;
            _opponentKnockback.Physics.StopAllForces();

           _opponentTransform.localPosition = Vector3.zero;
            _opponentCaptured = true;
            
            PauseAbilityTimer();

            //Check if the opponent should be let go 
            _opponentKnockback.AddOnKnockBackStartAction(DespawnSphere);
            _despawnTimer = RoutineBehaviour.Instance.StartNewTimedAction(info => DespawnSphere(), TimedActionCountType.SCALEDTIME, _holdTime);
            _despawnCondition = RoutineBehaviour.Instance.StartNewConditionAction(info => DespawnSphere(), condition => !_opponentCaptured || _auraSphere.transform.childCount <= 2 || _opponentKnockback.CurrentAirState != AirState.TUMBLING || _opponentKnockback.IsInvincible);
        }

        private void DespawnSphere()
        {
            if (!_auraSphere || !_auraSphere.activeInHierarchy)
                return;

            RoutineBehaviour.Instance.StopAction(_despawnTimer);
            UnpauseAbilityTimer();

            _opponentKnockback.RemoveOnKnockBackStartAction(DespawnSphere);


            _opponentTransform.parent = _opponentParent;
            _opponentKnockback.Physics.IgnoreForces = false;
            _opponentKnockback.Physics.UseGravity = true;
            _opponentCaptured = false;

            ObjectPoolBehaviour.Instance.ReturnGameObject(_auraSphere);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            _collider.ColliderInfo.OnHit -= LiftOpponent;
            _auraSphere.transform.DOKill();

            EnableAccessory();
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The opponent should only be captured if they are allowed to move at a valid location.
            if (!_panelTransform)
            {
                DespawnSphere();
                return;
            }

            //Spawn the new sphere and set its effect to inactive by default. The effect should only appear when the opponent is lifted.
            _auraSphere = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, _spawnPosition, owner.transform.rotation);
            _auraSphere.transform.GetChild(0).gameObject.SetActive(false);
            _returnToPool = _auraSphere.GetComponent<GameEventListener>();
            _returnToPool.AddAction(() =>
            {
                if (!_opponentTransform)
                    return;

                _opponentTransform.parent = _opponentParent;
            });

            _returnToPool.IntendedSender = _auraSphere;

            _originalChildCount = _auraSphere.transform.childCount;

            _rigidBody = _auraSphere.GetComponent<Rigidbody>();

            _rigidBody.velocity = Vector3.up * _ySpeed;

            //Initialize new collider for this attack.
            _collider = _auraSphere.GetComponent<HitColliderBehaviour>();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
            _collider.ColliderInfo = GetColliderData(0);
            _collider.Owner = owner;
            _collider.ColliderInfo.OnHit += LiftOpponent;
            
            DisableAccessory();
        }


        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            if (!_opponentCaptured || _originalChildCount == _auraSphere.transform.childCount)
                DespawnSphere();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            DespawnSphere();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        protected override void OnMatchRestart()
        {
            DespawnSphere();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!_opponentCaptured)
                return;

            int index = OwnerMoveset.GetSpecialAbilityIndex(this);

            //if (OwnerInput?.GetSpecialButton(index + 1) == false)
            //{
            //    UnpauseAbilityTimer();
            //}
            /*else */if (_auraSphere && OwnerInput)
            {
                Vector3 position = _auraSphere.transform.position + (Vector3)OwnerInput.AttackDirection * Time.deltaTime * _moveSpeed;
                position.x = Mathf.Clamp(position.x, 0, _maxX);
                position.y = Mathf.Clamp(position.y, 1, GridMovementBehaviour.MaxYPosition);
                _auraSphere.transform.position = position;
            }
        }
    }
}