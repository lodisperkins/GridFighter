using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    [RequireComponent(typeof(GridPhysicsBehaviour))]
    public class ColliderBehaviour : SimulationBehaviour
    {
        [SerializeField] private CustomEventSystem.Event _onHitObject;
        [SerializeField] private GridCollider _entityCollider;
        [SerializeField] protected bool _debuggingEnabled;
        //---
        protected Dictionary<GameObject, Fixed32> Collisions;
        protected CustomEventSystem.GameEventListener ReturnToPoolListener;
        protected float _lastHitFrame;
        protected CollisionEvent _onHit;

        private GridPhysicsBehaviour _gridPhysics;
        private EntityData _spawner;

        public LayerMask LayersToIgnore { get => EntityCollider.LayersToIgnore; set => EntityCollider.LayersToIgnore = value; }
        public string[] TagsToIgnore { get => EntityCollider.TagsToIgnore; set => EntityCollider.TagsToIgnore = value; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; private set => _gridPhysics = value; }
        public EntityData Spawner { get => _spawner; set { _spawner = value; } }
        public GridCollider EntityCollider { get => _entityCollider; private set => _entityCollider = value; }

        protected override void Awake()
        {
            base.Awake();
            ReturnToPoolListener = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            ReturnToPoolListener.Init(ObjectPoolBehaviour.Instance.OnReturnToPool, gameObject);
            Collisions = new Dictionary<GameObject, Fixed32>();
            GridPhysics = GetComponent<GridPhysicsBehaviour>();

            _entityCollider.Init(Entity.Data, GridPhysics);

            EntityCollider.OnCollisionEnter += RaiseHitEvents;
            EntityCollider.OnOverlapEnter += RaiseHitEvents;
        }

        protected virtual void Start()
        {
        }

        private void OnEnable()
        {
            Collisions.Clear();
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1">The collider that will have its values copied</param>
        /// <param name="collider2">The collider that will have its values overwritten</param>
        public static void Copy(ColliderBehaviour collider1, ColliderBehaviour collider2)
        {
            collider2._lastHitFrame = collider1._lastHitFrame;
            collider2.LayersToIgnore = collider1.LayersToIgnore;
            collider2.Collisions = collider1.Collisions;
        }

        public virtual void AddCollisionEvent(CollisionEvent collisionEvent)
        {
            _onHit += collisionEvent;
        }

        public virtual void RemoveCollisionEvent(CollisionEvent collisionEvent)
        {
            _onHit -= collisionEvent;
        }

        public void ClearAllCollisionEvents()
        {
            _onHit = null;
        }

        public override void Serialize(BinaryWriter bw)
        {
            
        }

        private void OnDrawGizmos()
        {
            if (!_debuggingEnabled) return;

            Vector3 size = new Vector3(_entityCollider.Width, _entityCollider.Height, 1);
            Vector3 offset = new Vector3(1.5f * _entityCollider.PanelYOffset, _entityCollider.WorldYPosition, 1.05f * _entityCollider.PanelXOffset);

            Gizmos.DrawCube(gameObject.transform.position + offset, size);
        }

        public override void Deserialize(BinaryReader br)
        {
        }
        public virtual void InitCollider(Fixed32 width, Fixed32 height, EntityData owner, GridPhysicsBehaviour ownerPhysicsComponent = null)
        {
            _entityCollider.Init(width, height, owner, ownerPhysicsComponent);
        }

        private void RaiseHitEvents(Collision collision)
        {
            _onHitObject?.Raise(gameObject);
            _onHit?.Invoke(collision);
        }
    }
}
