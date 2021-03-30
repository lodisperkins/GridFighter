using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.VariableScripts
{
    [CreateAssetMenu(menuName = "Variables/Float")]
    public class FloatVariable : ScriptableObject
    {
        [SerializeField]
        private float _val;
        public float Val
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


