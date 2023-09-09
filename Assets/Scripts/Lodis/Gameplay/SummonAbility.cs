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
        private Vector2[] _panelPositions;
        private string _id;
        private UnityAction _onMoveEndAction;
        private GridAlignment _alignement = GridAlignment.ANY;

        public Vector2[] PanelPositions { get => _panelPositions; set => _panelPositions = value; }
        public int EntityCount { get => _entityCount; private set => _entityCount = value; }
        public float MoveSpeed { get => _moveSpeed; private set => _moveSpeed = value; }
        public bool SmoothMovement { get => _smoothMovement; set => _smoothMovement = value; }
        public List<GridMovementBehaviour> ActiveEntities { get => _activeEntities; private set => _activeEntities = value; }
        public UnityAction OnMoveEndAction { get => _onMoveEndAction; set => _onMoveEndAction = value; }
        public GridAlignment Alignement { get => _alignement; set => _alignement = value; }

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            EntityCount = (int)abilityData.GetCustomStatValue("EntityCount");
            _moveSpeed = abilityData.GetCustomStatValue("Speed");
            ActiveEntities = new List<GridMovementBehaviour>();
            PanelPositions = new Vector2[EntityCount];
            _entityRef = abilityData.visualPrefab;

            _id = _entityRef.name + "(" + owner.name + ")";
        }

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

            for (int i = 0; i < EntityCount; i++)
            {
                GameObject instance = ObjectPoolBehaviour.Instance.GetObject(_entityRef, OwnerMoveset.ProjectileSpawner.transform.position, OwnerMoveset.ProjectileSpawner.transform.rotation);
                
                instance.name = _id;

                moveBehaviour = instance.GetComponent<GridMovementBehaviour>();
                BlackBoardBehaviour.Instance.AddEntityToList(moveBehaviour);
                if (!moveBehaviour)
                    moveBehaviour = instance.AddComponent<GridMovementBehaviour>();

                if (SmoothMovement)
                    moveBehaviour.Position = OwnerMoveScript.CurrentPanel.Position;

                moveBehaviour.Speed = _moveSpeed;
                moveBehaviour.CanBeWalkedThrough = true;
                moveBehaviour.CanMoveDiagonally = true;
                moveBehaviour.Alignment = Alignement;

                ActiveEntities.Add(moveBehaviour);
                moveBehaviour.MoveToPanel(_panelPositions[i], !SmoothMovement, Alignement, true, false, true);

                if (OnMoveEndAction != null)
                    moveBehaviour.AddOnMoveEndTempAction(OnMoveEndAction);
            }
        }
    }
}