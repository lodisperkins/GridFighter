using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Accessory Data")]
    public class AccessoryData : ScriptableObject
    {
        [SerializeField]
        private string _name;
        [SerializeField]
        private GameObject _visual;
        [SerializeField]
        private GameObject _spawnEffect;
        [SerializeField]
        private GameObject _despawnEffect;

        public string Name { get => _name; private set => _name = value; }
        public GameObject Visual { get => _visual; private set => _visual = value; }
        public GameObject SpawnEffect { get => _spawnEffect; set => _spawnEffect = value; }
        public GameObject DespawnEffect { get => _despawnEffect; set => _despawnEffect = value; }
    }
}