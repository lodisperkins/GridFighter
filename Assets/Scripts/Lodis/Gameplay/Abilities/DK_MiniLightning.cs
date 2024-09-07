using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_MiniLightning : Ability
    {
        private Transform[] _visualPrefabInstanceTransforms;
        private Transform[] _spawnTransforms;
        private HitColliderBehaviour _collider;
        private Coroutine _spawnRoutine;
        private float _delay;


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
            for (int i = 0; i < _visualPrefabInstanceTransforms[index].childCount; i++)
            {
                Transform child = _visualPrefabInstanceTransforms[index].GetChild(i);
                child.gameObject.SetActive(active);
            }

        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _delay = abilityData.GetCustomStatValue("SpawnDelay");
            _visualPrefabInstanceTransforms = new Transform[3];
            GetTargets();

            //Create object to spawn projectile from
            for (int i = 0; i < _spawnTransforms.Length; i++)
            {
                Transform target = _spawnTransforms[i];
                _visualPrefabInstanceTransforms[i] = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, target.transform.position, new Quaternion()).transform;
                //Initialize hit collider
                _collider = _visualPrefabInstanceTransforms[i].GetComponent<HitColliderBehaviour>();
                _collider.ColliderInfo = GetColliderData(i);
                _collider.Spawner = Owner;

                if (i > 0)
                    _collider.ColliderInfo.TimeActive += _delay;

                //Make all hit boxes inactive by default
                SetChildrenActive(i, false);
            }
        }

        /// <summary>
        /// Finds the transform to aim at when firing lighting
        /// </summary>
        /// <returns></returns>
        private void GetTargets()
        {
            _spawnTransforms = new Transform[3];
            Transform lastSpawnTransform = null;

            for (int i = 0; i < _spawnTransforms.Length; i++)
            {
                Transform transform = null;
                float travelDistance = abilityData.GetCustomStatValue("TravelDistance") + i;
                float direction = OwnerMoveScript.Alignment == GridAlignment.LEFT ? 1 : -1;

                PanelBehaviour targetPanel;
                if (BlackBoardBehaviour.Instance.Grid.GetPanel(OwnerMoveScript.Position + FVector2.Right * direction * travelDistance, out targetPanel))
                    transform = targetPanel.transform;
                else
                    transform = lastSpawnTransform;

                _spawnTransforms[i] = transform;
                lastSpawnTransform = transform;
            }

        }

        private IEnumerator SpawnRoutine()
        {
            for (int i = 0; i < _visualPrefabInstanceTransforms.Length; i++)
            {
                SetChildrenActive(i, true);
                ObjectPoolBehaviour.Instance.GetObject(abilityData.Effects[0], _visualPrefabInstanceTransforms[i].position, new Quaternion());

                yield return new WaitForSeconds(_delay);
            }
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _spawnRoutine = OwnerMoveset.StartCoroutine(SpawnRoutine());
        }

        protected override void OnEnd()
        {
            if (_spawnRoutine != null)
                OwnerMoveset.StopCoroutine(_spawnRoutine);
        }
    }
}