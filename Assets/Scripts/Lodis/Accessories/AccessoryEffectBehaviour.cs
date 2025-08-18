using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Accessories
{
    public class AccessoryEffectBehaviour : MonoBehaviour
    {
        [SerializeField] private AccessoryData _accessoryData;
        private GameObject _owner;

        public GameObject Owner { get => _owner; private set => _owner = value; }
        public AccessoryData Data { get => _accessoryData; set => _accessoryData = value; }

        // Start is called before the first frame update
        void Awake()
        {
            Owner = transform.root.gameObject;
        }

        public virtual void PlayEffect() { }

        public virtual void OnSetToWinPosition() { }

        public virtual void StopEffect() { }

    }
}