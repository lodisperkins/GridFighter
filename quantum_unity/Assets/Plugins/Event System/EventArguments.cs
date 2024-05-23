using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomEventSystem
{
    [CreateAssetMenu(menuName = "Event Arguments")]
    public class EventArguments : ScriptableObject
    {
        public Object[] UnityObjectArgs;
        public string[] StringArgs;
        public float[] FloatArgs;
        public int[] IntArgs;
        public bool[] BoolArgs;
    }
}