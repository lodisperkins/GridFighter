using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public class SummonAbility : Ability
    {
        private List<GridMovementBehaviour> _activeEntities;
        private GameObject _entityRef;
        private bool _smoothMovement;
        private int _entityCount;
        private float _moveSpeed;
        private FVector2[] _panelPositions;
        private string _id;
        private UnityAction _onMoveEndAction;
        private GridAlignment _alignement = GridAlignment.ANY;

        public FVector2[] PanelPositions { get => _panelPositions; set => _panelPositions = value; }
        public int EntityCount { get => _entityCount; private set => _entityCount = value; }
        public float MoveSpeed { get => _moveSpeed; private set => _moveSpeed = value; }
        public bool SmoothMovement { get => _smoothMovement; set => _smoothMovement = value; }
        public List<GridMovementBehaviour> ActiveEntities { get => _activeEntities; private set => _activeEntities = value; }
        public UnityAction OnMoveEndAction { get => _onMoveEndAction; set => _onMoveEndAction = value; }
        public GridAlignment Alignement { get => _alignement; set => _alignement = value; }

        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            //Init stats
            EntityCount = (int)abilityData.GetCustomStatValue("EntityCount");
            _moveSpeed = abilityData.GetCustomStatValue("Speed");

            ActiveEntities = new List<GridMovementBehaviour>();
            PanelPositions = new FVector2[EntityCount];

            //Reference for entities to spawn.
            _entityRef = abilityData.visualPrefab;

            _id = _entityRef.name + "(" + Owner.name + ")";
        }

        /// <summary>
        /// Removes all entites from the entity list.
        /// </summary>
        /// <param name="useName">If true, entities that share this ID will be cleared regardless of active status.
        /// If false, will only remove inactive entities.</param>
        public void CleanEntityList(bool useName = false)
        {
            for (int i = 0; i < ActiveEntities.Count; i++)
            {
                if (!ActiveEntities[i].gameObject.activeInHierarchy || (ActiveEntities[i].gameObject.name != _id && useName))
                {
                    ActiveEntities.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Returns all entities to the object pool without clearing the list.
        /// </summary>
        public void DisableAllEntities()
        {
            foreach (GridMovementBehaviour entity in ActiveEntities)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(entity.gameObject);
            }
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            CleanEntityList(true);
        }

        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            GridMovementBehaviour moveBehaviour = null;

            //Spawn entities.
            for (int i = 0; i < EntityCount; i++)
            {
                //Get a reference from the object pool and set its ID.
                GameObject instance = ObjectPoolBehaviour.Instance.GetObject(_entityRef, OwnerMoveset.ProjectileSpawner.transform.position, OwnerMoveset.ProjectileSpawner.transform.rotation);
                instance.name = _id;

                //Gets the move component so this can be placed on the grid.
                moveBehaviour = instance.GetComponent<GridMovementBehaviour>();
                BlackBoardBehaviour.Instance.AddEntityToList(moveBehaviour);

                if (!moveBehaviour)
                    moveBehaviour = instance.AddComponent<GridMovementBehaviour>();

                //If the entity should lerp to the position, update the starting position.
                if (SmoothMovement)
                    moveBehaviour.Position = OwnerMoveScript.CurrentPanel.Position;

                //Change attributes so the movement is unrestricted.
                moveBehaviour.Speed = _moveSpeed;
                moveBehaviour.CanBeWalkedThrough = true;
                moveBehaviour.CanMoveDiagonally = true;
                moveBehaviour.Alignment = Alignement;

                //Update entities list.
                ActiveEntities.Add(moveBehaviour);

                moveBehaviour.MoveToPanel(_panelPositions[i], !SmoothMovement, Alignement, true, false, true);

                //If there's something that should happen when this entity stops, it is set here.
                if (OnMoveEndAction != null)
                    moveBehaviour.AddOnMoveEndTempAction(OnMoveEndAction);
            }
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            DisableAllEntities();
        }
    }
}