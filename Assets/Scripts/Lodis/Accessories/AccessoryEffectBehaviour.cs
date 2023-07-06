using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Accessories
{
    public class AccessoryEffectBehaviour : MonoBehaviour
    {
        private GameObject _owner;

        public GameObject Owner { get => _owner; private set => _owner = value; }

        // Start is called before the first frame update
        void Awake()
        {
            Owner = transform.root.gameObject;
        }

        public virtual void PlayEffect() { }

        public virtual void StopEffect() { }

    }
}