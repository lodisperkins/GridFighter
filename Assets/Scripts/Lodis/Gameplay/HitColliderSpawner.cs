using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{
    public static class HitColliderSpawner
    {
        
        /// <summary>
        /// Spawns a new entity with a grid collider attached.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="damage"></param>
        /// <param name="baseKnockBack"></param>
        /// <param name="hitAngle"></param>
        /// <param name="despawnAfterTimeLimit"></param>
        /// <param name="timeActive"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCollider(FVector3 position, Fixed32 width, Fixed32 height, float damage,
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive = 0, EntityDataBehaviour owner = null, MarkerType marker = MarkerType.DANGER)
        {
            //Create a new grid collider in the simulation
            EntityData colliderEntity = GridGame.SpawnEntity(position);
            colliderEntity.Name = owner.Data.Name + "Collider";

            //Initialize collider stats
            HitColliderBehaviour hitScript = colliderEntity.AddComponent<HitColliderBehaviour>();
            HitColliderData info = new HitColliderData()
            {
                Damage = damage,
                BaseKnockBack = baseKnockBack,
                HitAngle = hitAngle,
                DespawnAfterTimeLimit = despawnAfterTimeLimit,
                TimeActive = timeActive
            };

            //Set colliders settings
            GridTrackerBehaviour tracker = colliderEntity.UnityObject.AddComponent<GridTrackerBehaviour>();
            tracker.Marker = MarkerType.DANGER;

            hitScript.InitCollider(width, height, owner);
            hitScript.ColliderInfo = info;
            hitScript.EntityCollider.Overlap = true;

            return hitScript;
        }

        /// <summary>
        /// Spawns a new box collider
        /// </summary>
        /// <param name="position">The world position of this collider</param>
        /// <param name="size">The dimension of this collider</param>
        /// <param name="hitCollider">The hit collider this collider will copy its values from</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCollider(FVector3 position, Fixed32 width, Fixed32 height, HitColliderData info, EntityDataBehaviour spawner, bool debuggingEnabled = true)
        {
            //Create a new grid collider in the simulation
            EntityDataBehaviour colliderEntity = GridGame.SpawnEntity(position);
            colliderEntity.Data.Name = spawner.Data.Name + "Collider";
            colliderEntity.gameObject.name = spawner.Data.Name + "Collider";

            GridPhysicsBehaviour physics = colliderEntity.Data.AddComponent<GridPhysicsBehaviour>();
            physics.IsKinematic = true;

            //Initialize collider stats
            HitColliderBehaviour hitScript = colliderEntity.Data.AddComponent<HitColliderBehaviour>();

            //Set colliders settings
            GridTrackerBehaviour tracker = colliderEntity.gameObject.AddComponent<GridTrackerBehaviour>();
            tracker.Marker = MarkerType.DANGER;

            hitScript.InitCollider(width, height, spawner);
            hitScript.ColliderInfo = info;
            hitScript.EntityCollider.LayersToIgnore = info.LayersToIgnore;
            hitScript.EntityCollider.Overlap = true;
            hitScript.DebuggingEnabled = debuggingEnabled;
            hitScript.Spawner = spawner;

            return hitScript;
        }

        /// <summary>
        /// Spawns a new box collider
        /// </summary>
        /// <param name="parent">The game object this collider will be attached to</param>
        /// <param name="size">The dimension of the box collider</param>
        /// <param name="damage">The amount of damage this collider will do</param>
        /// <param name="baseKnockBack">How far this object will knock others back</param>
        /// <param name="hitAngle">THe angle that objects hit by this collider will be launched at</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider should despawn after a given time</param>
        /// <param name="timeActive">The amount of time this actor can be active for</param>
        /// <param name="owner">The owner of this collider. Collision with owner are ignored</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCollider(FTransform parent, Fixed32 width, Fixed32 height, HitColliderData info, EntityDataBehaviour spawner = null, bool debuggingEnabled = true)
        {
            //Create a new grid collider in the simulation
            EntityDataBehaviour colliderEntity = GridGame.SpawnEntity(parent.Entity);
            colliderEntity.Data.Name = spawner.Data.Name + "Collider";

            GridPhysicsBehaviour physics = colliderEntity.Data.AddComponent<GridPhysicsBehaviour>();
            physics.IsKinematic = true;

            //Initialize collider stats
            HitColliderBehaviour hitScript = colliderEntity.Data.AddComponent<HitColliderBehaviour>();

            //Set colliders settings
            GridTrackerBehaviour tracker = colliderEntity.gameObject.AddComponent<GridTrackerBehaviour>();
            tracker.Marker = MarkerType.DANGER;

            hitScript.InitCollider(width, height, spawner);
            hitScript.ColliderInfo = info;
            hitScript.EntityCollider.LayersToIgnore = info.LayersToIgnore;
            hitScript.EntityCollider.Overlap = true;
            hitScript.DebuggingEnabled = debuggingEnabled;

            return hitScript;
        }

        /// <summary>
        /// Spawns a new box collider
        /// </summary>
        /// <param name="parent">The game object this collider will be attached to</param>
        /// <param name="size">The dimension of the box collider</param>
        /// <param name="damage">The amount of damage this collider will do</param>
        /// <param name="baseKnockBack">How far this object will knock others back</param>
        /// <param name="hitAngle">THe angle that objects hit by this collider will be launched at</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider should despawn after a given time</param>
        /// <param name="timeActive">The amount of time this actor can be active for</param>
        /// <param name="owner">The owner of this collider. Collision with owner are ignored</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCollider(FTransform parent, Fixed32 width, Fixed32 height, float damage,
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive, EntityDataBehaviour owner = null)
        {
            //Create a new grid collider in the simulation
            EntityData colliderEntity = GridGame.SpawnEntity(owner);
            colliderEntity.Name = owner.Data.Name + "Collider";

            //Initialize collider stats
            HitColliderBehaviour hitScript = colliderEntity.AddComponent<HitColliderBehaviour>();
            HitColliderData info = new HitColliderData()
            {
                Damage = damage,
                BaseKnockBack = baseKnockBack,
                HitAngle = hitAngle,
                DespawnAfterTimeLimit = despawnAfterTimeLimit,
                TimeActive = timeActive
            };

            //Set colliders settings
            GridTrackerBehaviour tracker = colliderEntity.UnityObject.AddComponent<GridTrackerBehaviour>();
            tracker.Marker = MarkerType.DANGER;

            hitScript.InitCollider(width, height, owner);
            hitScript.ColliderInfo = info;
            hitScript.EntityCollider.Overlap = true;

            return hitScript;
        }

        /// <summary>
        /// Spawns a new box collider
        /// </summary>
        /// <param name="parent">The game object this collider will be attached to</param>
        /// <param name="size">The dimension of the box collider</param>
        /// <param name="hitCollider">The hit collider this collider will copy its values from</param>
        /// <returns></returns>
        /// <param name="owner"></param>
        //public static HitColliderBehaviour SpawnCollider(Fixed32 width, Fixed32 height, FVector3 location, int panelY, HitColliderData hitCollider, GameObject owner)
        //{
        //    GameObject hitObject = null;

           

        //    bool colliderExisted = ObjectPoolBehaviour.Instance.GetObject(out hitObject, owner.name + hitCollider.Name + " BoxCollider", null, true, true);

        //    HitColliderBehaviour hitScript;

        //    hitObject.name = owner.name + hitCollider.Name + " BoxCollider";
        //    BoxCollider collider = colliderExisted? hitObject.GetComponent<BoxCollider>() : hitObject.AddComponent<BoxCollider>();
        //    hitObject.transform.SetParent(parent);
        //    hitObject.transform.localPosition = Vector3.zero;
        //    hitObject.transform.localRotation = Quaternion.identity;
        //    collider.isTrigger = true;
        //    collider.size = size;

        //    if (hitObject.TryGetComponent(out hitScript))
        //        return hitScript;

        //    hitScript = colliderExisted ? hitObject.GetComponent<HitColliderBehaviour>() : hitObject.AddComponent<HitColliderBehaviour>();
        //    GridTrackerBehaviour tracker = colliderExisted ? hitObject.GetComponent<GridTrackerBehaviour>() : hitObject.AddComponent<GridTrackerBehaviour>();
        //    tracker.Marker = MarkerType.DANGER;
        //    hitScript.ColliderInfo = hitCollider;
        //    hitScript.Owner = owner;

        //    return hitScript;
        //}

    }
}

