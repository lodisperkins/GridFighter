 using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// An unblockable ability that drops a shot onto the opponents head if they are on the same row.
    /// </summary>
    public class U_LightningStrike : Ability
    {
        private Transform _visualPrefabInstanceTransform;

        private float _oldBounciness;
        private GridPhysicsBehaviour _opponentPhysics;
        private HitColliderBehaviour _collider;
        private GameObject _stompEffectRef;
        private GameObject _stompEffect;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _stompEffectRef = Resources.Load<GameObject>("Effects/LightningStomp");
        }

        /// <summary>
        /// Toggles whether or not the children that contain the hitboxes are active in the hierarchy
        /// </summary>
        private void ToggleChildren()
        {
            for (int i = 0; i < _visualPrefabInstanceTransform.childCount; i++)
            {
                Transform child = _visualPrefabInstanceTransform.GetChild(i);
                child.gameObject.SetActive(!child.gameObject.activeInHierarchy);
            }
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            Transform target = GetTarget();

            if (!target) return;

            //Stores the opponents physics script to make them bounce later
            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner);
            if (opponent == null) return;
            _opponentPhysics = opponent.GetComponent<GridPhysicsBehaviour>();

            //Create object to spawn projectile from
            _visualPrefabInstanceTransform = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, target.transform.position, new Quaternion()).transform;

            //Initialize hit collider
            _collider = _visualPrefabInstanceTransform.GetComponent<HitColliderBehaviour>();
            _collider.ColliderInfo = GetColliderData(0);
            _collider.Owner = owner;

            _collider.AddCollisionEvent(EnableBounce);
            //Activate hitboxes attached to lighting object
            ToggleChildren();
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
            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(opponent.transform.position, out targetPanel) && targetPanel.Position.y == _ownerMoveScript.Position.y)
                transform = targetPanel.transform;


            return transform;
        }
        /// <summary>
        /// Makes the opponent bouncy after colliding with the ground.
        /// </summary>
        private void EnableBounce(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            if (_opponentPhysics?.PanelBounceEnabled == true || other != _opponentPhysics.gameObject)
                return;

            float bounciness = abilityData.GetCustomStatValue("OpponentBounciness");

            //Enable the panel bounce and set the temporary bounce value using the custom bounce stat.
            _opponentPhysics.EnablePanelBounce(false);
            _oldBounciness = _opponentPhysics.Bounciness;
            _opponentPhysics.Bounciness = bounciness;

            //Starts a new delayed action to disable the panel bouncing after it has bounced once. 
            RoutineBehaviour.Instance.StartNewConditionAction(parameters => { _opponentPhysics.DisablePanelBounce(); _opponentPhysics.Bounciness = _oldBounciness; }, condition => _opponentPhysics.IsGrounded);
        }
        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            if (!_visualPrefabInstanceTransform) return;

            _stompEffect = MonoBehaviour.Instantiate(_stompEffectRef, owner.transform.position, Camera.main.transform.rotation);

            ToggleChildren();
        }
    }
}