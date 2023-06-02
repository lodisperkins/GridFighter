using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class SummonAbility : Ability
    {
        private List<GridMovementBehaviour> _activeEntities;
        private GameObject _entityRef;
        private bool _smoothMovement;
        private int _entityCount;
        private float _moveSpeed;
        private bool _hasMovementComponent;
        private Vector2[] _panelPositions;

        public Vector2[] PanelPositions { get => _panelPositions; set => _panelPositions = value; }
        public int EntityCount { get => _entityCount; private set => _entityCount = value; }
        public float MoveSpeed { get => _moveSpeed; private set => _moveSpeed = value; }
        public bool SmoothMovement { get => _smoothMovement; set => _smoothMovement = value; }
        public List<GridMovementBehaviour> ActiveEntities { get => _activeEntities; private set => _activeEntities = value; }

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            EntityCount = (int)abilityData.GetCustomStatValue("EntityCount");
            _moveSpeed = abilityData.GetCustomStatValue("Speed");
            ActiveEntities = new List<GridMovementBehaviour>();
            PanelPositions = new Vector2[EntityCount];
            _entityRef = abilityData.visualPrefab;
        }

        public void CleanEntityList(bool useName = false)
        {
            for (int i = 0; i < ActiveEntities.Count; i++)
            {
                if (!ActiveEntities[i].gameObject.activeInHierarchy || (ActiveEntities[i].gameObject.name != _entityRef.name + "(" + abilityData.name + ")" && useName))
                {
                    ActiveEntities.RemoveAt(i);
                    i--;
                }
            }
        }

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
            CleanEntityList();
            _hasMovementComponent = _entityRef.TryGetComponent<GridMovementBehaviour>(out _);
        }

        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            GridMovementBehaviour moveBehaviour = null;

            for (int i = 0; i < EntityCount; i++)
            {
                GameObject instance = ObjectPoolBehaviour.Instance.GetObject(_entityRef, OwnerMoveset.ProjectileSpawner.transform.position, OwnerMoveset.ProjectileSpawner.transform.rotation);
                BlackBoardBehaviour.Instance.AddEntityToList(instance);

                if (_hasMovementComponent)
                    moveBehaviour = instance.GetComponent<GridMovementBehaviour>();
                else
                    moveBehaviour = instance.AddComponent<GridMovementBehaviour>();

                moveBehaviour.Position = OwnerMoveScript.CurrentPanel.Position;
                moveBehaviour.Speed = _moveSpeed;
                moveBehaviour.CanBeWalkedThrough = true;
                moveBehaviour.CanMoveDiagonally = true;

                ActiveEntities.Add(moveBehaviour);
                moveBehaviour.MoveToPanel(_panelPositions[i], !SmoothMovement, GridAlignment.ANY, true, false, true);
            }
        }
    }
}