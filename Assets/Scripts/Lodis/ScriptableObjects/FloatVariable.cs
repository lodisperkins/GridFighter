using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/Float")]
    public class FloatVariable : ScriptableObject
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

        public static FloatVariable operator + (FloatVariable lhs, FloatVariable rhs)
        {
            return CreateInstance(lhs.Value + rhs.Value);
        }

        public static FloatVariable operator - (FloatVariable lhs, FloatVariable rhs)
        {
            return CreateInstance(lhs.Value - rhs.Value);
        }

        public static FloatVariable operator * (FloatVariable lhs, FloatVariable rhs)
        {
            return CreateInstance(lhs.Value * rhs.Value);
        }

        public static FloatVariable operator / (FloatVariable lhs, FloatVariable rhs)
        {
            return CreateInstance(lhs.Value - rhs.Value);
        }

        public static implicit operator float(FloatVariable v)
        {
            return v.Value;
        }
    }
}


