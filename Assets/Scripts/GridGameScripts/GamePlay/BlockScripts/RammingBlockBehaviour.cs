using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame.GamePlay.AI;
using GridGame.Movement;

namespace GridGame.GamePlay.BlockScripts
{
    public class RammingBlockBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {
        [SerializeField]
        private BlockBehaviour _blockScript;
        [SerializeField]
        private Rigidbody _blockRigidbody;
        private Rigidbody _playerRigidbody;
        private Vector3 _ramForce;
        [SerializeField]
        private float _ramForceScale;
        [SerializeField] public int DamageVal;
        [SerializeField] public int UpgradeVal;
        private Quaternion playerRotation;
        public bool isRamming;
        private BulletBehaviour _projectileScript;
        private PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private int playerUseAmount;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        private bool playerAttached;
        [SerializeField]
        private bool _canBeHeld;
        [SerializeField]
        private Color _displayColor;
        private Vector3 positionRef;
        private GameObject previousPanel;
        private PlayerAnimationBehaviour playerAnimator;
        private MeshRenderer meshRenderer;
        bool playerInBall;
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

        private void Start()
        {
            _blockScript.specialActions += Ram;
            _blockRigidbody.isKinematic = true;
            _projectileScript = GetComponent<BulletBehaviour>();
            meshRenderer = GetComponent<MeshRenderer>();
        }
       
        public void InitializeProjectileScript()
        {
            _projectileScript.enabled = true;
            _projectileScript.DamageVal = DamageVal;
            _projectileScript.Owner = _blockScript.owner.name;
            _projectileScript.lifetime = 5;
        }
        public void UpgradeBlock(GameObject otherBlock)
        {
            BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.specialFeature.name == gameObject.name)
                {
                    component.specialFeature.GetComponent<RammingBlockBehaviour>().DamageVal += UpgradeVal;
                    return;
                }
            }
            TransferOwner(otherBlock);
        }
        public void Ram(object[] args)
        {
            if(isRamming)
            {
                return;
            }
            isRamming = true;
            _blockScript.inMotion = true;
            InitializeProjectileScript();
            transform.parent.rotation = _blockScript.owner.transform.rotation;
            if(block.gameObject.name != "Ramming Block(Clone)")
            {
                transform.parent.position += new Vector3(0, .4f, 0);
            }
            gameObject.tag = "Projectile";
            _blockRigidbody.isKinematic = false;
            _blockScript.currentPanel.GetComponent<GridScripts.PanelBehaviour>().Occupied = false;
            _blockScript.currentPanel.GetComponent<GridScripts.PanelBehaviour>().blockCounter = 0;
            _blockScript.GetComponent<OtherScripts.ScreenShakeBehaviour>().updatePosition = true;
            _ramForce = transform.parent.forward * _ramForceScale;
            block.canDelete = false;
            if((int)_ramForce.z != 0)
            {
                _blockRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                _blockRigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
            }
            _blockRigidbody.AddForce(_ramForce, ForceMode.Impulse);
        }
        public void PlayerRam()
        {
            if (isRamming)
            {
                return;
            }
            isRamming = true;
            positionRef = _playerRigidbody.position;
            _playerRigidbody.isKinematic = false;
            playerAttackScript.GetComponent<PlayerMovementBehaviour>().CurrentPanel.GetComponent<GridScripts.PanelBehaviour>().Occupied = false;
            playerAttackScript.GetComponent<PlayerAnimationBehaviour>().EnableMoveAnimation();
            playerAttackScript.GetComponent<GridGame.GamePlay.OtherScripts.ScreenShakeBehaviour>().enabled = false;
            _ramForce = transform.parent.forward * _ramForceScale;
            if ((int)_ramForce.z != 0)
            {
                _playerRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                _playerRigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
            }
            _playerRigidbody.AddForce(_ramForce, ForceMode.Impulse);
        }
        private void DisablePlayerRam()
        {
            gameObject.SetActive(false);
            _playerRigidbody.velocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
            _playerRigidbody.isKinematic = true;
            _playerRigidbody.transform.rotation = Quaternion.identity;
            _playerRigidbody.transform.position = positionRef;
            isRamming = false;
            _ramForceScale = 0;
            _canBeHeld = false;
            playerAttackScript.secondaryInputCanBeHeld = false;
            playerAttackScript.GetComponent<OtherScripts.ScreenShakeBehaviour>().enabled = false;
        }
        public void TransferOwner(GameObject otherBlock)
        {
            _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            _blockScript.componentList.Add(this);
            _blockRigidbody = otherBlock.GetComponent<Rigidbody>();
            _blockRigidbody.isKinematic = true;
            _blockScript.specialActions += Ram;
            transform.SetParent(otherBlock.transform,false);
            GetComponent<GameEventListener>().intendedSender = otherBlock;
            GetComponent<BulletBehaviour>().Laser = otherBlock;
        }
        public void RemoveOtherSpecialActions()
        {
            block.specialActions = null;
            block.specialActions += Ram;
        }
        void GridGame.Interfaces.IUpgradable.ResolveCollision(GameObject collision)
        {
            if (_blockScript.canUpgrade)
            {
                return;
            }
            if(playerAttached && !collision.CompareTag("Panel") && isRamming && !collision.CompareTag("Barrier"))
            {
                if(collision.CompareTag("Wall"))
                {
                    DisablePlayerRam();
                    return;
                }
                HealthBehaviour otherHealth = collision.GetComponent<HealthBehaviour>();
                if (otherHealth != null)
                {
                    otherHealth.takeDamage(DamageVal);
                }
                playerAttackScript.DecreaseAmmuntion(5);
                if (playerAttackScript.SecondAbilityUseAmount.Val <= 0 || collision.CompareTag("Core"))
                {
                    StopAllCoroutines();
                    DisablePlayerRam();
                    return;
                }
                _playerRigidbody.transform.position -= (_playerRigidbody.velocity.normalized * 2);
                _playerRigidbody.velocity = -_playerRigidbody.velocity;
                return;
            }
            _projectileScript.ResolveCollision(collision.gameObject);
        }
        private void OnDestroy()
        {
            if(!playerAttached)
            {
                GameObject temp = block.gameObject;
                Destroy(temp);
            }
        }

        public void ActivateDisplayMode()
        {
            return;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            _ramForceScale = 0;
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            SphereCollider collider = GetComponent<SphereCollider>();
            collider.isTrigger = true;
            playerAttached = true;
            playerAnimator = player.GetComponent<PlayerAnimationBehaviour>();
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
            gameObject.SetActive(false);
            _playerRigidbody = player.GetComponent<Rigidbody>();
            positionRef= player.transform.position;
        }

        public void ActivatePowerUp()
        {
            if(isRamming)
            {
                DisablePlayerRam();
                return;
            }
            _ramForceScale +=.5f;
            _canBeHeld = true;
            playerAttackScript.secondaryInputCanBeHeld = true;
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
            gameObject.SetActive(true);
            PlayerRam();
            _canBeHeld = false;
        }
        private void Update()
        {
            if ((isRamming && playerAttached) || playerInBall)
            {
                playerAnimator.EnableMoveAnimation();
            }
        }

    }
}

