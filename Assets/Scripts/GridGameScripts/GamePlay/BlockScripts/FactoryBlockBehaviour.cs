using GridGame.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame.Movement;

namespace GridGame.GamePlay.BlockScripts
{
    public class FactoryBlockBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {
        private PlayerMovementBehaviour _playerMoveScript;
        private GridGame.PlayerSpawnBehaviour _playerSpawnScript;
        private List<GameObject> _playerBlocks;
        private VariableScripts.BlockVariable _currentBlock;
        private int _blockIndex;
        [SerializeField]
        private BlockBehaviour _blockScript;
        [SerializeField]
        private RoutineBehaviour _spawnRoutine;
        GridScripts.PanelBehaviour _currentPanel;
        private GridScripts.Condition NeighboorCheck;
        List<GridScripts.PanelBehaviour> panelsInRange;
        private Color SelectionColor;
        private string colorName;
        [SerializeField]
        private GameObject box;
        private Material _attachedMaterial;
        private int panelIndex;
        private RaycastHit _rayHit;
        [SerializeField] private int playerUseAmount;
        private PlayerAttackBehaviour playerAttackScript;
        [SerializeField]
        private TeleportBeamBehaviour teleportBeam;
        [SerializeField] private Color _displayColor;
        [SerializeField] private GameObject _blockTransporterRef;
        private BlockTransporterBehaviour _blockTransporter;
        [SerializeField] private bool _canBeHeld;
        private bool _playerAttached;
        private bool _canSpawnGlobally;
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
        void Start () {
            _playerMoveScript = _blockScript.owner.GetComponent<PlayerMovementBehaviour>();
            _playerSpawnScript = _blockScript.owner.GetComponent<PlayerSpawnBehaviour>();
            _playerBlocks = _playerSpawnScript.Blocks;
            _currentPanel = _blockScript.Panel;
            _blockIndex = 0;
            panelIndex = 0;
            _currentBlock = new VariableScripts.BlockVariable();
            _attachedMaterial = box.GetComponent<MeshRenderer>().material;
            _currentBlock.Block = _playerBlocks[_blockIndex];
            block.specialActions += SwitchBlocks;
            NeighboorCheck += CheckIfNeighboor;
            colorName = "Color_262603E3";
            _blockScript.HealthScript.health.Val = _spawnRoutine.numberOfActionsLeft;
            List<GridScripts.PanelBehaviour> panelsInRange = new List<GridScripts.PanelBehaviour>();
            CheckCurrentPanel();
            FindNeighbors();
        }
        public bool CheckIfNeighboor(object[] arg)
        {
            GridScripts.PanelBehaviour temp = (GridScripts.PanelBehaviour)arg[0];
            Vector2 position = temp.Position;
            Vector2 displacdementX = new Vector2(1, 0);
            Vector2 displacdementY = new Vector2(0, 1);
            if (position == _currentPanel.Position + displacdementX || position == _currentPanel.Position - displacdementX)
            {
                return true;
            }
            if (position == _currentPanel.Position + displacdementY || position == _currentPanel.Position - displacdementY)
            {
                return true;
            }
            return false;
        }
        public void FilterNeighbors()
        {
            if (Physics.Raycast(transform.position, -transform.forward, out _rayHit, 2))
            {
                GridScripts.PanelBehaviour panel = _rayHit.transform.GetComponent<GridScripts.PanelBehaviour>();
                if(panel)
                {
                    panelsInRange.Remove(panel);
                }
            }
        }
        private void CheckCurrentPanel()
        {
            if(block.owner.name == "Player1" && BlackBoard.p2PanelList.Contains(_currentPanel))
            {
                _canSpawnGlobally = true;
            }
            else if(block.owner.name == "Player2" && BlackBoard.p1PanelList.Contains(_currentPanel))
            {
                _canSpawnGlobally = true;
            }
        }
        public bool FindNeighbors()
        {
            if(_canSpawnGlobally)
            {
                if (GridScripts.GridBehaviour.globalPanelList.GetPanels(NeighboorCheck, out panelsInRange))
                {
                    FilterNeighbors();
                    return true;
                }
                Debug.Log("Couldn't find neighboors");
                return false;
            }
            if (_playerMoveScript.Panels.GetPanels(NeighboorCheck, out panelsInRange))
            {
                FilterNeighbors();
                return true;
            }
            Debug.Log("Couldn't find neighboors");
            return false;
        }
        public void SpawnBlock()
        {
            _currentBlock.Block = _playerBlocks[_blockIndex];
            panelIndex++;
            Quaternion rotation = new Quaternion(0, _playerSpawnScript.Block_rotation.y, 0,0);
            if(panelIndex >= panelsInRange.Count)
            {
                panelIndex = 0;
            }
            if (panelsInRange[panelIndex].blockCounter + _currentBlock.BlockScript.BlockWeightVal <= 3)
            {
               
                var position = new Vector3(panelsInRange[panelIndex].gameObject.transform.position.x, _currentBlock.Block.transform.position.y, panelsInRange[panelIndex].gameObject.transform.position.z);
                GameObject BlockCopy = Instantiate(_currentBlock.Block, position, _playerSpawnScript.Block_rotation);
                BlockBehaviour copyScript = BlockCopy.GetComponent<BlockBehaviour>();
                copyScript.currentPanel = panelsInRange[panelIndex].gameObject;
                copyScript.owner = _blockScript.owner;
                copyScript.InitializeBlock();
                TransferUpgrades(copyScript);
                copyScript.currentPanel.GetComponent<GridScripts.PanelBehaviour>().blockCounter -= 1;
                panelsInRange[panelIndex].Occupied = true;
                panelsInRange[panelIndex].Selected = false;
                BlockCopy.GetComponent<Collider>().isTrigger = true;
                block.HealthScript.takeDamage(1);
            }
        }
        public void SwitchBlocks(object[] args)
        {
            _blockIndex++;
            if(_blockIndex == 2)
            {
                _blockIndex++;
            }
            if(_blockIndex > 3)
            {
                _blockIndex = 0;
            }
        }
        private void UpdateColor()
        {
            switch(_blockIndex)
            {
                case (0):
                    {
                        SelectionColor = Color.red;
                        break;
                    }
                case (1):
                    {
                        SelectionColor = Color.green;
                        break;
                    }
                case (2):
                    {
                        SelectionColor = Color.yellow;
                        break;
                    }
                case (3):
                    {
                        SelectionColor = Color.white;
                        break;
                    }
            }
            if(_attachedMaterial != null)
            {
                _attachedMaterial.SetColor(colorName, SelectionColor);
            }
        }
        public void UnHighlightPanels()
        {
            foreach (GridScripts.PanelBehaviour panel in panelsInRange)
            {
                panel.Selected = false;
            }
        }
        public void UpgradeBlock(GameObject otherBlock)
        {
            BlockBehaviour _blockScript = otherBlock.GetComponent<BlockBehaviour>();
            foreach (GridGame.Interfaces.IUpgradable component in _blockScript.componentList)
            {
                if (component.Name == Name)
                {
                    component.specialFeature.GetComponent<FactoryBlockBehaviour>().Upgrade();
                    return;
                }
            }
            TransferOwner(otherBlock);
        }
        public void TransferUpgrades(BlockBehaviour spawnedBlock)
        {
            GameObject compClone = null;
            foreach (GridGame.Interfaces.IUpgradable component in block.componentList)
            {
                if(component.Name == Name)
                {
                    continue;
                }
                foreach(GridGame.Interfaces.IUpgradable othercomponent in spawnedBlock.componentList)
                {
                    if(component.specialFeature.CompareTag(othercomponent.specialFeature.tag))
                    {
                        continue;
                    }
                    compClone = component.specialFeature.gameObject;
                }
            }
            if(compClone != null)
            {
                compClone = Instantiate(compClone, spawnedBlock.transform);
                compClone.GetComponent<GridGame.Interfaces.IUpgradable>().TransferOwner(spawnedBlock.gameObject);
            }
        }
        public void DestroyFactory()
        {
            _spawnRoutine.StopAllCoroutines();
            block.DestroyBlock();
        }
        public void Upgrade()
        {
            var routineScript = GetComponent<RoutineBehaviour>();
            routineScript.ResetActions();
            routineScript.actionLimit += 3;
            routineScript.actionDelay -= .5f;
            _blockScript.HealthScript.health.Val = _spawnRoutine.numberOfActionsLeft;
        }
        public void TransferOwner(GameObject otherBlock)
        {
            BlockBehaviour blockScript = otherBlock.GetComponent<BlockBehaviour>();
            blockScript.componentList.Add(this);
            transform.SetParent(otherBlock.transform, false);
        }
        private void OnDestroy()
        {
            if(panelsInRange == null)
            {
                return;
            }
            UnHighlightPanels();
            block.currentPanel.GetComponent<GridScripts.PanelBehaviour>().Occupied = false;
        }
        public void ResolveCollision(GameObject collision)
        {
            return;
        }
        private void Update()
        {
            if(!_playerAttached)
            {
                UpdateColor();
            }
            if(_blockTransporter == null && _playerAttached)
            {
                Vector3 position = new Vector3(transform.position.x, _blockTransporterRef.transform.position.y, transform.position.z);
                GameObject clone = Instantiate(_blockTransporterRef, position, _blockTransporterRef.transform.rotation);
                _blockTransporter = clone.GetComponent<BlockTransporterBehaviour>();
                _blockTransporter.owner = playerAttackScript.name;
                _blockTransporter.ownerTransform = transform;
                _blockTransporter._playerSpawnScript = _playerSpawnScript;
            }
        }
        public void ActivateDisplayMode()
        {
            gameObject.SetActive(false);
            _spawnRoutine.StopAllCoroutines();
            _spawnRoutine.enabled = false;
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            playerAttackScript = player;
            playerAttackScript.weaponUseAmount = playerUseAmount;
            _spawnRoutine.shouldStop = true;
            _spawnRoutine.StopAllCoroutines();
            transform.SetParent(player.transform, false);
            teleportBeam.transform.parent = null;
            teleportBeam.Teleport(player.transform.position);
            player.SetSecondaryWeapon(this, playerUseAmount);
            _playerAttached = true;
            transform.position += Vector3.up * 6;
        }

        public void ActivatePowerUp()
        {
            if(_blockTransporter.Deployed)
            {
                _blockTransporter.PlaceBlock();
            }
            else
            {
                playerAttackScript.IncreaseAmmuntion(1);
                _blockTransporter.moveDirection = playerAttackScript.transform.forward;
                _blockTransporter.Deploy();
            }
        }

        public void DetachFromPlayer()
        {
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
    
