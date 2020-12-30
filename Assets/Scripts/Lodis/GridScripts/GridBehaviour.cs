using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class GridBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panelRef;
        [SerializeField]
        private Vector2 _dimensions;
        [SerializeField]
        private float _panelSpacing;
        [SerializeField]
        private PanelBehaviour[,] _panels;

        private void Awake()
        {
            _panels = new PanelBehaviour[(int)_dimensions.x, (int)_dimensions.y];
        }

        // Start is called before the first frame update
        void Start()
        {
            CreateGrid();
        }

        /// <summary>
        /// Creates a grid using the given dimensions and spacing.
        /// </summary>
        private void CreateGrid()
        {
            //The world spawn position for each gameobject in the grid
            Vector3 spawnPosition = transform.position;

            //The x and y position for each game object in the grid
            int xPos = 0;
            int yPos = 0;
            for (int i = 0; i < (int)_dimensions.x * (int)_dimensions.y; i++)
            {
                GameObject panel = (Instantiate(_panelRef, spawnPosition, new Quaternion(), transform));
                _panels[xPos, yPos] = panel.GetComponent<PanelBehaviour>();

                //If the x position in the grid is equal to the given x dimension,
                //reset x position to be 0, and increase the y position.
                if (xPos == (int)_dimensions.x - 1)
                {
                    xPos = 0;
                    spawnPosition.x = transform.position.x;
                    yPos++;
                    spawnPosition.z += _panelRef.transform.localScale.z * _panelSpacing;
                    continue;
                }

                //Increase x position
                xPos++;
                spawnPosition.x += _panelRef.transform.localScale.x * _panelSpacing;
            }
        }
        
        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="x">The x position  of the panel.</param>
        /// <param name="y">The y position of the panel.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        public bool GetPanel(int x, int y, out PanelBehaviour panel, bool canBeOccupied = true)
        {
            panel = null;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (x < 0 || x >= _dimensions.x || y < 0 || y >= _dimensions.y)
                return false;
            else if (!canBeOccupied && _panels[x, y].Occupied)
                return false;

            panel = _panels[x, y];

            return true;
        }


        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="position">The position of the panel on the grid.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        public bool GetPanel(Vector2 position, out PanelBehaviour panel, bool canBeOccupied = true)
        {
            panel = null;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (position.x < 0 || position.x >= _dimensions.x || position.y < 0 || position.y >= _dimensions.y)
                return false;
            else if (!canBeOccupied && _panels[(int)position.x, (int)position.y].Occupied)
                return false;

            panel = _panels[(int)position.x, (int)position.y];

            return true;
        }
    }
}

