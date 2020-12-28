using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
	[CreateAssetMenu(menuName = "Variables/ObjectList")]
	public class GameObjectList : ScriptableObject
	{

		[SerializeField] private List<GameObject> objects;

		public List<GameObject> Objects
		{
			get { return objects; }
		}

		public void Init(List<GameObject> newObjects)
		{
			objects = new List<GameObject>();
			foreach (GameObject item in newObjects)
			{
				objects.Add(item);
			}
		}
		public void Init()
		{
			objects = new List<GameObject>();
		}
		//creates an instance of this scriptable object
		public static GameObjectList CreateInstance(List<GameObject> newObjects)
		{
			var data = ScriptableObject.CreateInstance<GameObjectList>();
			data.Init(newObjects);
			return data;
		}

		public void Clear()
        {
			objects.Clear();
        }

		//Adds a panel to the list
		public void Add(GameObject item)
		{
			objects.Add(item);
		}

		public GameObject this[int index]
		{
			get { return objects[index]; }
		}
		public bool RemoveItem(GameObject item)
		{
			if (objects.Remove(item))
			{
				return true;
			}
			return false;
		}

		public bool FindItem(GameObject item)
        {

			foreach(var gameObject in objects)
            {
				if (gameObject == item)
                {
					return true;
                }

            }

			return false;
        }

		public IEnumerator GetEnumerator()
		{
			return objects.GetEnumerator();
		}
	}
}
