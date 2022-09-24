using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lodis.Utility
{
    public class ObjectPoolBehaviour : MonoBehaviour
    {
        private Dictionary<string, Queue<GameObject>> _objectPool = new Dictionary<string, Queue<GameObject>>();
        private static ObjectPoolBehaviour _instance;
        private GridGame.Event _onReturnToPool;

        /// <summary>
        /// The only static instance of the object pool
        /// </summary>
        public static ObjectPoolBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(ObjectPoolBehaviour)) as ObjectPoolBehaviour;

                if (!_instance)
                {
                    GameObject pbjectPool = new GameObject("ObjectPool");
                    _instance = pbjectPool.AddComponent<ObjectPoolBehaviour>();
                }

                return _instance;
            }
        }

        public GridGame.Event OnReturnToPool { get => _onReturnToPool; private set => _onReturnToPool = value; }

        private void Awake()
        {
            OnReturnToPool = Resources.Load<GridGame.Event>("Events/OnReturnToPool");
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="gameObject">A reference to the object</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(gameObject);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="name">The name of the object to search for</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return null;
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="gameObject">A reference to the object</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject, Vector3 position, Quaternion rotation)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.transform.SetPositionAndRotation(position, rotation);
                objectInstance.SetActive(true);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(gameObject, position, rotation);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="name">The name of the object to search for</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <param name="createNew">Whether or not to make a new object if one can't be found</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name, Vector3 position,  Quaternion rotation, bool createNew = false)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                objectInstance.transform.SetPositionAndRotation(position, rotation);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else if (createNew)
            {
                GameObject newObject = new GameObject(name);
                
                newObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                return newObject;
            }

            return null;
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="gameObject">A reference to the object</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject, Transform parent, bool resetPosition = false)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                objectInstance.transform.parent = parent;

                if (resetPosition)
                    objectInstance.transform.localPosition = Vector3.zero;

                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(gameObject, parent, resetPosition);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="name">The name of the object to search for</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <param name="createNew">Whether or not to make a new object if one can't be found</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name, Transform parent, bool resetPosition = false, bool createNew = false)
        {
            //If an object of this type has a queue in the dictionary...
            if (_objectPool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                objectInstance.transform.parent = parent;

                if (resetPosition)
                    objectInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else if (createNew)
            {
                GameObject newObject = new GameObject(name);
                newObject.transform.parent = parent;

                if (resetPosition)
                    newObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                return newObject;
            }

            return null;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <returns>The newly instantiated prefab</returns>
        private GameObject CreateNewObject(GameObject gameObject)
        {
            GameObject newObject = Instantiate(gameObject);
            newObject.name = gameObject.name;
            return newObject;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <returns>The newly instantiated prefab</returns>
        private GameObject CreateNewObject(GameObject gameObject, Vector3 position, Quaternion rotation)
        {
            GameObject newObject = Instantiate(gameObject, position, rotation);
            newObject.name = gameObject.name;
            return newObject;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <returns>The newly instantiated prefab</returns>
        private GameObject CreateNewObject(GameObject gameObject, Transform parent, bool resetPosition = false)
        {
            GameObject newObject = Instantiate(gameObject, parent);


            if (resetPosition)
                newObject.transform.localPosition = Vector3.zero;

            newObject.name = gameObject.name;
            return newObject;
        }

        /// <summary>
        /// Makes the game object inactive in the seen and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        public void ReturnGameObject(GameObject objectInstance)
        {
            if (!objectInstance)
                return;

            Queue<GameObject> queue;
            //If the object has a queue in the dictionary already...
            if (_objectPool.TryGetValue(objectInstance.name, out queue) && !queue.Contains(objectInstance))
            {
                //...add the object back into the queue
                queue.Enqueue(objectInstance);
            }
            else if (queue?.Contains(objectInstance) == true)
            {
                return;
            }    
            //Otherwise...
            else
            {
                //...add the object to a new queue
                Queue<GameObject> newObjectQueue = new Queue<GameObject>();
                newObjectQueue.Enqueue(objectInstance);
                _objectPool.Add(objectInstance.name, newObjectQueue);
            }

            //Disable the object in the scene
            objectInstance.SetActive(false);
            OnReturnToPool?.Raise(objectInstance);
        }

        /// <summary>
        /// Makes the game object inactive in the seen and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        /// <param name="time">The amount of time in seconds to wait before returning the object</param>
        public void ReturnGameObject(GameObject objectInstance, float time)
        {
            RoutineBehaviour.Instance.StartNewTimedAction(args => ReturnGameObject(objectInstance), TimedActionCountType.SCALEDTIME, time);
        }
    }
}
