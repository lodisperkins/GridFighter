
using FixedPoints;
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

        public Transform Start { get => _start; set => _start = value; }
        public Transform Target { get => _target; set => _target = value; }
        public TeleporterBehaviour LastTeleporterUsed { get; set; }
        public bool IgnoreTeleporters { get; set; } = false;
        public UnityAction<TeleporterBehaviour> OnTeleportedEvent { get; set; }

        private Stack<LineFollowBehaviour> _linkedLines = new Stack<LineFollowBehaviour>();

        public void OnTeleported(EntityDataBehaviour teleporterOwner, TeleporterBehaviour teleporter, TeleporterBehaviour linkedTeleporter)
        {
            if (IgnoreTeleporters)
                return;

            if (LastTeleporterUsed == teleporter)
            {
                RemoveLinkedLine();
                return;
            }

            EntityDataBehaviour entity = GetComponentInParent<EntityDataBehaviour>();

            if (linkedTeleporter != LastTeleporterUsed)
            {
                //This breaks because it holds it open on return
                teleporter.HoldTeleporterOpen(entity);
            }

            FTransform trans = entity.FixedTransform;
            trans.WorldPosition = linkedTeleporter.FixedTransform.WorldPosition;

            AddLinkedLine(teleporter.transform, linkedTeleporter.transform);

        }

        public void AddLinkedLine(Transform previousTarget, Transform newStart)
        {
            LineFollowBehaviour newLine = Instantiate(gameObject, transform.parent).GetComponent<LineFollowBehaviour>();

            newLine.Target = _target;
            _target = previousTarget;
            newLine.Start = newStart;

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

            Destroy(lineToRemove.gameObject);
        }

        private void OnEnable()
        {
            if (_setStartPosOnEnable)
                _start = transform;

            _line.SetPosition(0, _start.position);
            _line.SetPosition(1, _target.position);
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