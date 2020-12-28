using GridGame.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.GamePlay.BlockScripts
{
	public class OrbiterBlockBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {

		[SerializeField]
		private BlockBehaviour _blockScript;
        [SerializeField]
        private GameObject _orb1;
        [SerializeField]
        private GameObject _orb2;
        [SerializeField]
        private GameObject _orb3;
        [SerializeField]
        private int _orbUpgradeVal;
        [SerializeField]
        private GameEventListener _upgradeEventListener;
        [SerializeField]
        private GameEventListener _deleteEventListener;
        [SerializeField] private int playerUseAmount;
        private PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField] private Color _displayColor;
        [SerializeField] private List<HitboxBehaviour> orbHitBoxes;
        private bool dontDeleteOrbs;
        [SerializeField]
        private bool _canBeHeld;
        private bool _orbsOveridden;
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

        public void UpgradeBlock(GameObject otherBlock)
		{
			BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
			foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
			{
				if (component.Name == Name)
				{
					component.specialFeature.GetComponent<OrbiterBlockBehaviour>().UpgradeOrbs();
                    return;
				}
			}
			TransferOwner(otherBlock);
		}
        public void DestroyOrbs()
        {
            if(dontDeleteOrbs)
            {
                return;
            }
            GameObject temp = _orb1;
            GameObject temp2 = _orb2;
            GameObject temp3 = _orb3;
            Destroy(temp);
            Destroy(temp2);
            Destroy(temp3);
        }
        //Adds another orb to the orbit
        public void UpgradeOrbs()
        {
            if(_orb2.activeInHierarchy)
            {
                _orb3.SetActive(true);
            }
            _orb2.SetActive(true);
        }
        //Takes whatever feature that was merged into the orbiter block and gives it to the orbs instead
        public void ChangeOrbProperties(GameObject otherBlock)
        {
            if(otherBlock == null)
            {
                return;
            }
            BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach(GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.specialFeature.CompareTag("Barrier") || component.specialFeature.CompareTag("Gun"))
                {
                    component.block = block;
                    component.specialFeature.transform.SetParent(_orb1.transform,false);
                    component.specialFeature.transform.position = _orb1.transform.position;
                    _orb1.transform.localScale *= 1.5f;
                    component.specialFeature.transform.localScale = _orb1.transform.localScale;
                    _orb2.transform.localScale *= 1.5f;
                    GameObject componentClone =Instantiate(component.specialFeature, _orb2.transform, false);
                    componentClone.transform.position =_orb2.transform.position;
                    DisableOrbAttack();
                    DisableOrbMesh();
                    _orbsOveridden = true;
                    return;
                }
            }
        }

        public void DisableOrbAttack()
        {
            _orb1.GetComponent<OrbBehaviour>().DamageVal = 0;
            _orb2.GetComponent<OrbBehaviour>().DamageVal = 0;
            _orb3.GetComponent<OrbBehaviour>().DamageVal = 0;
        }
        public void DisableOrbMesh()
        {
            _orb1.GetComponent<MeshRenderer>().enabled = false;
            _orb1.GetComponent<SphereCollider>().enabled = false;
            _orb2.GetComponent<SphereCollider>().enabled = false;
            _orb2.GetComponent<MeshRenderer>().enabled = false;
            _orb3.GetComponent<SphereCollider>().enabled = false;
            _orb3.GetComponent<MeshRenderer>().enabled = false;
        }
        public void EnableOrbMesh()
        {
            _orb1.GetComponent<MeshRenderer>().enabled = true;
            _orb1.GetComponent<SphereCollider>().enabled = true;
            _orb2.GetComponent<SphereCollider>().enabled = true;
            _orb2.GetComponent<MeshRenderer>().enabled = true;
            _orb3.GetComponent<SphereCollider>().enabled = true;
            _orb3.GetComponent<MeshRenderer>().enabled = true;
        }
		public void TransferOwner(GameObject otherBlock)
		{
			_blockScript = otherBlock.GetComponent<BlockBehaviour>();
			_blockScript.componentList.Add(this);
            transform.SetParent(otherBlock.transform,false);
            transform.position = otherBlock.transform.position;
            _upgradeEventListener.intendedSender = otherBlock;
            _deleteEventListener.intendedSender = otherBlock;
            ChangeOrbProperties(otherBlock);
            _orb1.GetComponent<OrbBehaviour>().block = _blockScript;
            _orb2.GetComponent<OrbBehaviour>().block = _blockScript;
            _orb3.GetComponent<OrbBehaviour>().block = _blockScript;
        }

        public void ResolveCollision(GameObject collision)
        {
            return;
        }
        public void ActivateDisplayMode()
        {
            DisableOrbAttack();
            DisableOrbMesh();
            return;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            _orb2.SetActive(true);
            _orb3.SetActive(true);
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            transform.SetParent(player.transform, false);
            transform.position += Vector3.up*2;
            teleportBeam.transform.parent = null;
            teleportBeam.Teleport(player.transform.position);
            dontDeleteOrbs = true;
            DisableOrbAttack();
            player.SetSecondaryWeapon(this, playerUseAmount);
        }

        public void ActivatePowerUp()
        {
            foreach(HitboxBehaviour hitbox in orbHitBoxes)
            {
                hitbox.MakeActive(2.0f);
            }
        }

        public void DetachFromPlayer()
        {
            GameObject temp = gameObject;
            Destroy(temp);
        }

        public void Stun()
        {
            DisableOrbMesh();
        }

        public void Unstun()
        {
            if (!_orbsOveridden)
                EnableOrbMesh();
        }

        public void DeactivatePowerUp()
        {
            throw new System.NotImplementedException();
        }
    }
}
