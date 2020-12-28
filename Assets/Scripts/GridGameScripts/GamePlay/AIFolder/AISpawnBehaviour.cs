using System;
using System.Collections;
using System.Collections.Generic;
using GridGame.GamePlay.GridScripts;
using UnityEngine;

namespace GridGame.GamePlay.AI
{
	public class AISpawnBehaviour : MonoBehaviour
	{
		private AIMovementBehaviour _moveScript;
		[SerializeField]
		private PanelBehaviour _testGoal;
		private PlayerSpawnBehaviour _spawnScript;
        private bool canBuild = true;
        public PlayerSpawnBehaviour PlayerSpawnScript
        {
            get
            {
                return _spawnScript;
            }
        }

        // Use this for initialization
        void Start()
		{
			
		}

		private void Awake()
		{
			_moveScript = GetComponent<AIMovementBehaviour>();
			_spawnScript = GetComponent<PlayerSpawnBehaviour>();
		}

		public void SelectBlock(int choice)
		{
			
			switch (choice)
			{
				case 0:
				{
					_spawnScript.SelectBlock0();
					break;
				}
				case 1:
				{
					_spawnScript.SelectBlock1();
					break;
				}
				case 2:
				{
					_spawnScript.SelectBlock2();
					break;
				}
				case 3:
				{
					_spawnScript.SelectBlock3();
					break;
				}
			}
		}
        private IEnumerator DisableBuilding()
        {

            canBuild = false;
            yield return new WaitForSeconds(.1f);
            canBuild = true;
        }
        public void DisableBuild()
        {
            if(canBuild)
            {
                StartCoroutine(DisableBuilding());
            }
            
        }
		public bool Build(int blockIndex, PanelBehaviour buildPanel)
		{
            if (buildPanel == null || canBuild == false)
            {
                Debug.Log("Build at null");
                return false;
            }
            //Check if building at oanel is possible
            if(_spawnScript.Blocks.Count<=0 || buildPanel.BlockCapacityReached || buildPanel.IsBroken)
            {
                if(buildPanel.CurrentBlock != null)
                {
                    if (buildPanel.CurrentBlock.CurrentLevel == 3)
                    {
                        return false;
                    }
                }
                return false;
            }
            //select block type
			SelectBlock(blockIndex);
            Dictionary<string, PanelBehaviour> availableBuildPanels;
            //Looks for all possible building vantage points
            if(name == "Player1")
            { 
                BlackBoard.p1PanelList.FindNeighborsForPanel(buildPanel, out availableBuildPanels);
            }
            else
            {
                BlackBoard.p2PanelList.FindNeighborsForPanel(buildPanel, out availableBuildPanels);
            }
            //Checks to see the direction the vantage point is in relation to the building spot
            if(availableBuildPanels.ContainsKey("Forward"))
            {
                if(availableBuildPanels["Forward"].CurrentBlock == null)
                {
                    _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                    _moveScript.onArrival += _spawnScript.PlaceBlockLeft;
                    _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                    _moveScript.onArrival += DisableBuild;
                    _moveScript.MoveToPanel(availableBuildPanels["Forward"]);
                    return true;
                }
                
            }
            if(availableBuildPanels.ContainsKey("Behind"))
            {
                if (availableBuildPanels["Behind"].CurrentBlock == null)
                {
                    _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                    _moveScript.onArrival += _spawnScript.PlaceBlockRight;
                    _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                    _moveScript.onArrival += DisableBuild;
                    _moveScript.MoveToPanel(availableBuildPanels["Behind"]);
                    return true;
                }
            }
            if(availableBuildPanels.ContainsKey("Above"))
            {
                if (availableBuildPanels["Above"].CurrentBlock == null)
                {
                    _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                    _moveScript.onArrival += _spawnScript.PlaceBlockBelow;
                    _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                    _moveScript.onArrival += DisableBuild;
                    _moveScript.MoveToPanel(availableBuildPanels["Above"]);
                    return true;
                }
            }
            if(availableBuildPanels.ContainsKey("Below"))
            {
                if (availableBuildPanels["Below"].CurrentBlock == null)
                {
                    _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                    _moveScript.onArrival += _spawnScript.PlaceBlockUp;
                    _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                    _moveScript.onArrival += DisableBuild;
                    _moveScript.MoveToPanel(availableBuildPanels["Below"]);
                    return true;
                }
            }
            return false;
		}
        public bool Delete(PanelBehaviour deletePanel)
        {
            //Check if building at oanel is possible
            if (deletePanel.IsBroken)
            {
                return false;
            }
            //select block type
            _spawnScript.EnableDeletion();
            Dictionary<string, PanelBehaviour> availableBuildPanels;
            //Looks for all possible building vantage points
            if (name == "Player1")
            {
                BlackBoard.p1PanelList.FindNeighborsForPanel(deletePanel, out availableBuildPanels);
            }
            else
            {
                BlackBoard.p2PanelList.FindNeighborsForPanel(deletePanel, out availableBuildPanels);
            }
            //Checks to see the direction the vantage point is in relation to the building spot
            if (availableBuildPanels.ContainsKey("Forward"))
            {
                _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                _moveScript.onArrival += _spawnScript.PlaceBlockLeft;
                _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                _moveScript.onArrival += _spawnScript.DisableDeletion;
                _moveScript.MoveToPanel(availableBuildPanels["Forward"]);
                return true;
            }
            else if (availableBuildPanels.ContainsKey("Behind"))
            {
                _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                _moveScript.onArrival += _spawnScript.PlaceBlockRight;
                _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                _moveScript.onArrival += _spawnScript.DisableDeletion;
                _moveScript.MoveToPanel(availableBuildPanels["Behind"]);
                return true;
            }
            else if (availableBuildPanels.ContainsKey("Above"))
            {
                _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                _moveScript.onArrival += _spawnScript.PlaceBlockBelow;
                _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                _moveScript.onArrival += _spawnScript.DisableDeletion;
                _moveScript.MoveToPanel(availableBuildPanels["Above"]);
                return true;
            }
            else if (availableBuildPanels.ContainsKey("Below"))
            {
                _moveScript.onArrival = new UnityEngine.Events.UnityAction(_spawnScript.FindNeighbors);
                _moveScript.onArrival += _spawnScript.PlaceBlockRight;
                _moveScript.onArrival += _moveScript.playerMoveScript.EnableMovement;
                _moveScript.onArrival += _spawnScript.DisableDeletion;
                _moveScript.MoveToPanel(availableBuildPanels["Above"]);
                return true;
            }
            return false;
        }
    }
}
