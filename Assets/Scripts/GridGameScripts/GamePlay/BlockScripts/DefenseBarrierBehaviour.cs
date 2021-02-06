using System;
using UnityEngine;
using System.Collections;
using GridGame.Movement;

namespace GridGame.GamePlay.BlockScripts
{
	public class DefenseBarrierBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable
	{
		[SerializeField] private HealthBehaviour healthScript;
		private float _decayVal;
		[SerializeField] private int _upgradeVal;
		private string colorName;
		private Material _attachedMaterial;
        [SerializeField]
        private BlockBehaviour _blockScript;
		[SerializeField] private RoutineBehaviour shieldTimer;
        private string _nameOfItem;
        private float lerpNum;
        [SerializeField]
        private Event onHit;
        [SerializeField]
        private Color _displayColor;
        [SerializeField]
        private int playerUseAmount;
        private bool playerAttached;
        PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField]
        private bool canReflect;
        [SerializeField]
        private float shieldActiveTime;
        [SerializeField]
        private bool _canBeHeld;
		// Use this for initialization
		void Start ()
		{
			_decayVal = (float)1/shieldTimer.actionLimit;
			lerpNum = (float)1/shieldTimer.actionLimit;
			colorName = "Color_262603E3";
			_attachedMaterial = GetComponent<MeshRenderer>().material;
            _nameOfItem = gameObject.name;
		}
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
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ResolveCollision(other.gameObject);
        }

        public void UpgradeBlock(GameObject otherBlock)
		{
			BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.Name == Name)
                {
                    component.specialFeature.GetComponent<DefenseBarrierBehaviour>().Upgrade();
					return;
				}
			}
			TransferOwner(otherBlock);
		}
        /// <summary>
        /// Upgrades:
        /// Increases health
        /// Replenishes shield
        /// </summary>
		public void Upgrade()
		{
			gameObject.SetActive(true);
			healthScript.health.Val += _upgradeVal;
			shieldTimer.actionLimit += 10;
			shieldTimer.ResetActions();
			_decayVal = (float)1/shieldTimer.actionLimit;
			lerpNum = (float)1/shieldTimer.actionLimit;
			_attachedMaterial.SetColor(colorName,Color.Lerp(Color.green, Color.red,lerpNum));
		}
        //Destrpys the barrier surrounding the block and reduces the health to 5
		public void DestroyBarrier()
		{
            if(healthScript.health.Val > 5)
            {
                healthScript.health.Val = 5;
            }
			gameObject.SetActive(false);
		}
        //Changes the color of the sphere to reflect how much time it has left
		public void DecaySphere()
		{
			_attachedMaterial.SetColor(colorName,Color.Lerp(Color.green, Color.red,lerpNum));
			lerpNum += _decayVal;
		}
        /// <summary>
        /// Transfers shield to other block
        /// while also healing other block.
        /// </summary>
        /// <param name="otherBlock"></param>
		public void TransferOwner(GameObject otherBlock)
		{
			block = otherBlock.GetComponent<BlockBehaviour>();
			healthScript = otherBlock.GetComponent<HealthBehaviour>();
			healthScript.health.Val += _upgradeVal;
			block.componentList.Add(this);
			transform.SetParent(otherBlock.transform,false);
		}
        //Tells the block to take damage
        public void ResolveCollision(GameObject collision)
        {
            if (collision.CompareTag("Projectile") && gameObject.activeSelf)
            {
                if (canReflect)
                {
                    collision.gameObject.GetComponent<BulletBehaviour>().Reflect(playerAttackScript.name);
                    return;
                }
                block.HealthScript.takeDamage(collision.GetComponent<BulletBehaviour>().DamageVal);
                onHit.Raise(gameObject);
                collision.GetComponent<BulletBehaviour>().Destroy();
            }
        }
        //Stops shield from decaying
        public void ActivateDisplayMode()
        {
            shieldTimer.StopAllCoroutines();
            shieldTimer.enabled = false;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            shieldTimer.shouldStop = true;
            shieldTimer.StopAllCoroutines();
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            SphereCollider collider = GetComponent<SphereCollider>();
            collider.isTrigger = false;
            healthScript = player.GetComponent<HealthBehaviour>();
            transform.localScale *= 1.5f;
            playerAttached = true;
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
            canReflect = true;
        }
        private IEnumerator EnableReflector()
        {
            canReflect = true;
            yield return new WaitForSeconds(.5f);
            gameObject.SetActive(false);
            canReflect = false;
        }
     
        public void ActivatePowerUp()
        {
            if(!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                healthScript.isInvincible = true;
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
            gameObject.SetActive(false);
            healthScript.isInvincible = false;
        }
    }
}
