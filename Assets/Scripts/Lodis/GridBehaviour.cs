using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelRef;
    [SerializeField]
    private Vector2 _dimensions;
    [SerializeField]
    private float _panelSpacing;
    [SerializeField]
    private List<GameObject> _panels;

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
            _panels.Add(Instantiate(_panelRef, spawnPosition, new Quaternion(), transform));

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
