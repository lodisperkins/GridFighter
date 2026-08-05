using FixedPoints;
using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;


namespace Lodis.Utility
{
    public class ObjectPoolBehaviour : MonoBehaviour
    {
        private Dictionary<string, Queue<GameObject>> _anyObjectPool = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, Queue<EntityDataBehaviour>> _anyEntityObjectPool = new Dictionary<string, Queue<EntityDataBehaviour>>();
        private Dictionary<string, Queue<GameObject>> _leftObjectPool = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, Queue<EntityDataBehaviour>> _leftEntityObjectPool = new Dictionary<string, Queue<EntityDataBehaviour>>();
        private Dictionary<string, Queue<GameObject>> _rightObjectPool = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, Queue<EntityDataBehaviour>> _rightEntityObjectPool = new Dictionary<string, Queue<EntityDataBehaviour>>();
        private static ObjectPoolBehaviour _instance;
        private CustomEventSystem.Event _onReturnToPool;

        public delegate void OnEntityAwake(EntityDataBehaviour entity);
        public delegate void OnGameObjectAwake(GameObject entity);

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

        public CustomEventSystem.Event OnReturnToPool { get => _onReturnToPool; private set => _onReturnToPool = value; }

        private void Awake()
        {
            OnReturnToPool = Resources.Load<CustomEventSystem.Event>("Events/OnReturnToPool");
        }

        private Dictionary<string, Queue<GameObject>> GetGameObjectPool(GridAlignment alignment)
        {
            switch (alignment)
            {
                case GridAlignment.LEFT:
                    return _leftObjectPool;
                case GridAlignment.RIGHT:
                    return _rightObjectPool;
                default:
                    return _anyObjectPool;
            }
        }

