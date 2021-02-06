using System;
using System.Collections;
using System.Collections.Generic;
using GridGame;
using GridGame.GamePlay;
using GridGame.GamePlay.GridScripts;
using UnityEngine;

public class MortarBlockBehaviour : MonoBehaviour
{
	//the grid space
	[SerializeField] private GridBehaviour _grid;
	//the block that this component is attached to
	private BlockBehaviour _block;
	//This will spawn bullets over the opponents panel
	[SerializeField]
	private GameObject _bulletEmitter;
	private PanelBehaviour _targetPanel;
	[SerializeField]
	private Vector3 _bulletEmitterPosition;
	private PanelBehaviour _firstPanelFound;
	private int _yPosition;
	private Vector2 _aimOffSet;
	private Vector3 _targetPosition;
    [SerializeField] private GunBehaviour _gun;
	// Use this for initialization
	void Start () {
		_block = GetComponent<BlockBehaviour>();
        if (_block.owner == null)
        {
            _bulletEmitter.GetComponent<GunBehaviour>().StopAllCoroutines();
            return;
        }
    }
	//This long piece of code is used to aim at the opponent panel regardless of how the field changes
	//There are still some instances where this will not work. In which case the first or last panel
	//in the opponent list is fired at
	public void AimAtOpponentPanelP1()
	{
		//First find the panel that shares the same y coordinate on the grid
		_yPosition = (int)_block.Panel.Position.y;
		for (int i = 0; i < _grid.CountP2; i++)
		{
			 _firstPanelFound = _grid.GetPanelFromP2List(i);
			int targetPositionY = (int)_firstPanelFound.Position.y;
			if (targetPositionY != _yPosition)
			{
				_firstPanelFound = null;
				continue;
			}
			break;
		}
		//checks to see if a panel was found
		if (_firstPanelFound == null)
		{
			return;
		}
		//If a panel is found, get the current panels position and the y posoition of the panel that was found
		int fp = (int)_firstPanelFound.Position.x;
		int cp = (int)_block.Panel.Position.x;
		int yPos = (int)_block.Panel.Position.y;
		//With this information, solve to find the target position
		int tp = fp + (fp - cp) - 1;
		//if the target position is greater than the highest possible x coordinate set the target to be the 
		//clamp the targets x to be the greatest x coordinate
		if (tp > 9)
		{
			tp = 9;
		}
		//if the target position is less than the lowest possible x coordinate, clamp the targets position
		//to the lowest possible x cooridinate
		else if(tp < 0)
		{
			tp = 0;
		}
		//look to see if the target position exists in the opponent list
		int target= _grid.GetIndexFromP2List(new Vector2(tp, yPos));
		//if the desired target doesn't exist in the list, fire at the first opponent panel
		if (target == -1)
		{
			target = 0;
		}
		//find target panel and set target position
		_targetPanel = _grid.GetPanelFromP2List(target);
		_targetPosition = _targetPanel.transform.position + _bulletEmitterPosition;
	}
    public void AimAtOpponentPanelP2()
    {
        //First find the panel that shares the same y coordinate on the grid
        _yPosition = (int)_block.Panel.Position.y;
        for (int i = 0; i < _grid.CountP1; i++)
        {
            _firstPanelFound = _grid.GetPanelFromP1List(i);
            int targetPositionY = (int)_firstPanelFound.Position.y;
            if (targetPositionY != _yPosition)
            {
                _firstPanelFound = null;
                continue;
            }
            break;
        }
        //checks to see if a panel was found
        if (_firstPanelFound == null)
        {
            return;
        }
        //If a panel is found, get the current panels position and the y posoition of the panel that was found
        int fp = (int)_firstPanelFound.Position.x;
        int cp = (int)_block.Panel.Position.x;
        int yPos = (int)_block.Panel.Position.y;
        //With this information, solve to find the target position
        int tp = fp + (fp - cp) + 1;
        //if the target position is greater than the highest possible x coordinate set the target to be the 
        //clamp the targets x to be the greatest x coordinate
        if (tp > 9)
        {
            tp = 9;
        }
        //if the target position is less than the lowest possible x coordinate, clamp the targets position
        //to the lowest possible x cooridinate
        else if (tp < 0)
        {
            tp = 0;
        }
        //look to see if the target position exists in the opponent list
        int target = _grid.GetIndexFromP1List(new Vector2(tp, yPos));
        //if the desired target doesn't exist in the list, fire at the first opponent panel
        if (target == -1)
        {
            target = 0;
        }
        //find target panel and set target position
        _targetPanel = _grid.GetPanelFromP1List(target);
        _targetPosition = _targetPanel.transform.position + _bulletEmitterPosition;
    }
    public void FireMortar()
	{
        if (_block.owner == null)
        {
            return;
        }
        if (_block.owner.name == "Player1")
        {
            AimAtOpponentPanelP1();
        }
        else
        {
            AimAtOpponentPanelP2();
        }
		_bulletEmitter.GetComponent<GunBehaviour>().owner = _block.owner.name;
		Instantiate(_bulletEmitter, _targetPosition, Quaternion.Euler(90, 0, 0));
		
	}
}
