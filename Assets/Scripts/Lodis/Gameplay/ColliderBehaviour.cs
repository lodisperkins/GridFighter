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
        [SerializeField] protected bool _shouldDrawCollider = true;
        [SerializeField] private bool _isHurtBox;
        [SerializeField] private bool debuggingEnabled;

        //---
        protected Dictionary<int, Fixed32> Collisions;
        protected CustomEventSystem.GameEventListener ReturnToPoolListener;
        protected float _lastHitFrame;
        private CollisionEvent onHit;
        protected CollisionEvent _onHitStay;
        protected CollisionEvent _onOverlap;
        protected CollisionEvent _onOverlapStay;
        protected CollisionEvent _onOpponentHit;
        private CollisionGroupBehaviour groupManager;
        private int _collisionCount;

        private GridPhysicsBehaviour _gridPhysics;
        private EntityData _spawner;
        private GameObject _visualCube;
        private bool _createdCube;

        public LayerMask LayersToIgnore { get => EntityCollider.LayersToIgnore; set => EntityCollider.LayersToIgnore = value; }
        public string[] TagsToIgnore { get => EntityCollider.TagsToIgnore; set => EntityCollider.TagsToIgnore = value; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; private set => _gridPhysics = value; }
        public EntityData Spawner { get => _spawner; set { _spawner = value; } }
        public GridCollider EntityCollider { get => _entityCollider; set => _entityCollider = value; }
        public CustomEventSystem.Event OnHitObject { get => _onHitObject; set => _onHitObject = value; }
        public bool DebuggingEnabled { get => debuggingEnabled; set => debuggingEnabled = value; }
        public CollisionGroupBehaviour GroupManager { get => groupManager; set => groupManager = value; }
        public bool CollisionEnabled { get => _entityCollider.CollisionEnabled; set => _entityCollider.CollisionEnabled = value; }
        public CollisionEvent OnHit { get => onHit; set => onHit = value; }

        public override void Init()
        {
            base.Init();

            ReturnToPoolListener = gameObject?.AddComponent<CustomEventSystem.GameEventListener>();
            ReturnToPoolListener.Init(ObjectPoolBehaviour.Instance.OnReturnToPool, gameObject);
        }

        protected override void Awake()
        {
            base.Awake();
            Collisions = new Dictionary<int, Fixed32>();
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
            EntityCollider.OnOverlapStay += c => _onOverlapStay?.Invoke(c);
            EntityCollider.OnCollisionStay += c => _onHitStay?.Invoke(c);
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
            OnHit += collisionEvent;
        }

        public virtual void AddCollisionStayEvent(CollisionEvent collisionEvent)
        {
            _onHitStay += collisionEvent;
        }

        public void AddOnOverlapEvent(UnityAction action)
        {
            _onOverlapBegin.AddListener(action);
        }

        public void AddOnOverlapStayEvent(CollisionEvent action)
        {
            _onOverlapStay += action;
        }

        public void AddOpponentCollisionEvent(CollisionEvent collisionEvent)
        {
            _onOpponentHit += collisionEvent;
        }

        public virtual void RemoveCollisionEvent(CollisionEvent collisionEvent)
        {
            OnHit -= collisionEvent;
        }

        public void ClearAllCollisionEvents()
        {
            OnHit = null;
        }

        public override void Serialize(BinaryWriter bw)
        {
            return;
            //Tell the actual colliding object to serialize.
            _entityCollider.Serialize(bw);

            //Save the number of collisions to know how many to add again during deserialization.
            bw.Write(Collisions.Count);


            //Save the keys and values of each collision in case the objects collided with changes.
            foreach (var entry in Collisions)
            {
                bw.Write(entry.Key);
                entry.Value.Serialize(bw);
            }
        }

        public override void Deserialize(BinaryReader br)
        {
            return;
            //Tell the actual colliding object to deserialize.
            _entityCollider.Deserialize(br);

            //Only try to add collisions if there were any serialized in the first place.
            int count = br.ReadInt32();

            if (count <= 0)
            {
                return;
            }


            //Load the old collisions.
            Collisions.Clear();

            for (int i = 0; i < count; i++)
            {
                int hashKey = br.ReadInt32();

                Fixed32 timeVal = new Fixed32();
                timeVal.Deserialize(br);

                Collisions.Add(hashKey, timeVal);
            }
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

            OnHit?.Invoke(collision);
        }

        public override void End()
        {
            base.End(); 
            
            if (_visualCube != null)
            {
                _visualCube.SetActive(false);
            }
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            if (!_shouldDrawCollider)
                return;

            //Only draw the collider if its been enabled by the event fired off from the match manager.
            if (MatchManagerBehaviour.Instance.CollidersEnabled)
            {
                //Only instantiate the cube once.
                if (!_createdCube)
                {
                    _visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                    //Get the material based on whether this is a hurtbox or hitbox.
                    Material mat = _isHurtBox ? MatchManagerBehaviour.Instance.HurtBoxMaterial : MatchManagerBehaviour.Instance.HitBoxMaterial;
                    _visualCube.GetComponent<MeshRenderer>().material = mat;

                    _visualCube.name = gameObject.name + " Visual Collider";
                    _createdCube = true;
                }

                _visualCube.SetActive(true);

                //Draw the rest of the owl.
                float width = _entityCollider.IsAWall ? 0.1f : _entityCollider.Width;
                Vector3 size = new Vector3(width, _entityCollider.Height, 1);
                Fixed32 panelZSpacing = GridBehaviour.Instance.FixedPanelSpacingX + GridBehaviour.Instance.FixedPanelScale.X;
                Vector3 offset = new Vector3(panelZSpacing * _entityCollider.PanelXOffset, _entityCollider.WorldYPosition, 2 * _entityCollider.PanelYOffset);

                Transform rootTransform = EntityCollider.Entity ? _entityCollider.Entity.transform : transform;

                _visualCube.transform.position = rootTransform.position + offset;
                _visualCube.transform.localScale = size;
            }
            else if (_visualCube != null)
            {
                _visualCube.SetActive(false);
            }
        }
    }
}
