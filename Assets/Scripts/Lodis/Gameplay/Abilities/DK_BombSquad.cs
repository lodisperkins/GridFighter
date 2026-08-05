using FixedPoints;
using Lodis.AI;
using Lodis.GridScripts;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_BombSquad : Ability
    {
        private int _spawnCount;
        private Fixed32 _spawnDelay;
        private FVector3 _spawnPosition;
        private FixedTimeAction _currentSpawnTimer;
        private GameObject _portal;

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _spawnCount = abilityData.GetCustomStatValue("SpawnCount");
            _spawnDelay = abilityData.GetCustomStatValue("SpawnDelay");
        }

        private void SpawnBomber()
        {
            if (_spawnCount <= 0)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_portal);
                return;
            }

            EntityDataBehaviour spawnedBomb = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), _spawnPosition, Owner.FixedTransform.WorldRotation);

            //Handle explosion setup.
            HitColliderBehaviour bombCollider = spawnedBomb.GetComponent<HitColliderBehaviour>();

            bombCollider.ColliderInfo = GetColliderData(0);
            bombCollider.Spawner = Owner;
            bombCollider.Entity.AddToGame();

            //Handle movement setup.
            NetworkSimpleAIMovementBehaviour aiMovementBehaviour = spawnedBomb.GetComponent<NetworkSimpleAIMovementBehaviour>();
            aiMovementBehaviour.MovementBehaviour.Position = OwnerMoveScript.Position + (FVector2.Right * OwnerMoveScript.GetAlignmentX());

            aiMovementBehaviour.MovementBehaviour.Position = GridBehaviour.Instance.ClampPanelPosition(aiMovementBehaviour.MovementBehaviour.Position, GridAlignment.ANY);

            FVector2 targetPanelPos = BlackBoardBehaviour.Instance.GetOpponentPanel(Owner).Position;

            targetPanelPos.X = OwnerMoveScript.Alignment == GridAlignment.LEFT ? GridBehaviour.Instance.Dimensions.x - 1 : 0;

            aiMovementBehaviour.MoveToLocation(targetPanelPos);

            //Decrease spawn count and setup next spawn.
            _spawnCount--;

            _currentSpawnTimer = FixedPointTimer.StartNewTimedAction(SpawnBomber, _spawnDelay);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Fixed32 offsetX = GridBehaviour.Instance.FixedPanelScale.X * OwnerMoveScript.GetAlignmentX();
            _spawnPosition = Owner.FixedTransform.WorldPosition + new FVector3(offsetX, 1, 0);

            _portal = ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], (Vector3)_spawnPosition, Camera.main.transform.rotation);

            SpawnBomber();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            if (_currentSpawnTimer != null)
            {
                _currentSpawnTimer.Stop();
                _currentSpawnTimer = null;
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(_portal);
        }
    }
}