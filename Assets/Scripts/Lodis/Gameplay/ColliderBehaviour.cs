using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    
    /// <summary>
    /// Event used when collisions occur. 
    /// Arg[0] = The game object collided with.
    /// Arg[1] = The collision data. Is a collider type when on trigger enter/stay is called,
    /// and is a collision type when on collision enter is called
    /// </summary>
    /// <param name="args"></param>
    public delegate void CollisionEvent(params object[] args);
    public class ColliderBehaviour : MonoBehaviour
    {
        protected Dictionary<GameObject, int> Collisions;
        /// <summary>
        /// Collision event called when this collider hits another. 
        /// First argument is game object it collided with.
        /// </summary>
        public CollisionEvent OnHit;
        protected float _lastHitFrame;
        [Tooltip("The game object spawned this collider.")]
        public GameObject Owner;
        [SerializeField]
        private LayerMask _layersToIgnore;

        public LayerMask LayersToIgnore { get => _layersToIgnore; set => _layersToIgnore = value; }

        private void Awake()
        {
            Collisions = new Dictionary<GameObject, int>();
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1">The collider that will have its values copied</param>
        /// <param name="collider2">The collider that will have its values overwritten</param>
        public static void Copy(ColliderBehaviour collider1, ColliderBehaviour collider2)
        {
            collider2.OnHit = collider1.OnHit;
            collider2._lastHitFrame = collider1._lastHitFrame;
            collider2.Owner = collider1.Owner;
            collider2.LayersToIgnore = collider1.LayersToIgnore;
            collider2.Collisions = collider1.Collisions;
        }

        /// <summary>
        /// Checks if the layer is in the colliders layer mask of 
        /// layers to ignore.
        /// </summary>
        /// <param name="layer">The unity physics collision layer of the game object.</param>
        /// <returns></returns>
        public bool CheckIfLayerShouldBeIgnored(int layer)
        {
            if (LayersToIgnore == 0)
                return false;

            int mask = LayersToIgnore;
            if (mask != (mask | 1 << layer))
                return true;

            return false;
        }


        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (other.gameObject == Owner || Collisions.ContainsKey(other.gameObject))
                return;

            ColliderBehaviour otherCollider = null;
            GameObject otherGameObject = null;

            //If the other object has a rigid body attached...
            if (other.attachedRigidbody)
            {
                //...grab the game object attached to the rigid body and collider script.
                //Hit events will now only be called using the parent object instead of the child collider.
                otherGameObject = other.attachedRigidbody.gameObject;
                otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            }
            else otherGameObject = other.gameObject;

            //If either colliders want to ignore the other's layer return.
            if (CheckIfLayerShouldBeIgnored(otherGameObject.layer) || otherCollider?.CheckIfLayerShouldBeIgnored(gameObject.layer) == true)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);

            //Calculate the normal and invoke hit event
            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;
            OnHit?.Invoke(otherGameObject, otherCollider, collisionDirection);
        }

        private void OnCollisionEnter(Collision collision)
        {

            GameObject other = collision.gameObject;
            //If the object has already been hit or if the collider is multihit return
            if (other.gameObject == Owner || Collisions.ContainsKey(other))
                return;

            ColliderBehaviour otherCollider = null;

            //If the other object has a rigid body attached...
            if (collision.collider.attachedRigidbody)
            {
                //...grab the game object attached to the rigid body and collider script.
                //Hit events will now only be called using the parent object instead of the child collider.
                other = collision.collider.attachedRigidbody.gameObject;
                otherCollider = other.GetComponent<ColliderBehaviour>();
            }
            else other = other.gameObject;

            //If either colliders want to ignore the other's layer return.
            if (CheckIfLayerShouldBeIgnored(other.layer) || otherCollider?.CheckIfLayerShouldBeIgnored(gameObject.layer) == true)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);

            //Calculate the normal and invoke hit event
            Vector3 collisionDirection = (other.transform.position - transform.position).normalized;
            OnHit?.Invoke(other, otherCollider, collisionDirection);
        }
    }
}
