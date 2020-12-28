using System.Collections;
using System.Collections.Generic;
using GridGame.GamePlay.GridScripts;
using UnityEngine;
using GridGame.VariableScripts;

namespace GridGame.GamePlay.AI
{
	public class BinaryTreeBehaviour : MonoBehaviour
    {
    
    	public BinaryTree Decisions;
        public int nodeCount;
    	// Use this for initialization
    	
    
    	
        public void TraverseTree()
        {
	        Decisions.currentNode = Decisions.nodes[0];
	        for (int i = 0; i < nodeCount;)
	        {
		        foreach (var actionName in Decisions.currentNode.actionNames)
		        {
			        if (actionName != "")
                    {
                        SendMessage(actionName);
                    }
		        }
		        
		        if (Decisions.currentNode.HasChildren())
		        {
			        if (Decisions.currentNode.ConditionMet)
			        {
				        Decisions.currentNode = Decisions.currentNode.ChildRight;
				        i++;
			        }
			        else 
			        {
				        Decisions.currentNode = Decisions.currentNode.ChildLeft;
				        i++;
			        }
		        }
		        else
		        {
			        break;
		        }
	        }
        }
    	// Update is called once per frame
    	void Update () {
    		
    		TraverseTree();
    	}
    }

}

