using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lodis.Gameplay
{
    public class HitColliderBehaviour : MonoBehaviour
    {
        [Tooltip("The amount of damage this attack will do.")]
        [SerializeField]
        private float _damage;
        [Tooltip("How far back this attack will knock an object back.")]
        [SerializeField]
        private float _knockBackScale;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        [SerializeField]
        private float _hitAngle;
        [Tooltip("If true, the hit collider will despawn after the amount of active frames have been surpassed.")]
        [SerializeField]
        private bool _despawnsAfterFrameLimit = false;
        [Tooltip("How many frames the hitbox will be active for.")]
        [SerializeField]
        private float _activeFrames;
        [Tooltip("If true, the hit collider will damage objects that enter it multiple times.")]
        [SerializeField]
        private bool _isMultiHit;
        [SerializeField]
        private bool _destroyOnHit;
        private float _currentFramesActive;
        private GameObject _owner;
        private List<GameObject> _collisions;

        public HitColliderBehaviour(float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner, destroyOnHit, isMultiHit);
        }

        private void Awake()
        {
            _collisions = new List<GameObject>();
        }

        public GameObject Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1"></param>
        /// <param name="collider2"></param>
        public static void Copy(HitColliderBehaviour collider1, HitColliderBehaviour collider2)
        {
            collider2.Init(collider1._damage, collider1._knockBackScale, collider1._hitAngle, collider1._despawnsAfterFrameLimit, collider1._activeFrames, collider1.Owner, collider1._destroyOnHit, collider1._isMultiHit);
        }

        /// <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="damage">The amount of damage this attack will do</param>
        /// <param name="knockBackScale">How far back this attack will knock an object back</param>
        /// <param name="hitAngle">The angle (in radians) that the object in knock back will be launched at</param>
        /// <param name="activeFrames">If true, the hit collider will damage objects that enter it multiple times</param>
        public void Init(float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            _damage = damage;
            _knockBackScale = knockBackScale;
            _hitAngle = hitAngle;
            _despawnsAfterFrameLimit = despawnAfterFrameLimit;
            _activeFrames = activeFrames;
            _owner = owner;
            _destroyOnHit = destroyOnHit;
            _isMultiHit = isMultiHit;
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (_collisions.Contains(other.gameObject) || _isMultiHit || other.gameObject == _owner)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(other.gameObject);

            //Grab whatever health script is attached to this object
            IDamagable damageScript = other.GetComponent<IDamagable>();

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(_damage, _knockBackScale, _hitAngle);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!_isMultiHit || other.gameObject == _owner)
                return;

            //Grab whatever health script is attached to this object. If none return
            IDamagable damageScript = other.GetComponent<IDamagable>();

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(_damage, _knockBackScale, _hitAngle);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (_collisions.Contains(collision.gameObject) || _isMultiHit || collision.gameObject == _owner)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(collision.gameObject);

            //Grab whatever health script is attached to this object
            IDamagable damageScript = collision.gameObject.GetComponent<IDamagable>();

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(_damage, _knockBackScale, _hitAngle);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (_currentFramesActive >= _activeFrames && _despawnsAfterFrameLimit)
                Destroy(gameObject);

            //Increase the the count of active frames
            _currentFramesActive += Time.deltaTime;
        }
    }
}


