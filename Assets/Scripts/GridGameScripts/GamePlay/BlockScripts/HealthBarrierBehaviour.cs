using GridGame.Movement;
using System;
using System.Collections;
using UnityEngine;

namespace GridGame.GamePlay.BlockScripts
{
	public class HealthBarrierBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable
	{
		[SerializeField] private HealthBehaviour _healthScript;
		[SerializeField] private BlockBehaviour _blockScript;
		[SerializeField] private int _upgradeVal;
		[SerializeField] private int _healVal;
		[SerializeField] private int _bulletsToHeal;
        [SerializeField]
        private GameEventListener _deleteEventListener;
        private float _healTimer;
        private string _nameOfItem;
        [SerializeField]
        private float _timeUntilNextHeal;
		private int _currentBulletsToHeal;
        private bool _canHeal;
        private int _healCounter;
        private int _healLimit;
        [SerializeField]
        private Color _displayColor;
        [SerializeField]
        private int playerUseAmount;
        private bool playerAttached;
        PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField]
        private bool _canBeHeld;

        [SerializeField] private FlashBehaviour _flashScript;
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
                return _nameOfItem;
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
                displayColor = value;
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
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        // Use this for initialization
        void Start ()
		{
            _canHeal = true;
            _nameOfItem = gameObject.name;
            _healTimer = Time.time + _timeUntilNextHeal;
            _healCounter = 0;
        }
        public void FindMaxHealthLimit()
        {
            if(_healthScript == null)
            {
                return;
            }
            _healLimit = (int)(_healthScript.HealthRef.Val + _healthScript.HealthRef.Val * 1.5f);
            if(_healLimit > BlackBoard.maxBlockHealth)
            {
                _healLimit = BlackBoard.maxBlockHealth;
            }
        }
        public void DestroyBarrier()
        {
            GameObject temp = gameObject;
            Destroy(temp);
        }
        private void TryHeal()
		{
            if(_canHeal)
            {
                _healCounter++;
                _healthScript.health.Val +=_healVal;
                _healTimer = Time.time + _timeUntilNextHeal;
            }
		}
		public void UpgradeBlock(GameObject otherBlock)
		{
			BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.specialFeature.name == gameObject.name)
                {
                    component.specialFeature.GetComponent<HealthBarrierBehaviour>()._healLimit *= 2;
                    StartCoroutine(TryToHeal());
                    return;
				}
			}
			TransferOwner(otherBlock);
		}
		public void TransferOwner(GameObject otherBlock)
		{
            block = otherBlock.GetComponent<BlockBehaviour>();
            
            block.componentList.Add(this);
            transform.SetParent(otherBlock.transform, false);
            _deleteEventListener.IntendedSender = otherBlock;
            if (otherBlock.name != "Repair Block(Clone)" && otherBlock.name != "Orbiter Block(Clone)")
            {
                _healthScript = otherBlock.GetComponent<HealthBehaviour>();
                FindMaxHealthLimit();
                StartCoroutine(TryToHeal());
            }
        }
        IEnumerator TryToHeal()
        {
            while(_healthScript!= null)
            {
                if(_canHeal == false)
                {
                    break;
                }
                if(_healthScript.health.Val < _healLimit)
                {
                    _healthScript.UnclampedHeal(_healVal);
                }
                yield return new WaitForSeconds(_timeUntilNextHeal);
            }
        }
        private void OnTriggerStay(Collider other)
        {
            HealthBehaviour otherHealth = other.GetComponent<HealthBehaviour>();
            if (playerAttached && otherHealth != null && other.CompareTag("Block") && other.name != "Repair Block(Clone)")
            {
                otherHealth.ClampedHeal(_healVal, _healLimit);
                if (otherHealth.health.Val == _healLimit)
                {
                    Instantiate(other, other.transform.position, other.transform.rotation);
                    otherHealth.health.Val = otherHealth.HealthRef.Val;
                }
                return;
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            HealthBehaviour otherHealth = other.GetComponent<HealthBehaviour>();
            if (playerAttached)
            {
                return;
            }
            if (other.CompareTag("Projectile"))
            {
                BulletBehaviour bulletScript = other.GetComponent<BulletBehaviour>();
                block.HealthScript.takeDamage(bulletScript.DamageVal);
                bulletScript.Destroy();
            }
            if (other.CompareTag("Block") && other.name != "Repair Block(Clone)" && other.gameObject != block.gameObject)
            {
                if(otherHealth != null)
                {
                    FindMaxHealthLimit();
                    otherHealth.ClampedHeal(_healVal, 50);
                }
            }
        }
        public void ResolveCollision(GameObject collision)
        {
            return;
        }
        public void ActivateDisplayMode()
        {
            return;
        }
        private void OnDestroy()
        {
            _canHeal = false;
        }
        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            playerAttached = true;
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
            _healLimit = BlackBoard.maxBlockHealth;
            gameObject.SetActive(false);
            transform.position += playerAttackScript.transform.forward * 2;
        }

        public void ActivatePowerUp()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
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
            StopAllCoroutines();
            _flashScript.isInfinite = true;
            _flashScript.StartFlashing();
        }

        public void Unstun()
        {
            StartCoroutine(TryToHeal());
            _flashScript.isInfinite = false;
            _flashScript.StopFlashing();
        }

        public void DeactivatePowerUp()
        {
            gameObject.SetActive(false);
        }
    }
}
