using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lodis.Gameplay
{

    /// <summary>
    /// An unblockable ability that drops a shot onto the opponents head if they are on the same row.
    /// </summary>
    public class DK_LightningStrike : Ability
    {
        private FTransform _visualPrefabInstanceTransform;

        private float _oldBounciness;
        private GridPhysicsBehaviour _opponentPhysics;
        private HitColliderBehaviour _collider;
        private GameObject _stompEffectRef;
        private GameObject _stompEffect;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _stompEffectRef = abilityData.Effects[0];
        }

        ///// <summary>
        ///// Toggles whether or not the children that contain the hitboxes are active in the hierarchy
        ///// </summary>
        //private void ToggleChildren()
        //{
        //    for (int i = 0; i < _visualPrefabInstanceTransform.childCount; i++)
        //    {
        //        Transform child = _visualPrefabInstanceTransform.GetChild(i);
        //        child.gameObject.SetActive(!child.gameObject.activeInHierarchy);
        //    }
        //}

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
        }

        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private bool GetTarget(out FVector3 position)
        {
            Transform transform = null;

            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);

            position = FVector3.Zero;

            PanelBehaviour targetPanel;
            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(opponent.transform.position, out targetPanel) && targetPanel.Position.Y == OwnerMoveScript.Position.Y)
                position = targetPanel.FixedWorldPosition;
            else
                return false;


            return true;
        }
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
                _opponentPhysics.SetBounceForce(new GridPhysicsBehaviour.BounceForce(1, new FVector3(0, 25, 0), false));
            }
            
        }
        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

            _stompEffect = MonoBehaviour.Instantiate(_stompEffectRef, Owner.transform.position, Camera.main.transform.rotation);

            FVector3 targetPosition;

            if (!GetTarget(out targetPosition)) return;

            //Stores the opponents physics script to make them bounce later
            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);
            if (opponent == null) return;

            _opponentPhysics = opponent.GetComponent<GridPhysicsBehaviour>();

            //Create object to spawn projectile from
            EntityDataBehaviour entity = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), targetPosition, FQuaternion.Identity);

            //Initialize hit collider
            _collider = entity.GetComponent<HitColliderBehaviour>();
            _collider.ColliderInfo = GetColliderData(0);
            _collider.Spawner = Owner;

            _collider.AddCollisionEvent(EnableBounce);
        }
    }
}