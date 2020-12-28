using System;
using GridGame.GamePlay.GridScripts;
using UnityEngine;
using GridGame.VariableScripts;
using System.Collections.Generic;
using System.Collections;

namespace GridGame.GamePlay.AI
{
	public class AIControllerBehaviour : MonoBehaviour
	{
		private BinaryTreeBehaviour _decisionTree;
		private AIMovementBehaviour _moveScript;
        private AISpawnBehaviour _spawnScript;
        private Condition CoreDefenseCheckDelegate;
        private Condition OpponentGridCheck;
        [SerializeField]
        private float _coreScore;
        [SerializeField]
        private float _supportScore;
        [SerializeField]
        private float _supportBlockScore;
        [SerializeField]
        private float _supportDefenseScore;
        [SerializeField]
        private float _blockHealthScore;
        [SerializeField]
        private float _blockAmountScore;
        [SerializeField]
        private float _totalScore;
        [SerializeField]
        private float _opponentCoreScore;
        [SerializeField]
        private float _opponentSupportScore;
        [SerializeField]
        private float _opponentSupportBlockScore;
        [SerializeField]
        private float _opponentSupportDefenseScore;
        [SerializeField]
        private float _opponentBlockHealthScore;
        [SerializeField]
        private float _opponentBlockAmountScore;
        [SerializeField]
        private float _opponentTotalScore;
        private List<int> yCoords;
        private bool isAttacking;
        private List<string> _priorityTypes;
        private RaycastHit _interactionRay;
        private PlayerAttackBehaviour _attackScript;
        [SerializeField]
        private float _shootDelay;
        [SerializeField]
        private HealthBehaviour healthScript;
        [SerializeField]
        private float supportScorePass;
        [SerializeField]
        private float coreScorePass;
        [SerializeField]
        private float supportDefScorePass;
        [SerializeField]
        private float gridScorePass;
        private List<BlockBehaviour> characterBlockList;
        private List<BlockBehaviour> opponentBlockList;
        private List<Vector2> defensePositions;
        private List<Vector2> supportDefensePositions;
        private List<Vector2> supportPositions;
        private PanelList characterPanelList;
        private PanelList opponentPanelList;
        private PanelBehaviour priorityPanel;
        private void Start()
		{
			_decisionTree = GetComponent<BinaryTreeBehaviour>();
			_moveScript = GetComponent<AIMovementBehaviour>();
            _spawnScript = GetComponent<AISpawnBehaviour>();
            _attackScript = GetComponent<PlayerAttackBehaviour>();

            _decisionTree.enabled = true;
            _moveScript.enabled = true;
            _spawnScript.enabled = true;

            CoreDefenseCheckDelegate += GetPanelsOnBackRow;
            yCoords = new List<int>() { 0, 1, 2, 3 };
            StartCoroutine(Shoot());
            InitializeLists();
        }

