using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
    public class CollisionChannel : ScriptableObject
    {

        public string name;
        public bool collisionEnabled;
        private void Init(string tag, bool collisionVal)
        {
            name = tag;
            collisionEnabled = collisionVal;
        }
        public static GridGame.VariableScripts.CollisionChannel CreateInstance(string tag, bool collisionVal)
        {
            var data = CreateInstance<GridGame.VariableScripts.CollisionChannel>();
            data.Init(tag, collisionVal);
            return data;
        }
    }
}


