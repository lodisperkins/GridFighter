using FixedPoints;
using Lodis.GridScripts;
using Lodis.Utility;
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
        private EntityDataBehaviour _attackLinkVisual;
        private HitColliderData _stunCollider;

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            _attackLinkVisual = ((GameObject)Resources.Load("Structures/ElectricTraps/AttackLink")).GetComponent<EntityDataBehaviour>();
            _linkMoveScripts = new List<Movement.GridMovementBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            OwnerKnockBackScript.AddOnKnockBackTempAction(() =>
            {
                DestroyLinks(1);
                EndAbility();
            });
            if (currentActivationAmount == 0)
                _linkMoveScripts.Clear();
        }

        /// <summary>
        /// Deploys one of the links
        /// </summary>
        private void FireLink(FVector2 position)
        {
            //Creates copy of link prefab
            EntityDataBehaviour visualPrefab = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), SpawnTransform.WorldPosition, FQuaternion.Identity);
            //Get the movement script attached and add it to a list
            Movement.GridMovementBehaviour gridMovement = visualPrefab.GetComponent<Movement.GridMovementBehaviour>();

            //Place the link in its appropriate position
            gridMovement.MoveToPanel((FixedPoints.FVector2)position, true, GridAlignment.ANY, true);
            gridMovement.Speed = abilityData.GetCustomStatValue("Speed");

            _linkMoveScripts.Add(gridMovement);
        }

        /// <summary>
        /// Activates the hitboxes along the path
        /// </summary>
        private void ActivateStunPath()
        {
            //Creates a new collider for the attackLinks in the path to use
            _stunCollider = GetColliderData(0);

            //When the attackLinks in the path collide with an something else, try to stun it 
            _stunCollider.OnHit += StunEntity;

            //Gets a path from the first link to the second link
            List<PanelBehaviour> panels = AI.AIUtilities.Instance.GetPath(_linkMoveScripts[0].CurrentPanel, _linkMoveScripts[1].CurrentPanel, true);

            //Spawns attackLinks on each panel in the path
            for (int i = 0; i < panels.Count; i++)
            {
                EntityDataBehaviour attackLink = ObjectPoolBehaviour.Instance.GetObject(_attackLinkVisual, panels[i].FixedWorldPosition + FVector3.Up * 0.8f, FQuaternion.Identity);

                HitColliderBehaviour collider = attackLink.GetComponent<HitColliderBehaviour>();

                if (!collider)
                {
                    collider = attackLink.Data.AddComponent<HitColliderBehaviour>();
                }

                collider.GetComponent<Collider>().enabled = true;
                collider.Spawner = Owner;
                OnHitTemp += args => collider.GetComponent<Collider>().enabled = false;
                collider.ColliderInfo = _stunCollider;
            }

            DestroyLinks(1);
        }

        /// <summary>
        /// Stuns the object that came in contact with a link
        /// </summary>
        /// <param name="args"></param>
        private void StunEntity(Collision collision)
        {
            //Get health beahviour
            GameObject entity = collision.OtherEntity.UnityObject;
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
                ObjectPoolBehaviour.Instance.ReturnGameObject(_linkMoveScripts[0].gameObject, time);

            if (_linkMoveScripts.Count <= 1)
                return;

            if (_linkMoveScripts[1])
                ObjectPoolBehaviour.Instance.ReturnGameObject(_linkMoveScripts[1].gameObject, time);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Activates the links if this is the second use of this ability
            if (currentActivationAmount > 1)
            {
                ActivateStunPath();
                return;;
            }

            SpawnTransform = OwnerMoveset.ProjectileSpawner.FixedTransform;

            FVector2 attackDirection = (FVector2)args[1];
            
            //Switch to know where to place the ability on the stage based on the direction given
            switch (attackDirection)
            {
                case FVector2 dir when dir.Equals(FVector2.Right):
                    FireLink(ShotDirection);
                    FireLink(ShotDirection);
                    break;
                case FVector2 dir when dir.Equals(FVector2.Left):
                    FireLink(ShotDirection);
                    FireLink(ShotDirection);
                    break;
                case FVector2 dir when dir.Equals(FVector2.Up):
                    FireLink(ShotDirection);
                    FireLink(ShotDirection);
                    break;
                case FVector2 dir when dir.Equals(FVector2.Down):
                    FireLink(ShotDirection);
                    FireLink(ShotDirection);
                    break;
                default:
                    FireLink(ShotDirection);
                    FireLink(ShotDirection);
                    break;
                    
            }
        }

        protected override void OnMatchRestart()
        {
            DestroyLinks(0);
        }
    }
}