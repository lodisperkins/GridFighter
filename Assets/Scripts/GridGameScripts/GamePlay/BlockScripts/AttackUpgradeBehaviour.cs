using GridGame.Movement;
using System;
using System.Collections;
using UnityEngine;

namespace GridGame.GamePlay.BlockScripts
{
	public class AttackUpgradeBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {

		[SerializeField] private GunBehaviour turretScript;
		[SerializeField] private BlockBehaviour _blockScript;
		[SerializeField] private int _damageUpgradeVal;
		[SerializeField] private int _bulletForceUpgradeVal;
		[SerializeField] private int _ammoUpgradeVal;
		[SerializeField] private HealthBehaviour _blockHealth;
        [SerializeField] private int playerUseAmount;
        private PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField] private Color _displayColor;
        [SerializeField] private bool _canBeHeld;
        [SerializeField] private GameObject smartBulletRef;
        private string _nameOfItem;
        [SerializeField] private FlashBehaviour flashScript;
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
        public string Name
        {
            get
            {
                return _nameOfItem;
            }
        }
        public GameObject specialFeature
        {
            get
            {
                return gameObject;
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
            _nameOfItem = specialFeature.name;
			turretScript = GetComponent<GunBehaviour>();
            turretScript.OutOfAmmo.AddListener(_blockScript.DestroyBlock);
            turretScript.owner = _blockScript.owner.name;
            _blockHealth.health.Val = turretScript.CurrentAmmo;
		}
        
		public void UpgradeBlock(GameObject otherBlock)
		{
			BlockBehaviour _otherBlockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _otherBlockScript.componentList)
            {
                if (component.Name == Name)
                {
                    component.specialFeature.GetComponent<AttackUpgradeBehaviour>().Upgrade();
					return;
				}
			}
			TransferOwner(otherBlock);
		}
        /// <summary>
        /// Upgrades:
        /// Bullet damage increased
        /// Bullet speed increased
        /// Ammo capacity increased
        /// Fire rate increased
        /// </summary>
        /// <param name="otherBlock"></param>
        public void Upgrade()
        {
            turretScript.damageVal += _damageUpgradeVal;
            turretScript.bulletForceScale += _bulletForceUpgradeVal;
            turretScript.bulletDelay -= .2f;
            _blockHealth.UnclampedHeal(_ammoUpgradeVal);
        }
        /// <summary>
        /// - Sets health of other block to be current
        /// ammunition count
        /// 
        /// -  Adds the detroy block func for the
        /// other block as a listener for the out of anmmo event
        /// </summary>
        /// <param name="otherBlock"></param>
		public void TransferOwner(GameObject otherBlock)
		{
			BlockBehaviour blockScript = otherBlock.GetComponent<BlockBehaviour>();
            _blockHealth = otherBlock.GetComponent<HealthBehaviour>();
			turretScript.OutOfAmmo.AddListener(blockScript.DestroyBlock);
			blockScript.componentList.Add(this);
			transform.SetParent(otherBlock.transform,false);
		}
        //Used to decrease health everytime a shot is fired
		public void DecreaseHealth()
		{
            if(playerAttackScript == null)
            {
                _blockHealth.takeDamage(1);
            }
			
		}

        public void ResolveCollision(GameObject collision)
        {
            return;
        }
        //Turns off turret 
        public void ActivateDisplayMode()
        {
            gameObject.SetActive(false);
            turretScript.StopAllCoroutines();
            turretScript.enabled = false;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            turretScript.IsTurret = false;
            turretScript.ChangeBullet(smartBulletRef);
            turretScript.StopAllCoroutines();
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
        }

        public void ActivatePowerUp()
        {
            turretScript.FireBullet();
        }

        public void DetachFromPlayer()
        {
            GameObject temp = gameObject;
            Destroy(temp);
        }
        
        public void Stun()
        {
            turretScript.StopAllCoroutines();
            flashScript.isInfinite = true;
            flashScript.StartFlashing();
        }

        public void Unstun()
        {
            turretScript.StartCoroutine(turretScript.Fire());
            flashScript.isInfinite = false;
            flashScript.StopFlashing();
        }
        
        public void DeactivatePowerUp()
        {
            throw new NotImplementedException();
        }
    }
}
