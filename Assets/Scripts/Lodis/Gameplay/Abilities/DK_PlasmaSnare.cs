using GridGame;
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
        private float _knockbackThreshold;
        private float _liftTime;
        private float _holdTime;
        private bool _opponentCaptured;
        private TimedAction _despawnTimer;
        private ConditionAction _despawnCondition;
        private GameObject _chargeEffectRef;
        private GameObject _chargeEffect;
        private Transform _opponentParent;
        private int _originalChildCount;
        private GameEventListener _returnToPool;
        private float _liftHeight;
        private float _moveSpeed;
        private float _maxX;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _chargeEffectRef = Resources.Load<GameObject>("Effects/Charge_Darkness");
            _opponentParent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform.parent;
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _panelTransform = GetTarget();

            if (!_panelTransform)
                return;

            _spawnPosition = _panelTransform.position + Vector3.up * 0.5f;

            //Get component info from opponent to use later.
            _opponentTransform = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).transform;
            _opponentKnockback = _opponentTransform.GetComponent<KnockbackBehaviour>();

            //Spawn the the holding effect.
            _chargeEffect = ObjectPoolBehaviour.Instance.GetObject(_chargeEffectRef.gameObject, _spawnPosition, Camera.main.transform.rotation);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_chargeEffect, 1);
            _chargeEffect.GetComponent<GridTrackerBehaviour>().Marker = MarkerType.DANGER;

            //Cache stat values to avoid repetitive calls.
            _liftHeight = abilityData.GetCustomStatValue("LiftHeight");
            _knockbackThreshold = abilityData.GetCustomStatValue("KnockbackThreshold");
            _liftTime = abilityData.GetCustomStatValue("LiftTime");
            _holdTime = abilityData.GetCustomStatValue("HoldTime");
            _moveSpeed = abilityData.GetCustomStatValue("MoveSpeed");
            _returnToPool?.ClearActions();

            _maxX = BlackBoardBehaviour.Instance.Grid.Width - BlackBoardBehaviour.Instance.Grid.PanelScale.x;
        }


        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private Transform GetTarget()
        {
            Transform transform = null;

            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner);

            PanelBehaviour targetPanel;
            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(opponent.transform.position, out targetPanel) && targetPanel.Position.y == OwnerMoveScript.Position.y)
                transform = targetPanel.transform;


            return transform;
        }

        private void LiftOpponent(params object[] args)
        {
            //Only check knockback if a player was hit.
            GameObject other = (GameObject)args[0];
            if (!other.CompareTag("Player"))
                return;

            _auraSphere.transform.GetChild(0).gameObject.SetActive(true);
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
            if (!_auraSphere || !_auraSphere.activeInHierarchy || (_opponentKnockback.LastTotalKnockBack < _knockbackThreshold && _opponentCaptured))
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

            //Despawn the old sphere if there is one.
            if (_auraSphere)
                DespawnSphere();

            //Spawn the new sphere and set its effect to inactive by default. The effect should only appear when the opponent is lifted.
            _auraSphere = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, _spawnPosition, new Quaternion());
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

            //Initialize new collider for this attack.
            _collider = _auraSphere.GetComponent<HitColliderBehaviour>();
            _collider.ColliderInfo = GetColliderData(0);
            _collider.Owner = owner;
            _collider.ColliderInfo.OnHit += LiftOpponent;
        }


        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            if (!_opponentCaptured || _originalChildCount == _auraSphere.transform.childCount)
                DespawnSphere();
        }

        protected override void OnMatchRestart()
        {
            DespawnSphere();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            int index = OwnerMoveset.GetSpecialAbilityIndex(this);

            if (OwnerInput?.GetSpecialButton(index + 1) == false)
            {
                UnpauseAbilityTimer();
                DespawnSphere();
            }
            else if (_auraSphere)
            {
                Vector3 position = _auraSphere.transform.position + (Vector3)OwnerInput?.AttackDirection * Time.deltaTime * _moveSpeed;
                position.x = Mathf.Clamp(position.x, 0, _maxX);

                _auraSphere.transform.position = position;
            }
        }
    }
}