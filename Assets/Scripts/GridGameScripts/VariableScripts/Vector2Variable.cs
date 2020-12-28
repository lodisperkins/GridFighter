using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
    [CreateAssetMenu(menuName = "Variables/Vector2")]
    public class Vector2Variable : ScriptableObject
    {

        [SerializeField]
        Vector2 val;
        public Vector2 Val
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
            }
        }
        public float X
        {
            get
            {
                return val.x;
            }
            set
            {
                val.x = value;
            }
        }
        public float Y
        {
            get
            {
                return val.y;
            }
            set
            {
                val.y = value;
            }
        }

    }
}

