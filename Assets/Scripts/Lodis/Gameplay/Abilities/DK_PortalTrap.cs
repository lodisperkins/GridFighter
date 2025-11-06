using FixedPoints;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_PortalTrap : Ability
    {
        protected TeleporterBehaviour _teleporter1;
        protected TeleporterBehaviour _teleporter2;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Fixed32 offset = abilityData.GetCustomStatValue("Offset");
            EntityDataBehaviour entity = null;

            FVector3 offsetPosition = Owner.FixedTransform.WorldPosition + (Owner.FixedTransform.Forward * offset);

            entity = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), offsetPosition, Owner.FixedTransform.WorldRotation);

            HitColliderBehaviour trapCollider = entity.Data.GetComponentInChildren<HitColliderBehaviour>();

            trapCollider.ColliderInfo = GetColliderData(0);
            trapCollider.Spawner = Owner;

            TeleporterBehaviour[] teleporters = entity.Data.GetComponentsInChildren<TeleporterBehaviour>();

            teleporters[0].InitTeleporter(Owner);
            teleporters[1].InitTeleporter(Owner);
        }
    }
}