        private Dictionary<string, Queue<EntityDataBehaviour>> GetEntityPool(GridAlignment alignment)
        {
            switch (alignment)
            {
                case GridAlignment.LEFT:
                    return _leftEntityObjectPool;
                case GridAlignment.RIGHT:
                    return _rightEntityObjectPool;
                default:
                    return _anyEntityObjectPool;
            }
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="gameObject">A reference to the object</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject, GridAlignment alignment = GridAlignment.ANY)
        {
#if UNITY_EDITOR
            if (gameObject.TryGetComponent<EntityDataBehaviour>(out _))
            {
                Debug.LogError("Tried to get an object that has an entity data behaviour from the normal pool instead of the Entity pool.");
            }
#endif

            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
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
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name, GridAlignment alignment = GridAlignment.ANY)
        {
            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
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
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject, Vector3 position, Quaternion rotation, GridAlignment alignment = GridAlignment.ANY)
        {
#if UNITY_EDITOR
            if (gameObject.TryGetComponent<EntityDataBehaviour>(out _))
            {
                Debug.LogError("Tried to get an object that has an entity data behaviour from the normal pool instead of the Entity pool.");
            }
#endif

            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();

                if (!objectInstance)
                    return null;

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
        /// <param name="entity">A reference to the object</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <param name="awakeEvent">Event to invoke when the object is retrieved</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public EntityDataBehaviour GetObject(EntityDataBehaviour entity, FVector3 position, FQuaternion rotation, OnEntityAwake awakeEvent = null, GridAlignment alignment = GridAlignment.ANY)
        {
            var pool = GetEntityPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(entity.Data.Name, out Queue<EntityDataBehaviour> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                EntityDataBehaviour objectInstance = objectQueue.Dequeue();

                if ((object)objectInstance == null)
                    return null;

                objectInstance.FixedTransform.SetPositionAndRotation(position, rotation);
                awakeEvent?.Invoke(objectInstance);
                objectInstance.gameObject.SetActive(true);
                objectInstance.UpdateUnityTransform(GridGame.FixedTimeStep);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(entity, position, rotation, awakeEvent);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="entity">A reference to the object</param>
        /// <param name="parent">The parent transform to attach to</param>
        /// <param name="awakeEvent">Event to invoke when the object is retrieved</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public EntityDataBehaviour GetObject(EntityDataBehaviour entity, FTransform parent, OnEntityAwake awakeEvent = null, GridAlignment alignment = GridAlignment.ANY)
        {
            var pool = GetEntityPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(entity.Data.Name, out Queue<EntityDataBehaviour> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                EntityDataBehaviour objectInstance = objectQueue.Dequeue();

                if (!objectInstance)
                    return null;

                objectInstance.FixedTransform.Parent = parent;
                objectInstance.FixedTransform.LocalPosition = FVector3.Zero;
                objectInstance.FixedTransform.LocalRotation = FQuaternion.Identity;
                awakeEvent?.Invoke(objectInstance);
                objectInstance.AddToGame();
                objectInstance.UpdateUnityTransform(GridGame.FixedTimeStep);
                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(entity, parent);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="name">The name of the object to search for</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <param name="createNew">Whether or not to make a new object if one can't be found</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name, Vector3 position, Quaternion rotation, bool createNew = false, GridAlignment alignment = GridAlignment.ANY)
        {
            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
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
        /// <param name="onAwake">Event to invoke when the object is retrieved</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Creates a new object otherwise</returns>
        public GameObject GetObject(GameObject gameObject, Transform parent, bool resetPosition = false, OnGameObjectAwake onAwake = null, GridAlignment alignment = GridAlignment.ANY)
        {
#if UNITY_EDITOR
            if (gameObject.TryGetComponent<EntityDataBehaviour>(out _))
            {
                Debug.LogError("Tried to get an object that has an entity data behaviour from the normal pool instead of the Entity pool.");
            }
#endif

            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(gameObject.name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                GameObject objectInstance = objectQueue.Dequeue();
                onAwake?.Invoke(objectInstance);
                objectInstance.SetActive(true);
                objectInstance.transform.parent = parent;

                if (resetPosition)
                    objectInstance.transform.localPosition = Vector3.zero;

                return objectInstance;
            }
            //...otherwise create a new instance of the object
            else
                return CreateNewObject(gameObject, parent, resetPosition, onAwake);
        }

        /// <summary>
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="name">The name of the object to search for</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <param name="createNew">Whether or not to make a new object if one can't be found</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public GameObject GetObject(string name, Transform parent, bool resetPosition = false, bool createNew = false, GridAlignment alignment = GridAlignment.ANY)
        {
            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
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
        /// Gets the first instance of the object found in the pool
        /// </summary>
        /// <param name="objectInstance">The output object instance</param>
        /// <param name="name">The name of the object to search for</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <param name="createNew">Whether or not to make a new object if one can't be found</param>
        /// <param name="alignment">The alignment pool to pull from</param>
        /// <returns>The object instance if it is in the pool. Returns null otherwise</returns>
        public bool GetObject(out GameObject objectInstance, string name, Transform parent, bool resetPosition = false, bool createNew = false, GridAlignment alignment = GridAlignment.ANY)
        {
            objectInstance = null;

            var pool = GetGameObjectPool(alignment);

            //If an object of this type has a queue in the dictionary...
            if (pool.TryGetValue(name, out Queue<GameObject> objectQueue) && objectQueue.Count > 0)
            {
                //...set the first instance found active and return the object
                objectInstance = objectQueue.Dequeue();
                objectInstance.SetActive(true);
                objectInstance.transform.parent = parent;

                if (resetPosition)
                    objectInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                return true;
            }
            //...otherwise create a new instance of the object
            else if (createNew)
            {
                objectInstance = new GameObject(name);
                objectInstance.transform.parent = parent;

                if (resetPosition)
                    objectInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                return false;
            }

            return false;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <returns>The newly instantiated prefab</returns>
        private GameObject CreateNewObject(GameObject gameObject, OnGameObjectAwake onAwake = null)
        {
            GameObject newObject = Instantiate(gameObject);
            onAwake?.Invoke(newObject);
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
        private GameObject CreateNewObject(GameObject gameObject, Vector3 position, Quaternion rotation, OnGameObjectAwake onAwake = null)
        {
            GameObject newObject = Instantiate(gameObject, position, rotation);
            onAwake?.Invoke(newObject);
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
        private EntityDataBehaviour CreateNewObject(EntityDataBehaviour entity, FVector3 position, FQuaternion rotation, OnEntityAwake onAwake = null)
        {
            EntityDataBehaviour newObject = Instantiate(entity, (Vector3)position, (Quaternion)rotation);

            newObject.FixedTransform.SetPositionAndRotation(position, rotation);
            newObject.name = entity.Data.Name;
            newObject.Data.Name = entity.Data.Name;
            onAwake?.Invoke(newObject);

            return newObject;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <param name="position">The new position of the object</param>
        /// <param name="rotation">The new rotation of the object</param>
        /// <returns>The newly instantiated prefab</returns>
        private EntityDataBehaviour CreateNewObject(EntityDataBehaviour entity, FTransform parent)
        {
            EntityDataBehaviour newObject = Instantiate(entity, (Vector3)parent.WorldPosition, (Quaternion)parent.WorldRotation);

            newObject.FixedTransform.Parent = parent;
            newObject.FixedTransform.LocalPosition = FVector3.Zero;
            newObject.FixedTransform.LocalRotation = FQuaternion.Identity;
            newObject.name = entity.Data.Name;
            newObject.Data.Name = entity.Data.Name;
            return newObject;
        }

        /// <summary>
        /// Instantiates a new instance of the object and changes its name to match the prefab
        /// </summary>
        /// <param name="gameObject">A reference to the prefab to instantiate</param>
        /// <param name="parent">The new parent of the object</param>
        /// <param name="resetPosition">Whether or not to make this game object match the position and rotation of its parent</param>
        /// <returns>The newly instantiated prefab</returns>
        private GameObject CreateNewObject(GameObject gameObject, Transform parent, bool resetPosition = false, OnGameObjectAwake onAwake = null)
        {
            GameObject newObject = Instantiate(gameObject, parent);
            onAwake?.Invoke(newObject);

            if (resetPosition)
                newObject.transform.localPosition = Vector3.zero;

            newObject.name = gameObject.name;
            return newObject;
        }

        /// <summary>
        /// Makes the game object inactive in the scene and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        /// <param name="alignment">The alignment pool to return to</param>
        public void ReturnGameObject(GameObject objectInstance, GridAlignment alignment = GridAlignment.ANY)
        {
            if (!objectInstance)
                return;

#if UNITY_EDITOR
            if (objectInstance.TryGetComponent<EntityDataBehaviour>(out _))
            {
                Debug.LogError("Tried to return an object that has an entity data behaviour to the normal pool instead of the Entity pool.");
            }
#endif

            var pool = GetGameObjectPool(alignment);

            Queue<GameObject> queue;
            //If the object has a queue in the dictionary already...
            if (pool.TryGetValue(objectInstance.name, out queue) && !queue.Contains(objectInstance))
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
                pool.Add(objectInstance.name, newObjectQueue);
            }

            //Disable the object in the scene
            objectInstance.SetActive(false);
            OnReturnToPool?.Raise(objectInstance);

            for (int i = 0; i < objectInstance.transform.childCount; i++)
            {
                OnReturnToPool?.Raise(objectInstance.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Makes the game object inactive in the scene and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        /// <param name="time">The amount of time in seconds to wait before returning the object</param>
        /// <param name="alignment">The alignment pool to return to</param>
        public void ReturnGameObject(GameObject objectInstance, float time, GridAlignment alignment = GridAlignment.ANY)
        {
#if UNITY_EDITOR
            if (objectInstance.TryGetComponent<EntityDataBehaviour>(out _))
            {
                Debug.LogError("Tried to return an object that has an entity data behaviour to the normal pool instead of the Entity pool. Object was " + objectInstance.name);
            }
#endif
            RoutineBehaviour.Instance.StartNewTimedAction(args => ReturnGameObject(objectInstance, alignment), TimedActionCountType.SCALEDTIME, time);
        }

        /// <summary>
        /// Makes the game object inactive in the scene and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        /// <param name="loseParent">Whether to clear the parent transform</param>
        /// <param name="alignment">The alignment pool to return to</param>
        public void ReturnGameObject(EntityDataBehaviour objectInstance, bool loseParent = true, GridAlignment alignment = GridAlignment.ANY)
        {
            if (!objectInstance)
                return;

            var pool = GetEntityPool(alignment);

            //If the object has a queue in the dictionary already...
            if (pool.TryGetValue(objectInstance.Data.Name, out Queue<EntityDataBehaviour> queue) && !queue.Contains(objectInstance))
            {
                //...add the object back into the queue
                queue.Enqueue(objectInstance);
            }
            else if (queue?.Contains(objectInstance) == true)
            {
                Debug.LogWarning("Tried to return an object that was already in the queue. Object was " + objectInstance.Data.Name);
                return;
            }
            //Otherwise...
            else
            {
                //...add the object to a new queue
                Queue<EntityDataBehaviour> newObjectQueue = new Queue<EntityDataBehaviour>();
                newObjectQueue.Enqueue(objectInstance);
                pool.Add(objectInstance.Data.Name, newObjectQueue);
            }

            //Disable the object in the scene
            objectInstance.RemoveFromGame();
            OnReturnToPool?.Raise(objectInstance.gameObject);

            for (int i = 0; i < objectInstance.transform.childCount; i++)
            {
                OnReturnToPool?.Raise(objectInstance.transform.GetChild(i).gameObject);
            }

            if (loseParent)
            {
                objectInstance.FixedTransform.Parent = null;
            }
        }

        /// <summary>
        /// Makes the game object inactive in the scene and adds it back to the pool
        /// </summary>
        /// <param name="objectInstance">The instance of the game object to return to the pool</param>
        /// <param name="time">The amount of time in seconds to wait before returning the object</param>
        /// <param name="alignment">The alignment pool to return to</param>
        public void ReturnGameObject(EntityDataBehaviour objectInstance, Fixed32 time, GridAlignment alignment = GridAlignment.ANY)
        {
            FixedPointTimer.StartNewTimedAction(() => ReturnGameObject(objectInstance, false, alignment), time);
        }
    }
}
