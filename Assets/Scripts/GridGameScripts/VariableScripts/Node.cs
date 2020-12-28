using System.Collections;
using System.Collections.Generic;
using GridGame.GamePlay.GridScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GridGame.VariableScripts
{
	[CreateAssetMenu(menuName = "AI/Node")]
	public class Node : ScriptableObject
	{
		public Node parent;
		[SerializeField]
		private Node _childLeft;
		[SerializeField]
		private Node _childRight;
		private bool _condition;
		public string conditionName;
		public List<string> actionNames;
		public bool ConditionMet
		{
			get { return _condition; }
			set { _condition = value; }
		}

		
		public Node ChildLeft
		{
			get { return _childLeft; }
			set
			{
				_childLeft = value;
				_childLeft.parent = this;
			}
		}

		public bool HasChildren()
		{
			return _childLeft != null && _childRight != null;
		}
		public Node ChildRight
		{
			get { return _childRight; }
			set
			{
				_childRight = value;
				_childRight.parent = this;
			}
		}
		public Node(Node parent = null, Node childLeft = null, Node childRight = null)
		{
			this.parent = parent;
			_childLeft = childLeft;
			_childRight = childRight; 
		}
	}
}

