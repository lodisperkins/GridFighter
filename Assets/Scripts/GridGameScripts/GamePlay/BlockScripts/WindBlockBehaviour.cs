using GridGame.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame.GamePlay
{
    
    public class WindBlockBehaviour : MonoBehaviour,GridGame.Interfaces.IUpgradable {
        [SerializeField]
        private float windForce;
        private Vector3 velocity;
        private GridScripts.PanelBehaviour currentPanel;
        public List<GridScripts.PanelBehaviour> panelsInRange;
        public List<GameObject> blocksInRange;
        [SerializeField]
        private BlockBehaviour _block;
        private GameObject blockInWind;
        private RaycastHit _windRay;
        private void Start()
        {
            panelsInRange = new List<GridScripts.PanelBehaviour>();
            currentPanel = block.currentPanel.GetComponent<GridScripts.PanelBehaviour>();
            _windRay = new RaycastHit();
        }
        public BlockBehaviour block
        {
            get
            {
                return _block;
            }

            set
            {
                _block = value;
            }
        }
        public bool InRange(float min, float max, float Val)
        {
            if(Val <= max && Val >= min)
            {
                return true;
            }
            return false;
        }
        public bool CheckIfAllPanelsFound()
        {
            
            if(!InRange(1,8,panelsInRange[panelsInRange.Count-1].Position.x))
            {
                blockInWind.gameObject.AddComponent<SeekBehaviour>();
                blockInWind.gameObject.GetComponent<SeekBehaviour>().Init(FindSafePanel(blockInWind.transform.position), velocity, (int)windForce, 0.1f, true);
                return true;
            }
            if (!InRange(1, 2, panelsInRange[panelsInRange.Count - 1].Position.y))
            {
                blockInWind.gameObject.AddComponent<SeekBehaviour>();
                blockInWind.gameObject.GetComponent<SeekBehaviour>().Init(FindSafePanel(blockInWind.transform.position), velocity, (int)windForce, 0.1f, true);
                return true;
            }
            return false;
        }
        GameObject GridGame.Interfaces.IUpgradable.specialFeature
        {
            get
            {
                return specialFeature;
            }
        }

        public string Name
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        public Color displayColor
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

        public bool CanBeHeld
        {
            get
            {
                throw new System.NotImplementedException();
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

        public GameObject specialFeature;

        public void ResolveCollision(GameObject other)
        {
            
        }

        public void TransferOwner(GameObject otherBlock)
        {
            return;
        }
        public Vector3 FindSafePanel(Vector3 OtherPosition)
        {
            GridScripts.PanelBehaviour safePanel = new GridScripts.PanelBehaviour();
            float lastDisplacementVal =0;
            if(panelsInRange.Count == 3)
            {
                if(panelsInRange[1].Occupied)
                {
                    safePanel = blockInWind.GetComponent<BlockBehaviour>().Panel;
                    return new Vector3(safePanel.XAbsolute, OtherPosition.y, safePanel.ZAbsolute);
                }
            }
            if(panelsInRange[panelsInRange.Count-1].CurrentBlock != null)
            {
                if(panelsInRange[panelsInRange.Count - 1].CurrentBlock.gameObject ==blockInWind)
                {
                    safePanel = panelsInRange[panelsInRange.Count - 1];
                    return new Vector3(safePanel.XAbsolute, OtherPosition.y, safePanel.ZAbsolute);
                    
                }
            }
            foreach(var panel in panelsInRange)
            {
                if(panel.Occupied)
                {
                    continue;
                }
                float currentDisplacementVal = Vector2.Distance(currentPanel.Position,panel.Position);
                if (currentDisplacementVal >= lastDisplacementVal )
                {
                    lastDisplacementVal = currentDisplacementVal;
                    safePanel = panel;
                }
            }
            safePanel.Occupied = true;
            Vector3 newPosition = new Vector3(safePanel.XAbsolute, OtherPosition.y, safePanel.ZAbsolute);
            return newPosition;
        }
        public void UpgradeBlock(GameObject otherBlock)
        {
            return;
        }
        public void FindPanels()
        {
            panelsInRange.Clear();
            RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, transform.forward, 6);
            foreach(var hit in raycastHits)
            {
                GameObject currentObject= hit.transform.gameObject;
                if (currentObject.CompareTag("Panel"))
                {
                    GridScripts.PanelBehaviour panel = currentObject.GetComponent<GridScripts.PanelBehaviour>();
                    if(panel.CurrentBlock != gameObject)
                    {
                        panelsInRange.Add(panel);
                    }
                }
            }
        }
        public void FindBlock()
        {
            int layerMask = 1 << 9;
            if (Physics.Raycast(transform.position, transform.forward, out _windRay, 6, layerMask))
            {
                GameObject currentObject = _windRay.transform.gameObject;
                if (currentObject.CompareTag("Block"))
                {
                    if (currentObject != gameObject)
                    {
                        blockInWind = currentObject;
                        PushBlock();
                    }
                }
            }
        }
        public void PushBlock()
        {
            switch (panelsInRange.Count)
            {
                case (2):
                    {
                        GridScripts.PanelBehaviour safePanel = panelsInRange[1];
                        if(blockInWind.GetComponent<BlockBehaviour>().Panel == safePanel || panelsInRange[1].Occupied)
                        {
                            break;
                        }
                        Vector3 safeSpot = new Vector3(safePanel.XAbsolute, blockInWind.transform.position.y, safePanel.ZAbsolute);
                        if (safeSpot.x != 0 || safeSpot.z != 0)
                        {
                            blockInWind.AddComponent<SeekBehaviour>();
                            blockInWind.GetComponent<SeekBehaviour>().Init(safeSpot, velocity, (int)windForce, 0.1f, true);
                            blockInWind.GetComponent<BlockBehaviour>().shakeScript.isMoving = true;
                            blockInWind.GetComponent<BlockBehaviour>().inMotion = true;
                        }
                        break;
                    }
                case (3):
                    {
                        GridScripts.PanelBehaviour safePanel;
                        if(blockInWind.GetComponent<BlockBehaviour>().Panel == panelsInRange[2])
                        {
                            break;
                        }
                        if (panelsInRange[2].Occupied == false && panelsInRange[1].Occupied == false && panelsInRange[2].CurrentBlock != blockInWind)
                        {
                            safePanel = panelsInRange[2];
                        }
                        else if(panelsInRange[2].Occupied == false && panelsInRange[0].Occupied == false)
                        {
                            safePanel = panelsInRange[2];
                        }
                        else if(panelsInRange[1].Occupied == false)
                        {
                            safePanel = panelsInRange[1];
                        }
                        else
                        {
                            break;
                        }
                        Vector3 safeSpot = new Vector3(safePanel.XAbsolute, blockInWind.transform.position.y, safePanel.ZAbsolute);
                        if (safeSpot.x != 0 || safeSpot.z != 0)
                        {
                            if (blockInWind.GetComponent<BlockBehaviour>().Panel != safePanel)
                            {
                                blockInWind.AddComponent<SeekBehaviour>();
                                blockInWind.GetComponent<SeekBehaviour>().Init(safeSpot, velocity, (int)windForce, 0.1f, true);
                                blockInWind.GetComponent<BlockBehaviour>().shakeScript.isMoving = true;
                                blockInWind.GetComponent<BlockBehaviour>().shakeScript.isMoving = true;
                            }
                                
                        }
                        break;
                    }
            }

        }
        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Block"))
            {
                Vector2 direction2D = new Vector2(transform.forward.z, transform.forward.x);
                other.GetComponent<Movement.GridPhysicsBehaviour>().AddForce(direction2D* 3);
            }
        }
        public void ActivateDisplayMode()
        {
            return;
        }
        private void Update()
        {
            
        }

        public void UpgradePlayer(PlayerAttackBehaviour player)
        {
            throw new System.NotImplementedException();
        }

        public void ActivatePowerUp()
        {
            throw new System.NotImplementedException();
        }

        public void DetachFromPlayer()
        {
            throw new System.NotImplementedException();
        }

        public void Stun()
        {
            throw new System.NotImplementedException();
        }

        public void Unstun()
        {
            throw new System.NotImplementedException();
        }

        public void DeactivatePowerUp()
        {
            throw new System.NotImplementedException();
        }
    }
}

