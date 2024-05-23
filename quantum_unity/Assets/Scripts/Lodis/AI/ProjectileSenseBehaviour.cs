using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class ProjectileSenseBehaviour : MonoBehaviour
    {
        [SerializeField] private AIControllerBehaviour _owner;

        private void OnTriggerEnter(Collider other)
        {

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

            HitColliderBehaviour collider = otherGameObject.GetComponent<HitColliderBehaviour>();
            if (collider && collider.Owner != _owner.Character)
                _owner.GetAttacksInRange().Add(collider);

            if (!other.CompareTag("Structure") || _owner.Knockback?.CurrentAirState != AirState.TUMBLING)
                return;

            RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

            if (!ringBarrier)
                return;

            if (ringBarrier.Owner == _owner.Character)
                _owner.TouchingBarrier = true;
            else
                _owner.TouchingOpponentBarrier = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Structure") || _owner.Knockback?.CurrentAirState != AirState.TUMBLING)
                return;

            RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

            if (!ringBarrier)
                return;

            if (ringBarrier.Owner == _owner.Character)
                _owner.TouchingBarrier = false;
            else
                _owner.TouchingOpponentBarrier = false;
        }

    }
}
