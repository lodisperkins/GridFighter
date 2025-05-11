using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_MiniLightning : Ability
    {
        private FTransform[] _visualPrefabInstanceTransforms;
        private FVector3[] _spawnPositions;
        private HitColliderBehaviour _collider;
        private Coroutine _spawnRoutine;
        private FixedTimeAction _spawnAction;
        private Fixed32 _delay;
        private int _currentSpawnIndex;


        protected override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            bw.Write(_currentSpawnIndex);
        }

        protected override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            _currentSpawnIndex = br.ReadInt32();
        }

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        /// <summary>
        /// Toggles whether or not the children that contain the hitboxes are active in the hierarchy
        /// </summary>
        private void SetChildrenActive(int index, bool active)
        {
            for (int i = 0; i < _visualPrefabInstanceTransforms[index].ChildCount; i++)
            {
                FTransform child = _visualPrefabInstanceTransforms[index].GetChild(i);
                child.EntityData.Active = active;
            }
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _currentSpawnIndex = 0;
            _delay = abilityData.GetCustomStatValue("SpawnDelay");
            _visualPrefabInstanceTransforms = new FTransform[3];
            GetTargets();

            //Create object to spawn projectile from
            for (int i = 0; i < _spawnPositions.Length; i++)
            {
                FVector3 targetPosition = _spawnPositions[i];
                _visualPrefabInstanceTransforms[i] = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), targetPosition, new FQuaternion()).FixedTransform;
                //Initialize hit collider
                _collider = _visualPrefabInstanceTransforms[i].EntityData.GetComponent<HitColliderBehaviour>();
                _collider.ColliderInfo = GetColliderData(i);
                _collider.Spawner = Owner;

                if (i > 0)
                {
                    _collider.ColliderInfo.TimeActive += _delay;
                }

                //Make all hit boxes inactive by default
                _visualPrefabInstanceTransforms[i].EntityData.Active = false;
            }
        }

        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private void GetTargets()
        {
            _spawnPositions = new FVector3[3];
            FVector3 lastSpawnTransform = FVector3.Zero;

            for (int i = 0; i < _spawnPositions.Length; i++)
            {
                FVector3 spawnPosition = FVector3.Zero;
                Fixed32 travelDistance = abilityData.GetCustomStatValue("TravelDistance") + i;
                Fixed32 direction = OwnerMoveScript.Alignment == GridAlignment.LEFT ? 1 : -1;

                PanelBehaviour targetPanel;
                FVector2 position = OwnerMoveScript.Position + FVector2.Right * direction * travelDistance;

                if (BlackBoardBehaviour.Instance.Grid.GetPanel(position, out targetPanel))
                {
                    spawnPosition = targetPanel.FixedWorldPosition;
                }
                else
                {
                    spawnPosition = lastSpawnTransform;
                }

                _spawnPositions[i] = spawnPosition;
                lastSpawnTransform = spawnPosition;
            }

        }

        int count = 0;
        private void Spawn()
        {
            if (_currentSpawnIndex >= _visualPrefabInstanceTransforms.Length)
            {
                return;
            }
            count++;
            Debug.Log("Laser: " + count);
            _visualPrefabInstanceTransforms[_currentSpawnIndex].EntityData.Active = true;
            ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], (Vector3)_visualPrefabInstanceTransforms[_currentSpawnIndex].WorldPosition, new Quaternion());
            _currentSpawnIndex++;
        }

        private IEnumerator SpawnRoutine()
        {
            for (int i = 0; i < _visualPrefabInstanceTransforms.Length; i++)
            {

                yield return new WaitForSeconds(_delay);
            }
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _spawnAction = FixedPointTimer.StartNewTimedAction(Spawn, _delay).Loop(_visualPrefabInstanceTransforms.Length + 2);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            _spawnAction?.Stop();
        }
    }
}