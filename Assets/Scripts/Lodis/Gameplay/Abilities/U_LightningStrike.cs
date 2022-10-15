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

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        private void ToggleChildren()
        {
            for (int i = 0; i < _visualPrefabInstanceTransform.childCount; i++)
            {
                Transform child = _visualPrefabInstanceTransform.GetChild(i);
                child.gameObject.SetActive(!child.gameObject.activeInHierarchy);
            }
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);

            Transform target = GetTarget();

            if (!target) return;

            //Create object to spawn projectile from
            _visualPrefabInstanceTransform = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, target.transform.position, new Quaternion()).transform;
            HitColliderBehaviour collider = _visualPrefabInstanceTransform.GetComponent<HitColliderBehaviour>();
            collider.ColliderInfo = GetColliderData(0);
            collider.Owner = owner;
            ToggleChildren();
        }

        private Transform GetTarget()
        {
            Transform transform = null;

            GameObject opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner);

            PanelBehaviour targetPanel;
            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(opponent.transform.position, out targetPanel) && targetPanel.Position.y == _ownerMoveScript.Position.y)
                transform = targetPanel.transform;

            return transform;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            if (!_visualPrefabInstanceTransform) return;

            ToggleChildren();
        }
    }
}