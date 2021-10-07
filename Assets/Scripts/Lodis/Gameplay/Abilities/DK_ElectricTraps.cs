using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_ElectricTraps : ProjectileAbility
    {
        private float _maxTravelDistance;
        private List<Movement.GridMovementBehaviour> _linkMoveScripts;
        private GameObject _attackLinkVisual;
        private HitColliderBehaviour _stunCollider;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _attackLinkVisual = (GameObject)Resources.Load("Structures/AttackLink");
            _linkMoveScripts = new List<Movement.GridMovementBehaviour>();
        }

        /// <summary>
        /// Deploys one of the links
        /// </summary>
        private void FireLink()
        {
            //Creates copy of link prefab
            GameObject visualPrefab = MonoBehaviour.Instantiate(abilityData.visualPrefab, spawnTransform.position, abilityData.visualPrefab.transform.rotation);
            //Get the movement script attached and add it to a list
            Movement.GridMovementBehaviour gridMovement = visualPrefab.GetComponent<Movement.GridMovementBehaviour>();
            gridMovement.Position = _ownerMoveScript.Position;
            gridMovement.Speed = abilityData.GetCustomStatValue("Speed");
            _linkMoveScripts.Add(gridMovement);

            //Makes the link move until it runs into an obstacle
            for (int i = (int)_maxTravelDistance; i >= 0; i--)
            {
                Vector2 moveOffset = new Vector2(i, 0);
                if (gridMovement.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * owner.transform.forward.x, false, GridScripts.GridAlignment.ANY))
                    break;
            }
        }

        /// <summary>
        /// Activates the hitboxes along the path
        /// </summary>
        private void ActivateStunPath()
        {
            //Creates a new collider for the attackLinks in the path to use
            _stunCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("KnockBackScale"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.GetCustomStatValue("Lifetime"), owner);

            //When the attackLinks in the path collide with an something else, try to stun it 
            _stunCollider.onHit += StunEntity;

            //Gets a path from the first link to the second link
            List<PanelBehaviour> panels = AI.AIUtilities.Instance.GetPath(_linkMoveScripts[0].CurrentPanel, _linkMoveScripts[1].CurrentPanel, true);

            //Spawns attackLinks on each panel in the path
            for (int i = 0; i < panels.Count; i++)
            {
                GameObject attackLink = MonoBehaviour.Instantiate(_attackLinkVisual, panels[i].transform.position + new Vector3(0, .5f,0), _attackLinkVisual.transform.rotation);
                HitColliderBehaviour collider = attackLink.AddComponent<HitColliderBehaviour>();
                HitColliderBehaviour.Copy(_stunCollider, collider);
            }

        }

        /// <summary>
        /// Stuns the object that came in contact with a link
        /// </summary>
        /// <param name="args"></param>
        private void StunEntity(params object[] args)
        {
            //Get health beahviour
            GameObject entity = (GameObject)args[0];
            HealthBehaviour entityHealth = entity.GetComponent<HealthBehaviour>();

            //If there  is a health behaviour...
            if (entityHealth)
                //...stun the entity
                entityHealth.Stun(abilityData.GetCustomStatValue("StunTime"));
        }

        /// <summary>
        /// Destroys all links
        /// </summary>
        /// <param name="time"></param>
        private void DestroyLinks(float time)
        {
            if (_linkMoveScripts.Count == 0)
                return;

            if (_linkMoveScripts[0])
                MonoBehaviour.Destroy(_linkMoveScripts[0].gameObject, time);

            if (_linkMoveScripts.Count <= 1)
                return;

            if (_linkMoveScripts[1])
                MonoBehaviour.Destroy(_linkMoveScripts[1].gameObject, time);
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Switch to know which stage of the ability should be activated
            switch (currentActivationAmount)
            {
                case 1:
                case 2:
                    _maxTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");
                    FireLink();
                    break;
                case 3:
                    ActivateStunPath();
                    DestroyLinks(abilityData.GetCustomStatValue("Lifetime"));
                    break;
            }
        }

        public override void EndAbility()
        {
            base.EndAbility();

            DestroyLinks(0);
        }
    }
}