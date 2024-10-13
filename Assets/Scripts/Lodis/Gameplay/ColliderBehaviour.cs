using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public class ColliderBehaviour : SimulationBehaviour
    {
        [SerializeField] private CustomEventSystem.Event _onHitObject;
        [SerializeField] private UnityEvent _onOverlapBegin;
        [SerializeField] private UnityEvent _onHitBegin;
        [SerializeField] private GridCollider _entityCollider;
        [SerializeField] private bool debuggingEnabled;
        //---
        protected Dictionary<GameObject, Fixed32> Collisions;
        protected CustomEventSystem.GameEventListener ReturnToPoolListener;
        protected float _lastHitFrame;
        protected CollisionEvent _onHit;
        protected CollisionEvent _onOpponentHit;
        private CollisionGroupBehaviour groupManager;

        private GridPhysicsBehaviour _gridPhysics;
        private EntityData _spawner;

        public LayerMask LayersToIgnore { get => EntityCollider.LayersToIgnore; set => EntityCollider.LayersToIgnore = value; }
        public string[] TagsToIgnore { get => EntityCollider.TagsToIgnore; set => EntityCollider.TagsToIgnore = value; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; private set => _gridPhysics = value; }
        public EntityData Spawner { get => _spawner; set { _spawner = value; } }
        public GridCollider EntityCollider { get => _entityCollider; set => _entityCollider = value; }
        public CustomEventSystem.Event OnHitObject { get => _onHitObject; set => _onHitObject = value; }
        public bool DebuggingEnabled { get => debuggingEnabled; set => debuggingEnabled = value; }
        public CollisionGroupBehaviour GroupManager { get => groupManager; set => groupManager = value; }

        public override void Init()
        {
            base.Init();

            ReturnToPoolListener = gameObject?.AddComponent<CustomEventSystem.GameEventListener>();
            ReturnToPoolListener.Init(ObjectPoolBehaviour.Instance.OnReturnToPool, gameObject);
        }

        protected override void Awake()
        {
            base.Awake();
            Collisions = new Dictionary<GameObject, Fixed32>();
            GridPhysics = Entity.GetComponent<GridPhysicsBehaviour>();

            if (!GridPhysics)
                throw new Exception(Entity.name + " has a collider but is missing a GridPhysicsBehaviour.");

            if (_entityCollider == null)
                _entityCollider = new GridCollider();

            _entityCollider.Init(Entity, GridPhysics);

            EntityCollider.OnCollisionEnter += RaiseHitEvents;
            EntityCollider.OnCollisionEnter += c => _onHitBegin?.Invoke();
            EntityCollider.OnOverlapEnter += RaiseHitEvents;
            EntityCollider.OnOverlapEnter += c => _onOverlapBegin?.Invoke();
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

        public void AddOpponentCollisionEvent(CollisionEvent collisionEvent)
        {
            _onOpponentHit += collisionEvent;
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
            if (!DebuggingEnabled) return;

            float width = _entityCollider.IsAWall ? 0.1f : _entityCollider.Width;
            Vector3 size = new Vector3(width, _entityCollider.Height, 1);
            Vector3 offset = new Vector3(1.5f * _entityCollider.PanelXOffset, _entityCollider.WorldYPosition, 2 * _entityCollider.PanelYOffset);

            Transform rootTransform = EntityCollider.Entity ? _entityCollider.Entity.transform : transform; ;

            Gizmos.DrawCube(rootTransform.position + offset, size);
        }

        public override void Deserialize(BinaryReader br)
        {
        }
        public virtual void InitCollider(Fixed32 width, Fixed32 height, EntityDataBehaviour spawner)
        {
            if (_entityCollider == null)
                _entityCollider = new GridCollider();

            Spawner = spawner;

            _entityCollider.Width = width;
            _entityCollider.Height = height;
        }

        private void RaiseHitEvents(Collision collision)
        {
            if (groupManager == null)
                OnHitObject?.Raise(gameObject);
            else
                OnHitObject?.Raise(groupManager.gameObject);

            if (collision.OtherEntity.UnityObject == BlackBoardBehaviour.Instance.GetOpponentForPlayer(Spawner?.UnityObject))
            {
                _onOpponentHit?.Invoke(collision);
            }

            _onHit?.Invoke(collision);
        }
    }
}
