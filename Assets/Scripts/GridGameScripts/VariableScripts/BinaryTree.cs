using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GridGame.VariableScripts
{
    [CreateAssetMenu(menuName = "AI/BinaryTree")]
    public class BinaryTree : ScriptableObject
    {
        public List<Node> nodes;
        public Node root;
        public Node currentNode;
        //The amount of time it takes for the tree to travle to the next node
        public float decisionDelay;
        //Sets the condition for the current node in the tree to be either true or false
        public void SetCondition(string conditionName, bool value)
        {
            foreach (Node node in nodes)
            {
                if (node.conditionName == conditionName)
                {
                    node.ConditionMet = value;
                }
            }
        }
        
    }
}
