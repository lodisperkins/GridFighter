using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class ProjectileSenseBehaviour : MonoBehaviour
    {
        [SerializeField] private AttackDummyBehaviour _owner;

        private void OnTriggerEnter(Collider other)
        {

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

            HitColliderBehaviour collider = otherGameObject.GetComponent<HitColliderBehaviour>();
            if (collider && collider.Owner != _owner.Character)
                _owner.GetAttacksInRange().Add(collider);
        }
    }
}
