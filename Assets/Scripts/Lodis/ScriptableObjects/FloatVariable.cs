using System;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/Float")]
    public class FloatVariable : ScriptableObject
    {
        [SerializeField]
        private float _val;
        private Fixed32 _fixedVal;
        
        public float Value
        {
            get
            {
                return _val;
            }
            set
            {
                _val = value;
                _fixedVal = (Fixed32)value;
            }
        }

        public Fixed32 FixedValue
        {
            get
            {
                return _fixedVal;
            }
            set
            {
                _fixedVal = value;
                _val = value;
            }
        }

        public void Init(float value)
        {
            _val = value;
            _fixedVal = value;
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


