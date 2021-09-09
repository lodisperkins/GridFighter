using GridGame.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.GamePlay.BlockScripts
{

    /// <summary>
    /// rework
    /// </summary>
    public class KineticBlockBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {
        private List<Rigidbody> _rigidbodies;
        private List<BulletBehaviour> _bullets;
        private List<Vector3> velocityVals;
        [SerializeField] private HealthBehaviour _blockHealth;
        [SerializeField]
        private BlockBehaviour _blockScript;
        [SerializeField]
        private GameEventListener _eventListener;
        public int bulletCapacity;
        [SerializeField] private int _bulletCapUpgradeVal;
        [SerializeField] private int playerUseAmount;
        private PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField] private Color _displayColor;
        [SerializeField]
        private bool _canBeHeld;
        [SerializeField]
        private GameObject _kineticBombRef;
        private KineticBombBehaviour _currentKineticBomb;
        private List<GameObject> _ownerBullets;
        public BlockBehaviour block
        {
            get
            {
                return _blockScript;
            }
            set
            {
                _blockScript = value;
            }
        }
        public GameObject specialFeature
        {
            get
            {
                return gameObject;
            }
        }

        public string Name
        {
            get
            {
                return gameObject.name;
            }
        }

        public Color displayColor
        {
            get
            {
                return _displayColor;
            }

            set
            {
                _displayColor = value;
            }
        }

        public bool CanBeHeld
        {
            get
            {
                return _canBeHeld;
            }
        }

        public GridPhysicsBehaviour PhysicsBehaviour
        {
            get
            {
                throw new System.NotImplementedException();
            }

            set
            {
                throw new System.NotImplementedException();
            }
        }

        // Use this for initialization
        void Start() 
        {
            _rigidbodies = new List<Rigidbody>();
            _bullets = new List<BulletBehaviour>();
            velocityVals = new List<Vector3>();
            _ownerBullets = new List<GameObject>();
            _eventListener.IntendedSender = _blockScript.owner;
            _blockHealth.health.Val = bulletCapacity;
            _blockScript.specialActions += DetonateBlock;
        }
        //Destroys block and fires all absorbed projectiles
        public void DetonateBlock(object[]arg)
        {
            _blockScript.DestroyBlock(0);
            for (int i = 0; i < _bullets.Count; i++)
            {
                if (_bullets[i] != null)
                {
                    if(_bullets[i].Owner == _blockScript.owner.name)
                    {
                        _ownerBullets.Add(_bullets[i].gameObject);
                    }
                    _bullets[i].Owner = _blockScript.owner.name;
                    _bullets[i].DamageVal *= 2;
                }
            }
            for (int i = 0; i < _rigidbodies.Count; i++)
            {
                if (_rigidbodies[i] != null)
                {
                    _rigidbodies[i].GetComponent<Transform>().parent = null;
                    _rigidbodies[i].GetComponent<Collider>().enabled = true;
                    _rigidbodies[i].isKinematic = false;
                    if(_ownerBullets.Contains(_rigidbodies[i].gameObject))
                    {
                        _rigidbodies[i].AddForce((velocityVals[i]) * 2, ForceMode.Impulse);
                        continue;
                    }
                    _rigidbodies[i].AddForce(-(velocityVals[i]) *2, ForceMode.Impulse); 
                }
            }
        }
        /// <summary>
        /// Upgrades:
        /// Bullet Capacity Increased
        /// </summary>
        /// <param name="otherBlock"></param>
        public void UpgradeBlock(GameObject otherBlock)
        {
            BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.specialFeature.name == gameObject.name)
                {
                    component.specialFeature.GetComponent<KineticBlockBehaviour>().bulletCapacity+= _bulletCapUpgradeVal;
                    component.specialFeature.GetComponent<KineticBlockBehaviour>()._blockHealth.health.Val+=_bulletCapUpgradeVal;
                    return;
                }
            }
            TransferOwner(otherBlock);
        }
        public void TransferOwner(GameObject otherBlock)
        {
            _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            _blockScript.componentList.Add(this);
            _blockScript.specialActions += DetonateBlock;
            _blockHealth = otherBlock.GetComponent<HealthBehaviour>();
            transform.SetParent(otherBlock.transform,false);
        }
        // Update is called once per frame
        void Update() {
        }
        //This needs to be done here. Resolve collision func is called twice
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Projectile"))
            {
                BulletBehaviour bulletscript = other.GetComponent<BulletBehaviour>();
                /// <summary>
                /// Since a ramming block has its barrier as a separate collider,
                /// there must be a separate condition to get its parent added to 
                /// the list instead of it. The other condition should work for
                /// every other projectile.
                /// </summary>
                /// <param name="other"></param>
                if (other.name == "Ramming Barrier")
                {
                    Rigidbody parentRigidbody = other.GetComponentInParent<Rigidbody>();
                    Collider parentCollider = parentRigidbody.GetComponentInParent<Collider>();
                    if (parentRigidbody != null)
                    {
                        //disables ramming barrier
                        other.GetComponent<Collider>().enabled= false;
                        //disables parent
                        parentCollider.enabled = false;
                        parentCollider.transform.SetParent(transform, false);
                        parentCollider.transform.position = transform.position;
                        parentCollider.transform.localScale /= 2;
                        //Adds parent to list
                        velocityVals.Add(parentRigidbody.velocity);
                        _rigidbodies.Add(parentRigidbody);
                        parentRigidbody.velocity = Vector3.zero;
                        parentRigidbody.isKinematic = true;
                        
                    }
                    _bullets.Add(bulletscript);
                    _blockHealth.takeDamage(bulletscript.DamageVal);
                    return;
                }
                //Condition for normal projectiles
                if (bulletscript != null)
                {
                    _bullets.Add(bulletscript);
                    bulletscript.hitTrail.SetActive(false);
                    other.GetComponent<Collider>().enabled = false;
                    other.transform.localScale /= 2;
                    other.transform.SetParent(transform, false);
                    other.transform.position = transform.position;
                    _blockHealth.takeDamage(bulletscript.DamageVal);
                }
                
                Rigidbody temp = other.GetComponent<Rigidbody>();
                if (temp != null)
                {
                    velocityVals.Add(temp.velocity);
                    _rigidbodies.Add(temp);
                    temp.velocity = Vector3.zero;
                    temp.isKinematic = true;
                }

            }
        }
        public void ResolveCollision(GameObject other)
        {
            return;
        }

        public void ActivateDisplayMode()
        {
            return;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
            gameObject.SetActive(false);
        }

        public void ActivatePowerUp()
        {
            if (_currentKineticBomb == null)
            {
                _currentKineticBomb = Instantiate(_kineticBombRef,transform.position,transform.rotation).GetComponent<KineticBombBehaviour>();
                _currentKineticBomb.owner = playerAttackScript.name;
                _currentKineticBomb.ownerTransform = transform;
                _currentKineticBomb.moveDirection = transform.parent.forward;
                return;
            }
            else if(playerAttackScript.SecondAbilityUseAmount.Val > 1)
            {
                playerAttackScript.IncreaseAmmuntion(1);
                _currentKineticBomb.Explode();
                _currentKineticBomb = null;
            }
            else
            {
                _currentKineticBomb.Explode();
                _currentKineticBomb = null;
            }
        }

        public void DetachFromPlayer()
        {
            playerAttackScript.secondaryInputCanBeHeld = false;
            GameObject temp = gameObject;
            Destroy(temp);
        }

        public void Stun()
        {
            
        }

        public void Unstun()
        {
           
        }

        public void DeactivatePowerUp()
        {
            throw new System.NotImplementedException();
        }
    }
}
