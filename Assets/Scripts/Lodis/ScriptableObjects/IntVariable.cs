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

        public static bool operator > (IntVariable lhs, int rhs)
        {
            return lhs.Value > rhs;
        }

        public static bool operator < (IntVariable lhs, int rhs)
        {
            return lhs.Value < rhs;
        }

        public static bool operator == (IntVariable lhs, int rhs)
        {
            return lhs.Value == rhs;
        }

        public static bool operator != (IntVariable lhs, int rhs)
        {
            return lhs.Value != rhs;
        }

        public static IntVariable operator -- (IntVariable lhs)
        {
            lhs.Value--;
            return lhs;
        }

        public static IntVariable operator ++ (IntVariable lhs)
        {
            lhs.Value++;
            return lhs;
        }

        public static implicit operator string(IntVariable lhs)
        {
            return lhs.Value.ToString();
        }

        public static implicit operator int(IntVariable lhs)
        {
            return lhs.Value;
        }
    }
}


