using System;
using System.Collections;
using System.Collections.Generic;
using GridGame.GamePlay.GridScripts;
using GridGame.Movement;
using UnityEngine;
using GridGame.VariableScripts;

namespace GridGame.Movement
{
    /// <summary>
    /// This script handles all movement of the player. To move, a positive or negative value is added
    /// to the players destination vector. If the value for the destination vector matches that of a panel 
    /// position, and that position is not occupied, the players position vector and their position in the world is 
    /// changed to match that of the desired panel.
    /// </summary>
    public class PlayerMovementBehaviour : MonoBehaviour
    {
        //The players current position on the grid
        public Vector2 Position;
        [SerializeField]
        private Vector2Variable PositionRef;
        //The players desired position on the grid
        [SerializeField]
        private Vector2 Destination;
        //The direction in which the player is trying to travel on the grid
        [SerializeField]
        private Vector2Variable Direction;
        public bool canMove;
        public bool panelStealActive;
        [SerializeField]
        public Event OnPanelSteal;
        [SerializeField]
        public Event OnPanelStealDisabled;
        [SerializeField]
        public Event OnPanelStealEnabled;
        //the current panel the player is on
        public PanelBehaviour _currentPanel;
        public GamePlay.OtherScripts.ScreenShakeBehaviour shakeScript;
        [SerializeField] private FlashBehaviour _flashScript;
        private HealthBehaviour _health;
        [SerializeField]
        private List<MonoBehaviour> _inputScripts;

        public PanelBehaviour CurrentPanel
        {
            get
            {
                return _currentPanel;
            }
            set
            {
                _currentPanel = value;
            }
        }
        
        //Used to store the value of the panel the player will be traveling to
        PanelBehaviour NewPanel;
        //The list of all panels available to the player
        [SerializeField]
        private List<GameObject> startingPanels;
        public PanelList Panels;

        [SerializeField] private Event _onMove;
        private GridPhysicsBehaviour physicsBehaviour;
        // Use this for initialization
        void Start()
        {
            Destination = new Vector2(0,0);
            canMove = true;
            panelStealActive = false;
            _currentPanel = Panels[16].GetComponent<PanelBehaviour>();
            _health = GetComponent<HealthBehaviour>();
            _health.onStunned.AddListener(Stun);
            _health.onUnstunned.AddListener(Unstun);
            if (name == "Player1")
            {
                BlackBoard.Player1 = gameObject;
            }
            else
            {
                BlackBoard.Player2 = gameObject;
            }
            physicsBehaviour = GetComponent<GridPhysicsBehaviour>();
        }

        private void Stun()
        {
            GetComponent<InputCustom.InputButtonBehaviour>().enabled = false;
            GetComponent<InputCustom.InputAxisBehaviour>().enabled = false;
            _flashScript.isInfinite = true;
            _flashScript.StartFlashing();
        }
        private void Unstun()
        {
            GetComponent<InputCustom.InputButtonBehaviour>().enabled = true;
            GetComponent<InputCustom.InputAxisBehaviour>().enabled = true;
            _flashScript.isInfinite = false;
            _flashScript.StopFlashing();
        }
        
        private void Awake()
        {
            Panels.Init(startingPanels, name);
        }
        //Allows the player to steal panels
        public void EnablePanelSteal()
        {
            if (panelStealActive == false)
            {
                panelStealActive = true;
                DisableMovement();
                OnPanelStealEnabled.Raise(gameObject);
            }
        }
        //disables the ability to steal panels
        public void DisablePanelSteal()
        {
            if(panelStealActive)
            {
                panelStealActive = false;
                EnableMovement();
                OnPanelStealDisabled.Raise(gameObject);
            }
        }
        //Raises the event to steal a panel from the other panelist
        public void StealPanel()
        {
            if (panelStealActive)
            {
                OnPanelSteal.Raise(gameObject);
            }       
        }

