using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/bool")]
    public class BoolVariable : ScriptableObject
    {
        [SerializeField]
        private bool _val;
        public bool Value
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

        public void Init(bool value)
        {
            _val = value;
        }

        public static BoolVariable CreateInstance(bool value)
        {
            var data = CreateInstance<BoolVariable>();
            data.Init(value);
            return data;
        }
    }
}


