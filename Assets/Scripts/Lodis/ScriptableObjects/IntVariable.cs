using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/int")]
    public class IntVariable : ScriptableObject
    {
        [SerializeField]
        private int _val;
        public int Value
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

        public void Init(int value)
        {
            _val = value;
        }

        public static IntVariable CreateInstance(int value)
        {
            var data = CreateInstance<IntVariable>();
            data.Init(value);
            return data;
        }
    }
}


