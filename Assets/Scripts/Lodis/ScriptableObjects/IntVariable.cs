using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/int")]
    public class IntVariable : ScriptableObject
    {
        [SerializeField]
        private float _val;
        public float Value
        {
            get
            {
                return _val;
            }
            set
            {
                _val = value;
            }
        }

        public void Init(float value)
        {
            _val = value;
        }

        public static FloatVariable CreateInstance(float value)
        {
            var data = CreateInstance<FloatVariable>();
            data.Init(value);
            return data;
        }
    }
}


