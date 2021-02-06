using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame
{
	public class LoadoutDisplayBehaviour : MonoBehaviour
	{

		[SerializeField]
		private List<BlockDisplayBehaviour> _blockDisplays;
		[SerializeField]
		private GridGame.VariableScripts.GameObjectList _objectList;

		public void ClearDisplays()
        {
			foreach (var display in _blockDisplays)
				display.ClearDisplay();
        }

		public void UpdateDisplays()
        {
			for (int i = 0; i < 4; i++)
				UpdateDisplay(i);
        }

		public void UpdateDisplay(int index)
        {
			if (index >= 0  && index < _blockDisplays.Count)
				_blockDisplays[index].DisplayBlock(_objectList[index]);
        }
	}
}


