using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lodis.Quest
{
    public class MoveStep : QuestStep
    {
        private List<Vector2> _moveLocations;
        private GridMovementBehaviour _ownerMovement;

        public MoveStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
            _ownerMovement = owner.GetComponent<GridMovementBehaviour>();

            _moveLocations = new List<Vector2>();

            _moveLocations.Add(_ownerMovement.Position + Vector2.right);
            _moveLocations.Add(_ownerMovement.Position + Vector2.left);
            _moveLocations.Add(_ownerMovement.Position + Vector2.up);
            _moveLocations.Add(_ownerMovement.Position + Vector2.down);
        }

        public override void OnStart()
        {
            base.OnStart();
            List<PanelBehaviour> panels = BlackBoardBehaviour.Instance.Grid.GetPanelNeighbors(_ownerMovement.Position, true, GridAlignment.ANY, false);

            foreach (PanelBehaviour panel in panels)
            {
                panel.Mark(MarkerType.POSITION, _ownerMovement.gameObject);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (_moveLocations.Count == 0)
                Complete();

            if (_moveLocations.Contains(_ownerMovement.Position))
                _moveLocations.Remove(_ownerMovement.Position);
        }
    }
}