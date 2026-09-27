using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.AI
{
    public class ProjectileSenseBehaviour : SimulationBehaviour
    {

        #region Simulation Functions

        public override void Deserialize(Deserializer br)
        {
           return;
        }


        public override void Serialize(BinaryWriter bw)
        {
            return;
        }

        protected override string[] GetLogItems()
        {
            return null;
        }

        #endregion

        [SerializeField] private AIControllerBehaviour _owner;

        public override string LogName => "ProjectileSenseBehaviour";



        /// <summary>
        /// Hashes the serialized projectile sensing state, which is currently empty,
        /// so this component still participates in the shared checksum surface.
        /// </summary>


        //public override void OnOverlapEnter(Collision other)
        //{
        //    HitColliderBehaviour collider = other.Entity.GetComponent<HitColliderBehaviour>();
        //    if (collider && collider.Owner != _owner.Character)
        //        _owner.GetAttacksInRange().Add(collider);

        //    if (!other.CompareTag("Structure") || _owner.Knockback?.CurrentAirState != AirState.TUMBLING)
        //        return;

        //    RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

        //    if (!ringBarrier)
        //        return;

        //    if (ringBarrier.Owner == _owner.Character)
        //        _owner.TouchingBarrier = true;
        //    else
        //        _owner.TouchingOpponentBarrier = true;
        //}

        //private void OnTriggerExit(Collider other)
        //{
        //    if (!other.CompareTag("Structure") || _owner.Knockback?.CurrentAirState != AirState.TUMBLING)
        //        return;

        //    RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

        //    if (!ringBarrier)
        //        return;

        //    if (ringBarrier.Owner == _owner.Character)
        //        _owner.TouchingBarrier = false;
        //    else
        //        _owner.TouchingOpponentBarrier = false;
        //}

    }
}
