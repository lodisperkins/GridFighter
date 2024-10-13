using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Utility;
using Lodis.Movement;
using FixedPoints;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Throws three bombs in a V shape in front of the player that detonate after a short time.
    /// </summary>
    public class DK_ScatterBomb : SummonAbility
    {
        List<PanelBehaviour> _targetPanels = new List<PanelBehaviour>();
        List<Transform> _bombs = new List<Transform>();
        private float _bombTimer;
        private GameObject _explosionEffect;
        private TimedAction _explosionTimer;
        private HitColliderData _hitColliderData;
        private int _startX = 1;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            _explosionEffect = (GameObject)Resources.Load("Effects/SmallExplosion");
            _bombTimer = abilityData.GetCustomStatValue("BombTimer");
            //Stop the timer to prevent the explosion from happening
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            //Flip the starting x position based on the side of the grid.
            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
            {
                _startX = (int)(GridBehaviour.Grid.Dimensions.x - 2);
            }

            _bombs?.Clear();
            _targetPanels?.Clear();

            SmoothMovement = false;
            _hitColliderData = GetColliderData(0);
        }

        /// <summary>
        /// Despawns all bombs and spawns explosions.
        /// </summary>
        private void DetonateBombs()
        {
            CleanEntityList();

            foreach (GridMovementBehaviour entity in ActiveEntities)
            {
                ExplodeBomb(entity);
            }

            _bombs.Clear();
        }

        /// <summary>
        /// Spawns an explosion at returns the entity to the pool.
        /// </summary>
        private void ExplodeBomb(GridMovementBehaviour entity)
        {
            HitColliderBehaviour collider =  HitColliderSpawner.SpawnCollider(entity.FixedTransform.WorldPosition + FVector3.Up / 2, 1, 1, _hitColliderData, Owner);

            collider.Entity.Data.Name = "Explosion";
            collider.Entity.name = "Explosion";

            Object.Instantiate(_explosionEffect, entity.transform.position, Camera.main.transform.rotation);

            ClearBombEvent(entity);

            ObjectPoolBehaviour.Instance.ReturnGameObject(entity.gameObject, _hitColliderData.TimeActive + 0.1f);

            CameraBehaviour.ShakeBehaviour.ShakeRotation(0.5f);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Positioning bombs on the two back rows.
            Vector2 spawnPos = new Vector2(_startX, 0);
            spawnPos.x = _startX + OwnerMoveScript.GetAlignmentX();

            PanelPositions[0] = (FixedPoints.FVector2)spawnPos;
            PanelPositions[1] = (FixedPoints.FVector2)(spawnPos + Vector2.up);
            PanelPositions[2] = (FixedPoints.FVector2)(spawnPos + Vector2.up * 2);

            //Move the x back 1 row.

            //PanelPositions[3] = spawnPos;
            //PanelPositions[4] = spawnPos + Vector2.up;
            //PanelPositions[5] = spawnPos + Vector2.up * 2;

            base.OnActivate(args);

            //Making the bombs explode on touch.
            foreach (GridMovementBehaviour entity in ActiveEntities)
            {
                ColliderBehaviour collider = entity.GetComponent<ColliderBehaviour>();

                collider.AddOpponentCollisionEvent(collisionArgs => ExplodeBomb(entity));
                collider.Spawner = Owner;
            }

            //Starts the bomb countdown
            _explosionTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => DetonateBombs(), TimedActionCountType.SCALEDTIME, _bombTimer);
        }

        /// <summary>
        /// Removes all collision events from bombs. Used during clean up.
        /// </summary>
        private void ClearBombEvents()
        {
            foreach (GridMovementBehaviour entity in ActiveEntities)
            {
                ClearBombEvent(entity);
            }
        }

        private void ClearBombEvent(GridMovementBehaviour entity)
        {
            ColliderBehaviour collider = entity.GetComponent<ColliderBehaviour>();
            collider.ClearAllCollisionEvents();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            RoutineBehaviour.Instance.StopAction(_explosionTimer);
            ClearBombEvents();
        }
    }
}