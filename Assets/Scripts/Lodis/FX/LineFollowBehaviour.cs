
using FixedPoints;
using Lodis.GridScripts;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public class LineFollowBehaviour : MonoBehaviour, ITeleportable
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField] private bool _setStartPosOnEnable = true;
        [SerializeField] private bool _updateTargetPosition = true;
        [SerializeField] private bool _updateStartPosition = true;

        [HideIf("_setStartPosOnEnable")]
        [SerializeField] private Transform _start;

        [SerializeField] private Transform _target;

        private Stack<LineFollowBehaviour> _linkedLines = new Stack<LineFollowBehaviour>();
        private LineFollowBehaviour _lineParent;

        public Transform Start { get => _start; set => _start = value; }
        public Transform Target { get => _target; set => _target = value; }
        public TeleporterBehaviour LastTeleporterUsed { get; set; }
        public bool IgnoreTeleporters { get; set; } = false;
        public UnityAction<TeleporterBehaviour> OnTeleportedEvent { get; set; }
        public bool ShouldCancelTeleport { get; set; } = false;



        public void OnTeleported(EntityDataBehaviour teleporterOwner, TeleporterBehaviour teleporter, TeleporterBehaviour linkedTeleporter)
        {
            //If they are both on the back row then there nothing for the whip to even do so dont even teleport.
            //if (!CheckTeleportersValid(linkedTeleporter) && !CheckTeleportersValid(teleporter))
            //{
            //    ShouldCancelTeleport = true;
            //    return;
            //}

            EntityDataBehaviour entity = GetComponentInParent<EntityDataBehaviour>();
            FTransform trans = entity.FixedTransform;

            //If the last teleporter used is the same as the linked one, we are returning back through the teleporter chain.
            if (LastTeleporterUsed == linkedTeleporter)
            {
                RemoveLinkedLine();

                trans.WorldPosition = linkedTeleporter.FixedTransform.WorldPosition;
                entity.UpdateUnityTransform(GridGame.FixedTimeStep);
                return;
            }

            //Otherwise we are going forward through the teleporter chain. We need to hold it open so it doesn't count our return trip.
            if (linkedTeleporter != LastTeleporterUsed && !_lineParent)
            {
                teleporter.HoldTeleporterOpen(entity);
            }

            //trans.WorldPosition = linkedTeleporter.FixedTransform.WorldPosition;

            AddLinkedLine(teleporter.transform, linkedTeleporter.transform);

        }

        private bool CheckTeleportersValid(TeleporterBehaviour linkedTeleporter)
        {
            if (linkedTeleporter == null)
                return false;

            PanelBehaviour panel;

            if (!GridBehaviour.Instance.GetPanelAtLocationInWorld(linkedTeleporter.transform.position, out panel))
                return false;

            if (panel.Position.X == 0 || GridBehaviour.Instance.Dimensions.x - 1 == panel.Position.X)
                return false;

            return true;
        }

        public void AddLinkedLine(Transform previousTarget, Transform newStart)
        {
            LineFollowBehaviour newLine = Instantiate(gameObject, transform.parent).GetComponent<LineFollowBehaviour>();

            newLine.Target = _target;
            _target = previousTarget;
            newLine.Start = newStart;
            newLine._lineParent = this;

            _linkedLines.Push(newLine);
        }

        public void RemoveLinkedLine()
        {
            if (_linkedLines.Count <= 0)
            {
                return;
            }

            LineFollowBehaviour lineToRemove = _linkedLines.Pop();
            _target = lineToRemove.Target;

            lineToRemove.gameObject.SetActive(false);
            Destroy(lineToRemove.gameObject);
        }

        private void OnEnable()
        {
            if (_setStartPosOnEnable)
                _start = transform;

            _line.SetPosition(0, _start.position);
            _line.SetPosition(1, _target.position);

            IgnoreTeleporters = false;
        }

        private void OnDisable()
        {
            LastTeleporterUsed = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (_updateStartPosition)
                _line.SetPosition(0, _start.position);

            if (_updateTargetPosition)
                _line.SetPosition(1, _target.position);
        }
    }
}