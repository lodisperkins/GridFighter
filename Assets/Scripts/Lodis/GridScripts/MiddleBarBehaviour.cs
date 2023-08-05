using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class MiddleBarBehaviour : MonoBehaviour
    {
        private PanelBehaviour _referencePanel;
        private GridBehaviour _grid;

        // Start is called before the first frame update
        void Start()
        {
            _grid = BlackBoardBehaviour.Instance.Grid;
        }

        // Update is called once per frame
        void Update()
        {
            _grid.GetPanel(_grid.TempMaxColumns - 1, 1, out _referencePanel);
            transform.position = _referencePanel.transform.position + Vector3.right * ((_grid.PanelScale.x + _grid.PanelSpacingX)  / 2);
            transform.position = new Vector3(transform.position.x, 0.028f, transform.position.z);
        }
    }
}