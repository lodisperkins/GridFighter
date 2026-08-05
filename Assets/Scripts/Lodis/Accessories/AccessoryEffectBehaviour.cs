using FixedPoints;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Accessories
{
    public class AccessoryEffectBehaviour : MonoBehaviour
    {
        [SerializeField] private AccessoryData _accessoryData;
        [SerializeField] private EntityDataBehaviour _projectileSpawnPoint;
        [SerializeField] private ColorManagerBehaviour _colorManager;
        [SerializeField] private bool _setColorOnEnable;

        private GameObject _owner;

        public GameObject Owner { get => _owner; set => _owner = value; }
        public AccessoryData Data { get => _accessoryData; set => _accessoryData = value; }

        public FTransform ProjectileSpawnPoint
        {
            get
            {
                if (_projectileSpawnPoint)
                    return _projectileSpawnPoint.FixedTransform;

                return null;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
        }

        private void OnEnable()
        {
            Owner = transform.root.gameObject;

            if (_owner != null && _setColorOnEnable && _colorManager != null)
            {
                GridMovementBehaviour ownerMove = Owner.GetComponentInChildren<GridMovementBehaviour>();

                if (ownerMove != null)
                {
                    _colorManager.SetColors((int)ownerMove.Alignment);
                }
            }
        }

        public virtual void PlayEffect() { }

        public virtual void OnSetToWinPosition() { }

        public virtual void StopEffect() { }


    }
}