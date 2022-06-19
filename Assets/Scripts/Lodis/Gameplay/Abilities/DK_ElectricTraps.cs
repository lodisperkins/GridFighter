﻿using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Needs to be activated at least three times
    ///to be fully used.The first activation places
    ///1 link on the grid, the second activation
    ///places another.When the ability is activated
    ///a third time, a current of electricity flows
    ///between both links. If the opponent is caught
    ///in the current they are stunned.
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
        private void FireLink(Vector2 position)
        {
            //Creates copy of link prefab
            GameObject visualPrefab = Object.Instantiate(abilityData.visualPrefab, SpawnTransform.position, abilityData.visualPrefab.transform.rotation);
            //Get the movement script attached and add it to a list
            Movement.GridMovementBehaviour gridMovement = visualPrefab.GetComponent<Movement.GridMovementBehaviour>();
            gridMovement.Position = position;
            gridMovement.Speed = abilityData.GetCustomStatValue("Speed");
            _linkMoveScripts.Add(gridMovement);
        }

        /// <summary>
        /// Activates the hitboxes along the path
        /// </summary>
        private void ActivateStunPath()
        {
            //Creates a new collider for the attackLinks in the path to use
            _stunCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);

            //When the attackLinks in the path collide with an something else, try to stun it 
            _stunCollider.OnHit += StunEntity;

            //Gets a path from the first link to the second link
            List<PanelBehaviour> panels = AI.AIUtilities.Instance.GetPath(_linkMoveScripts[0].CurrentPanel, _linkMoveScripts[1].CurrentPanel, true);

            //Spawns attackLinks on each panel in the path
            for (int i = 0; i < panels.Count; i++)
            {
                GameObject attackLink = Object.Instantiate(_attackLinkVisual, panels[i].transform.position + new Vector3(0, .5f,0), _attackLinkVisual.transform.rotation);
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
                Object.Destroy(_linkMoveScripts[0].gameObject, time);

            if (_linkMoveScripts.Count <= 1)
                return;

            if (_linkMoveScripts[1])
                Object.Destroy(_linkMoveScripts[1].gameObject, time);
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            if (currentActivationAmount > 1)
            {
                ActivateStunPath();
                return;;
            }
            
            
            //If the owner doesn't have a transform to spawn projectiles from...
            SpawnTransform = !ownerMoveset.ProjectileSpawnTransform ? owner.transform : ownerMoveset.ProjectileSpawnTransform;

            Vector2 attackDirection = (Vector2)args[1];

            int gridTempMaxColumns = BlackBoardBehaviour.Instance.Grid.TempMaxColumns;
            int closestPanelX = gridTempMaxColumns;

            if (owner.transform.forward.x < 0)
                closestPanelX--;
                    
            Vector2 dimensions = BlackBoardBehaviour.Instance.Grid.Dimensions;
                    int ownerFacing = (int)owner.transform.forward.x;
            
            //Switch to know which stage of the ability should be activated
            switch (attackDirection)
            {
                case Vector2 dir when dir.Equals(Vector2.right):
                    FireLink(new Vector2(closestPanelX + (gridTempMaxColumns - 1) * ownerFacing, dimensions.y - 1));
                    FireLink(new Vector2(closestPanelX + (gridTempMaxColumns - 1) * ownerFacing, 0));
                    break;
                case Vector2 dir when dir.Equals(Vector2.left):
                    FireLink(new Vector2(closestPanelX, dimensions.y - 1));
                    FireLink(new Vector2(closestPanelX, 0));
                    break;
                case Vector2 dir when dir.Equals(Vector2.up):
                    FireLink(new Vector2(closestPanelX, dimensions.y - 1));
                    FireLink(new Vector2(closestPanelX + (gridTempMaxColumns - 1) * ownerFacing, dimensions.y - 1));
                    break;
                case Vector2 dir when dir.Equals(Vector2.down):
                    FireLink(new Vector2(closestPanelX, 0));
                    FireLink(new Vector2(closestPanelX + (gridTempMaxColumns - 1) * ownerFacing, 0));
                    break;
                default:
                    FireLink(new Vector2(closestPanelX, dimensions.y - 2));
                    FireLink(new Vector2(closestPanelX + (gridTempMaxColumns - 1) * ownerFacing, dimensions.y - 2));
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