using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
	[CreateAssetMenu(menuName = "Variables/GridGame.VariableScripts.ObjectList")]
	public class ObjectList : ScriptableObject
	{

		[SerializeField] private List<Object> objects;

		public List<Object> Objects
		{
			get { return objects; }
		}

		public void Init(List<Object> newObjects)
		{
			objects = new List<Object>();
			foreach (Object item in newObjects)
			{
				objects.Add(item);
			}
		}
		public void Init()
		{
			objects = new List<Object>();
		}
		//creates an instance of this scriptable object
		public static GridGame.VariableScripts.ObjectList CreateInstance(List<Object> newObjects)
		{
			var data = ScriptableObject.CreateInstance<GridGame.VariableScripts.ObjectList>();
			data.Init(newObjects);
			return data;
		}

		//Adds a panel to the list
		public void Add(Object item)
		{
			objects.Add(item);
		}

		public void Clear()
		{
			objects.Clear();
		}

		public Object this[int index]
		{
			get { return objects[index]; }
		}
		public bool RemoveItem(Object item)
		{
			if (objects.Remove(item))
			{
				return true;
			}
			return false;
		}
		public IEnumerator GetEnumerator()
		{
			return objects.GetEnumerator();
		}
	}
}