        public void ResetPositionToCurrentPanel()
        {
            transform.position = new Vector3(CurrentPanel.transform.position.x, transform.position.y, CurrentPanel.transform.position.z);
            Position = _currentPanel.Position;
        }
        public void ResetPositionToStartPanel()
        {
            transform.position = new Vector3(Panels[0].transform.position.x, transform.position.y, Panels[0].transform.position.z);
            _currentPanel.GetComponent<PanelBehaviour>().Occupied = false;
            _currentPanel = Panels[0].GetComponent<PanelBehaviour>();
            Position = _currentPanel.Position;
        }
        //enables the players movement
        public void EnableMovement()
        {
            canMove = true;
        }
        //disables the players movement
        public void DisableMovement()
        {
            canMove = false;
        }
        //Sets the players position to the desired panel if it exists and is not occupied
        void UpdatePosition()
        {
            if (CheckPanels(Destination, out NewPanel))
            {
                if (NewPanel.Occupied == true)
                {
                    Destination = new Vector2(0, 0);
                    return;
                }
                transform.position = new Vector3(NewPanel.transform.position.x, transform.position.y, NewPanel.transform.position.z);
                shakeScript.StartPosition = transform.position;
                _currentPanel.Occupied = false;
                _currentPanel = NewPanel;
                _currentPanel.Occupied = true;
                Position += Destination;
                Destination = new Vector2(0, 0);
                _onMove.Raise(gameObject);
                return;
            }
            Destination = new Vector2(0, 0);
        }

        public bool TempStealPanel(PanelBehaviour panel)
        {
            if (name == "Player1")
                return BlackBoard.grid.TempStealPanelP1(panel);
            else
                return BlackBoard.grid.TempStealPanelP2(panel);
        }
        //Checks to see if a panel is accessible
        public bool CheckPanels(Vector2 panelPosition, out PanelBehaviour ReturnPanel)
        {
            if (GamePlay.GridScripts.GridBehaviour.globalPanelList.FindPanel(Position + panelPosition, out ReturnPanel))
            {
                if (!Panels.Contains(ReturnPanel))
                    return TempStealPanel(ReturnPanel);

                return true;
            }

            ReturnPanel = null;
            return false;
        }
        //Checks to see if a panel is accessible
        public bool CheckPanels(Vector2 PanelPosition)
        {
            foreach (PanelBehaviour panel in Panels.Panels)
            {
                var coordinate = panel.GetComponent<PanelBehaviour>().Position;
                if (Position + PanelPosition == coordinate)
                {
                    return true;
                }
            }
            return false;
        }
        
        public void MoveRight()
        {
            if(canMove == false)
            {
                return;
            }
            Destination.x += 1;
            UpdatePosition();
        }
        public void MoveLeft()
        {
            if (canMove == false)
            {
                return;
            }
            Destination.x -= 1;
            UpdatePosition();
        }
        public void MoveUp()
        {
            if (canMove == false)
            {
                return;
            }
            Destination.y += 1;
            UpdatePosition();
        }
        public void MoveDown()
        {
            if (canMove == false)
            {
                return;
            }
            Destination.y -= 1;
            UpdatePosition();
        }
        //Is used to update the destination vector to be the desired location of the player
        public void ChangeDestination()
        {
            Destination = new Vector2(0, 0);
            if (Direction.Val.x == -1)
            {
                Destination.x -= 1;
            }
            else if (Direction.Val.x == 1)
            {
                Destination.x += 1;
            }
            else if (Direction.Val.y == -1)
            {
                Destination.y -= 1;
            }
            else if (Direction.Val.y == 1)
            {
                Destination.y += 1;
            }
            UpdatePosition();
        }

        public void DisableControls()
        {
            foreach(var script in _inputScripts)
            {
                script.enabled = false;
            }
        }

        public void EnableControls()
        {
            foreach (var script in _inputScripts)
            {
                script.enabled = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Panel") && physicsBehaviour.IsMoving)
            {
                _currentPanel = other.GetComponent<PanelBehaviour>();
                Position = _currentPanel.Position;
            }
        }
        // Update is called once per frame
        void Update()
        {
            PositionRef.Val = Position;
            if(name =="Player1")
            {
                BlackBoard.p1Position = _currentPanel;
            }
            else
            {
                BlackBoard.p2Position = _currentPanel;
            }
            physicsBehaviour.currentPanel = CurrentPanel;
        }
    }
}