        public void InitializeLists()
        {
            //I can use this to initialize all list values instead of checking for which player in each func
            characterBlockList = new List<BlockBehaviour>();
            defensePositions = new List<Vector2>();
            characterPanelList = new PanelList();
            supportDefensePositions = new List<Vector2>();
            supportPositions = new List<Vector2>();
            //Check player tag
            if (name == "Player1")
            {
                characterBlockList = BlackBoard.p1Blocks;
                opponentBlockList = BlackBoard.p2Blocks;
                characterPanelList = BlackBoard.p1PanelList;
                opponentPanelList = BlackBoard.p2PanelList;
                defensePositions.Add(new Vector2(1, 0));
                defensePositions.Add(new Vector2(2, 0));
                defensePositions.Add(new Vector2(1, 1));
                defensePositions.Add(new Vector2(2, 1));
                supportDefensePositions.Add(new Vector2(2, 0));
                supportDefensePositions.Add(new Vector2(2, 1));
                supportPositions.Add(new Vector2(0, 1));
                supportPositions.Add(new Vector2(0, 0));
            }
            else
            {
                characterBlockList = BlackBoard.p2Blocks;
                opponentBlockList = BlackBoard.p1Blocks;
                characterPanelList = BlackBoard.p2PanelList;
                opponentPanelList = BlackBoard.p1PanelList;
                defensePositions.Add(new Vector2(8, 0));
                defensePositions.Add(new Vector2(7, 0));
                defensePositions.Add(new Vector2(7, 1));
                defensePositions.Add(new Vector2(8, 1));
                supportDefensePositions.Add(new Vector2(7, 0));
                supportDefensePositions.Add(new Vector2(7, 1));
                supportPositions.Add(new Vector2(9, 0));
                supportPositions.Add(new Vector2(9, 1));
            }
        }
        private float ResourceStatusCheck()
        {
            float totalDefenseBlocks = 0;
            float totalSupportBlocks = 0;
            
            //Check amount of support blocks
            foreach (BlockBehaviour block in characterBlockList)
            {
                foreach(string type in block.types)
                {
                    if (type == "Support")
                    {
                        totalSupportBlocks++;
                    }
                }
                
            }
            //check if there are defense blocks in front of support blocks
            foreach(var panelLocation in defensePositions)
            {
                PanelBehaviour currentPanel;
                if(characterPanelList == null)
                {
                    break;
                }
                if(characterPanelList.FindPanel(panelLocation,out currentPanel))
                {
                    if(currentPanel.CurrentBlock != null)
                    {
                        if(currentPanel.CurrentBlock.Type == "Defense")
                        {
                            totalDefenseBlocks++;
                        }
                    }
                }
            }
            _supportDefenseScore = (totalDefenseBlocks / 2f) * 100f;
            _supportBlockScore = (totalSupportBlocks / 2f) * 100f;
            if(_supportBlockScore == 0)
            {
                return 0;
            }
            return (_supportDefenseScore + _supportBlockScore) / 2f;
        }
        private float OpponentResourceStatusCheck()
        {
            float totalDefenseBlocks = 0;
            float totalSupportBlocks = 0;
            List<Vector2> supportBlockPositions = new List<Vector2>();
            //Check amount of support blocks
            foreach (BlockBehaviour block in opponentBlockList)
            {
                foreach (string type in block.types)
                {
                    if (type == "Support")
                    {
                        totalSupportBlocks++;
                        supportBlockPositions.Add(block.Panel.Position);
                    }
                }

            }
            //check if there are defense blocks in front of support blocks
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (block.types.Contains("Defense"))
                {
                    foreach(Vector2 position in supportBlockPositions)
                    {
                        if(position.y == block.Panel.Position.y)
                        {
                            totalDefenseBlocks++;
                        }
                    }
                    
                }
            }
            _supportDefenseScore = (totalDefenseBlocks / 2f) * 100f;
            _supportBlockScore = (totalSupportBlocks / 2f) * 100f;
            if (_supportBlockScore == 0)
            {
                return 0;
            }
            return (_supportDefenseScore + _supportBlockScore) / 2f;
        }
        private bool GetPanelsOnBackRow(object[] args)
        {
            PanelBehaviour panelScript = (PanelBehaviour)args[0];
            if(name == "Player1")
            {
                if(panelScript.Position.x == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if(name == "Player2")
            {
                if (panelScript.Position.x == 9)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        private bool GetPriorityPanels(object[] args)
        {
            PanelBehaviour panelScript =(PanelBehaviour)args[0]; 
            foreach(string type in _priorityTypes)
            {
                if (panelScript.CurrentBlock.types.Contains(type))
                {
                    return true;
                }
            }
            return false;
        }
        public void NotifyOfBulletHit(PanelBehaviour panelToBuild,string notifyType)
        {
            priorityPanel = panelToBuild;
            if(notifyType == "Core")
            {
                _coreScore -= 5;
            }
            else if(notifyType == "SupportDefense")
            {
                _supportDefenseScore -= 10;
            }
        }
        private float CoreDefenseCheck()
        {
            List<PanelBehaviour> panelsOnBack = new List<PanelBehaviour>();
            float CoreDefenseVal = 0f;
            
            if (characterPanelList != null)
            {
                characterPanelList.GetPanels(CoreDefenseCheckDelegate, out panelsOnBack);
            }

            foreach (var panel in panelsOnBack)
            {
                if (panel.Occupied && panel.CurrentBlock != null)
                {
                    CoreDefenseVal++;
                }
            }
            return (CoreDefenseVal / 4f) * 100;
        }
        private void BuildCoreDefense()
        {
            List<PanelBehaviour> panelsOnBack = new List<PanelBehaviour>();
            if(characterPanelList != null)
            {
                characterPanelList.GetPanels(CoreDefenseCheckDelegate, out panelsOnBack);
            }
            if(priorityPanel != null)
            {
                if(!priorityPanel.Occupied)
                {
                    _spawnScript.Build(1, priorityPanel);
                }
            }
            foreach(var panel in panelsOnBack)
            {
                if(!panel.Occupied && panel != priorityPanel)
                {
                    _spawnScript.Build(1, panel);
                }
            }
        }
        private void BuildSupportDefense()
        {
            PanelBehaviour buildPanel;
            int buildCounter =0;

            if (characterPanelList == null)
            {
                return;
            }
            foreach (var position in supportDefensePositions)
            {
                if (characterPanelList.FindPanel(position, out buildPanel) && buildCounter < 2)
                {
                    if(buildPanel.Occupied && CheckIfOpponentAttackingPanel(buildPanel))
                    {
                        buildPanel = Aim((int)buildPanel.Position.y);
                        _spawnScript.Build(1, buildPanel);
                    }
                    else if (_spawnScript.Build(1, buildPanel))
                    {
                        buildCounter++;
                    }
                }
               
            }
        }
        private bool CheckIfOpponentAttackingPanel(PanelBehaviour panel)
        {
            foreach(BlockBehaviour block in opponentBlockList)
            {
                if(block.types.Contains("Attack") && block.Panel.Position.y == panel.Position.y)
                {
                    return true;
                }
            }
            return false;
        }
        private void BuildSupport()
        {
            PanelBehaviour buildPanel;
            foreach (var position in supportPositions)
            {
                if (characterPanelList != null)
                {
                    if (characterPanelList.FindPanel(position, out buildPanel))
                    {
                        _spawnScript.Build(2, buildPanel);
                    }
                }
            }
        }
        private PanelBehaviour FindLocationOfWeakBlock(string Name,out string blocktype)
        {
            PanelBehaviour desiredLocation = null;
            BlockBehaviour temp = new BlockBehaviour();
            blocktype = "";
            if (Name == "Player1" && BlackBoard.p1Blocks.Count>0)
            {
                temp = BlackBoard.p1Blocks[0];
                desiredLocation = temp.Panel;
                blocktype = BlackBoard.p1Blocks[0].Type;
                foreach (BlockBehaviour block in BlackBoard.p1Blocks)
                {
                    if (block.types.Contains("Support"))
                    {
                        continue;
                    }
                    if (temp.Type== "Support")
                    {
                        temp = block;
                        desiredLocation = temp.Panel;
                        blocktype = block.Type;
                    }
                    
                    if ( block.HealthScript.health.Val < temp.HealthScript.health.Val)
                    {
                        temp = block;
                        desiredLocation = temp.Panel;
                        blocktype = block.Type;
                    }
                }
            }
            else if(Name == "Player2" && BlackBoard.p2Blocks.Count > 0)
            {
                temp = BlackBoard.p2Blocks[0];
                desiredLocation = temp.Panel;
                blocktype = BlackBoard.p2Blocks[0].Type;
                foreach (BlockBehaviour block in BlackBoard.p2Blocks)
                {
                    if (block.types.Contains("Support"))
                    {
                        continue;
                    }
                    if (temp.Type == "Support")
                    {
                        temp = block;
                        desiredLocation = temp.Panel;
                        blocktype = block.Type;
                    }
                    if (block.HealthScript.health.Val < temp.HealthScript.health.Val)
                    {
                        temp = block;
                        desiredLocation = temp.Panel;
                        blocktype = block.Type;
                    }
                }
            }
            return desiredLocation;
        }
        private IEnumerator Shoot()
        {
            yield return new WaitForSecondsRealtime(3);
            while (gameObject != null)
            {
                int layerMask = 1 << 9;
                if (Physics.Raycast(transform.position, transform.forward, out _interactionRay, 50,layerMask))
                {
                    if(_interactionRay.collider.gameObject.CompareTag("Barrier"))
                    {
                        GridGame.Interfaces.IUpgradable barrier = _interactionRay.collider.gameObject.GetComponent<GridGame.Interfaces.IUpgradable>();
                        if(barrier.block.owner.name != name)
                        {
                            _attackScript.FireGun();
                        }
                    }
                    else
                    {
                        BlockBehaviour blockScript = _interactionRay.collider.gameObject.GetComponent<BlockBehaviour>();
                        if (blockScript != null)
                        {
                            if (blockScript.owner.name != name)
                            {
                                _attackScript.FireGun();
                            }
                        }
                        else
                        {
                            _attackScript.FireGun();
                        }
                    }
                    
                }
                yield return new WaitForSeconds(_shootDelay);
            }
        }
        private void UpgradeBlock()
        {
            string blockType;
            PanelBehaviour desiredLocation = FindLocationOfWeakBlock(name,out blockType);
            if(!desiredLocation)
            {
                Debug.Log("Wanted to upgrade blocks, but I cant find any");
                return;
            }
            switch (blockType)
            {
                case("Attack"):
                    {
                        if (_spawnScript.Build(0, desiredLocation) == false && desiredLocation.CurrentBlock.types.Contains("Support") == false)
                        {
                            if(desiredLocation.BlockCapacityReached)
                            {
                                _spawnScript.Delete(desiredLocation);
                            }
                        }
                        break;
                    }
                case ("Defense"):
                    {
                        if (_spawnScript.Build(1, desiredLocation) ==false && desiredLocation.CurrentBlock.types.Contains("Support") == false)
                        {
                            if (desiredLocation.BlockCapacityReached)
                            {
                                _spawnScript.Delete(desiredLocation);
                            }
                        }
                        break;
                    }
                //case ("Support"):
                //    {
                //        if (_spawnScript.Build(2, desiredLocation) == false)
                //        {
                //            _spawnScript.Delete(desiredLocation);
                //        }
                //        break;
                //    }
            }
        }
        private void AttackOpponentOpening()
        {
            if(isAttacking)
            {
                return;
            }
            isAttacking = true;
            PanelBehaviour target = Aim();
            _spawnScript.Build(0, target);
            isAttacking = false;
            yCoords = new List<int>() { 0, 1, 2, 3 };
        }
        private void AttackOpponentWeakSpot()
        {
            if(isAttacking)
            {
                return;
            }
            isAttacking = true;
            int weakYCoord;
            string blockType;
            PanelBehaviour desiredLocation = null;
            if (name == "Player1")
            {
                desiredLocation = FindLocationOfWeakBlock("Player2", out blockType);
                if(!desiredLocation)
                {
                    Debug.Log("Opponent has no weak defenses");
                    isAttacking = false;
                    return;
                }
                weakYCoord = (int)desiredLocation.Position.y;
            }
            else
            {
                desiredLocation = FindLocationOfWeakBlock("Player1", out blockType);
                if (!desiredLocation)
                {
                    Debug.Log("Opponent has no weak defenses");
                    isAttacking = false;
                    return;
                }
                weakYCoord = (int)desiredLocation.Position.y;
            }
            //if(yCoords.Count == 0)
            //{
            //    yCoords.Add(weakYCoord);
            //}
            //else
            //{
            //    yCoords[0] = weakYCoord;
            //}
            PanelBehaviour target = Aim(weakYCoord);
            _spawnScript.Build(0, target);
            isAttacking = false;
            yCoords = new List<int>() { 0, 1, 2, 3 };
        }
        private float GradeFriendlyGrid()
        {
            
            HealthBehaviour characterCoreHealth = new HealthBehaviour();
            if(name == "Player1")
            {
                characterCoreHealth = BlackBoard.p1Core.GetComponent<HealthBehaviour>();
            }
            else
            {
                characterCoreHealth = BlackBoard.p2Core.GetComponent<HealthBehaviour>();
            }
            if(characterPanelList != null)
            {
                _blockAmountScore = ((float)characterBlockList.Count / (float)characterPanelList.Count) * 100f;
            }
            float totalBlockHealth = 0;
            float currentBlockHealth = 0;
            foreach(BlockBehaviour block in characterBlockList)
            {
                if(block.types.Contains("Support"))
                {
                    continue;
                }
                totalBlockHealth += block.HealthScript.HealthRef.Val;
            }
            foreach (BlockBehaviour block in characterBlockList)
            {
                if (block.types.Contains("Support"))
                {
                    continue;
                }
                currentBlockHealth += block.HealthScript.health.Val;
            }
            if(characterCoreHealth.health.Val < characterCoreHealth.health.Val/2)
            {
                coreScorePass = 50;
            }
            _blockHealthScore = (currentBlockHealth / totalBlockHealth) * 100f;
            _coreScore = ((((float)characterCoreHealth.health.Val / 200f) * 100f) + CoreDefenseCheck())/2;
            _supportScore= ResourceStatusCheck();
            _totalScore = (_blockAmountScore + _blockHealthScore+_coreScore+_supportScore) / 4f;
            return _totalScore;
        }
        private float GradeOpponentGrid()
        {

            HealthBehaviour opponentCoreHealth = new HealthBehaviour();
            List<BlockBehaviour> opponentBlockList = new List<BlockBehaviour>();
            if (name == "Player1")
            {
                opponentCoreHealth = BlackBoard.p2Core.GetComponent<HealthBehaviour>();
                opponentBlockList = BlackBoard.p2Blocks;
            }
            else
            {
                opponentCoreHealth = BlackBoard.p1Core.GetComponent<HealthBehaviour>();
                opponentBlockList = BlackBoard.p1Blocks;
            }
            if (opponentPanelList != null)
            {
                _opponentBlockAmountScore = ((float)opponentBlockList.Count / (float)opponentPanelList.Count) * 100f;
            }
            float totalBlockHealth = 0;
            float currentBlockHealth = 0;
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (block.types.Contains("Support"))
                {
                    continue;
                }
                totalBlockHealth += block.HealthScript.HealthRef.Val;
            }
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (block.types.Contains("Support"))
                {
                    continue;
                }
                currentBlockHealth += block.HealthScript.health.Val;
            }
            if(totalBlockHealth == 0)
            {
                _opponentBlockHealthScore = 0;
            }
            else
            {
                _opponentBlockHealthScore = (currentBlockHealth / totalBlockHealth) * 100f;
            }
            _opponentCoreScore = ((((float)opponentCoreHealth.health.Val / 200f) * 100f) + GradeOpponentOpenings()) / 2;
            _opponentSupportScore = OpponentResourceStatusCheck();
            _opponentTotalScore = (_opponentBlockAmountScore + _opponentBlockHealthScore + _opponentCoreScore + _opponentSupportScore) / 4f;
            return _opponentTotalScore;
        }
        private PanelBehaviour Aim()
        {
            PanelBehaviour targetPanel = new PanelBehaviour();
            Vector2 location = new Vector2();
            
            if (name == "Player1")
            {
                location.x = 4;
            }
            else
            {
                location.x = 5;
            }
            if(yCoords.Count == 0)
            {
                Debug.Log("Can't Aim! No valid spots!");
                return new PanelBehaviour();
            }
            foreach (BlockBehaviour block in characterBlockList)
            {
                if (yCoords[0] == block.Panel.Position.y)
                {
                    if (name == "Player1")
                    {
                        if (block.Panel.Position.x + 1 > 4)
                        {
                            location.x = block.Panel.Position.x;
                        }
                        else if (block.Panel.Position.x > location.x)
                        {
                            location.x = block.Panel.Position.x + 1;
                        }
                    }
                    else
                    {
                        if (block.Panel.Position.x - 1 < 5)
                        {
                            location.x = block.Panel.Position.x;
                        }
                        else if(block.Panel.Position.x < location.x)
                        {
                            location.x = block.Panel.Position.x - 1;
                        }

                    }

                }
            }
            location.y = yCoords[0];
            characterPanelList.FindPanel(location, out targetPanel);
            return targetPanel;
        }
        private PanelBehaviour Aim(int yCoordIndex)
        {
            PanelBehaviour targetPanel = new PanelBehaviour();
            Vector2 location = new Vector2();

            if (name == "Player1")
            {
                location.x = 1;
            }
            else
            {
                location.x = 8;
            }
            foreach (BlockBehaviour block in characterBlockList)
            {
                if (yCoordIndex == block.Panel.Position.y)
                {
                    if (name == "Player1")
                    {
                        if (block.Panel.Position.x + 1 > 4)
                        {
                            location.x = block.Panel.Position.x;
                        }
                        else if(block.Panel.Position.x + 1 > location.x)
                        {
                            location.x = block.Panel.Position.x + 1;
                        }
                    }
                    else
                    {
                        if (block.Panel.Position.x - 1 < 5)
                        {
                            location.x = block.Panel.Position.x;
                        }
                        else if(block.Panel.Position.x - 1 < location.x)
                        {
                            location.x = block.Panel.Position.x - 1;
                        }

                    }

                }
            }
            location.y = yCoordIndex;
            characterPanelList.FindPanel(location, out targetPanel);
            return targetPanel;
        }
        private float GradeOpponentOpenings()
        {
            List<int> opponentYCoords = new List<int> { 0, 1, 2, 3 };
            List<BlockBehaviour> opponentBlockList = new List<BlockBehaviour>();
            PanelList opponentPanelList = new PanelList();
            List<BlockBehaviour> attackBlocks = new List<BlockBehaviour>();
            float score =0;
            if (name == "Player1")
            {
                opponentBlockList = BlackBoard.p2Blocks;
                opponentPanelList = BlackBoard.p2PanelList;
            }
            else
            {
                opponentBlockList = BlackBoard.p1Blocks;
                opponentPanelList = BlackBoard.p1PanelList;
            }

            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (opponentYCoords.Contains((int)block.Panel.Position.y))
                {
                    score++;
                }
            }
            return (score/4) * 100;
        }
        private bool FindOpponentOpenings()
        {
            yCoords = new List<int>() { 0, 1, 2, 3 };
            List<BlockBehaviour> opponentBlockList = new List<BlockBehaviour>();
            PanelList opponentPanelList = new PanelList();
            List<BlockBehaviour> attackBlocks = new List<BlockBehaviour>();
            if (name == "Player1")
            {
                opponentBlockList = BlackBoard.p2Blocks;
                opponentPanelList = BlackBoard.p2PanelList;
            }
            else
            {
                opponentBlockList = BlackBoard.p1Blocks;
                opponentPanelList = BlackBoard.p1PanelList;
            }
            
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if(yCoords.Contains((int)block.Panel.Position.y))
                {
                    yCoords.Remove((int)block.Panel.Position.y);
                }
            }
            foreach(BlockBehaviour block in  characterBlockList)
            {
                if (yCoords.Contains((int)block.Panel.Position.y) && block.types.Contains("Attack"))
                {
                    yCoords.Remove((int)block.Panel.Position.y);
                }
            }
            if(yCoords.Count == 0)
            {
                _decisionTree.Decisions.SetCondition("OpponentOpeningCheck", true);
                return true;
            }
            else
            {
                _decisionTree.Decisions.SetCondition("OpponentOpeningCheck", false);
                return false;
            }
        }
        private bool OpponentAttackBlockCheck()
        {
            List<BlockBehaviour> opponentBlockList = new List<BlockBehaviour>();
            PanelList opponentPanelList = new PanelList();
            yCoords.Clear();
            if (name == "Player1")
            {
                opponentBlockList = BlackBoard.p2Blocks;
                opponentPanelList = BlackBoard.p2PanelList;
            }
            else
            {
                opponentBlockList = BlackBoard.p1Blocks;
                opponentPanelList = BlackBoard.p1PanelList;
            }
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (block.types.Contains("Attack"))
                {
                    yCoords.Add((int)block.Panel.Position.y);
                }
            }
            foreach (BlockBehaviour block in characterBlockList)
            {
                if (yCoords.Contains((int)block.Panel.Position.y) && block.types.Contains("Attack"))
                {
                    yCoords.Remove((int)block.Panel.Position.y);
                }
            }
            if (yCoords.Count == 0)
            {
                _decisionTree.Decisions.SetCondition("OpponentAttackBlockCheck", true);
                return true;
            }
            else
            {
                _decisionTree.Decisions.SetCondition("OpponentAttackBlockCheck", false);
                return false;
            }
        }
        private void AttackPriorityBlocks()
        {
            if (isAttacking)
            {
                return;
            }
            isAttacking = true;
            for(int i =0; i< yCoords.Count;i++)
            {
                PanelBehaviour target = Aim(yCoords[i]);
                if(target.BlockCapacityReached)
                {
                    _spawnScript.Delete(target);
                }
                _spawnScript.Build(0, target);
            }
            isAttacking = false;
            yCoords = new List<int>() { 0, 1, 2, 3 };
        }
        private bool OpponentSupportBlockCheck()
        {
            List<BlockBehaviour> opponentBlockList = new List<BlockBehaviour>();
            PanelList opponentPanelList = new PanelList();
            yCoords.Clear();
            if (name == "Player1")
            {
                opponentBlockList = BlackBoard.p2Blocks;
                opponentPanelList = BlackBoard.p2PanelList;
            }
            else
            {
                opponentBlockList = BlackBoard.p1Blocks;
                opponentPanelList = BlackBoard.p1PanelList;
            }
            foreach (BlockBehaviour block in opponentBlockList)
            {
                if (block.types.Contains("Support"))
                {
                    yCoords.Add((int)block.Panel.Position.y);
                }
            }
            foreach(BlockBehaviour block in characterBlockList)
            {
                if (yCoords.Contains((int)block.Panel.Position.y) && block.types.Contains("Attack"))
                {
                    yCoords.Remove((int)block.Panel.Position.y);
                }
            }
            if (yCoords.Count == 0)
            {
                _decisionTree.Decisions.SetCondition("OpponentSupportBlockCheck", true);
                return true;
            }
            else
            {
                _decisionTree.Decisions.SetCondition("OpponentSupportBlockCheck", false);
                return false;
            }
        }
        private void Update()
		{
            if (healthScript.health.Val < healthScript.HealthRef.Val * .50)
            {
                SendMessage("StopMaterialLoss");
                if(_spawnScript.PlayerSpawnScript.overdriveEnabled)
                {
                    _decisionTree.Decisions.SetCondition("GridStatusCheck", GradeFriendlyGrid() >= 60);
                    _decisionTree.Decisions.SetCondition("CoreStatusCheck", _coreScore >= 50);
                    _decisionTree.Decisions.SetCondition("SupportStatusCheck", _supportScore == 100);
                    _decisionTree.Decisions.SetCondition("SupportDefenseCheck", _supportDefenseScore == 100);
                    return;
                }
            }
            
            _decisionTree.Decisions.SetCondition("GridStatusCheck", GradeFriendlyGrid() > gridScorePass);
            _decisionTree.Decisions.SetCondition("CoreStatusCheck", _coreScore>coreScorePass);
            _decisionTree.Decisions.SetCondition("SupportStatusCheck", _supportScore == supportScorePass);
            _decisionTree.Decisions.SetCondition("SupportDefenseCheck", _supportDefenseScore == supportDefScorePass);
            
        }
	}
}
