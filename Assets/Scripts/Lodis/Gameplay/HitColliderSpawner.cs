using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public static class HitColliderSpawner
    {
        /// <summary>
        /// Spawns a new sphere collider
        /// </summary>
        /// <param name="position">The world position of this collider</param>
        /// <param name="radius">The size of the sphere colliders radius</param>
        /// <param name="damage">The amount of damage this collider will do</param>
        /// <param name="baseKnockBack">How far this object will knock others back</param>
        /// <param name="hitAngle">THe angle that objects hit by this collider will be launched at</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider should despawn after a given time</param>
        /// <param name="timeActive">The amount of time this actor can be active for</param>
        /// <param name="owner">The owner of this collider. Collision with owner are ignored</param>
        /// <returns>A new collider with the given parameters</returns>
        public static HitColliderBehaviour SpawnSphereCollider(Vector3 position, float radius, float damage,
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner,false, false, true);

            return hitScript;
        }

        /// <summary>
        /// Spawns a new sphere collider
        /// </summary>
        /// <param name="parent">The game object this collider will be attached to</param>
        /// <param name="radius">The size of the sphere colliders radius</param>
        /// <param name="damage">The amount of damage this collider will do</param>
        /// <param name="baseKnockBack">How far this object will knock others back</param>
        /// <param name="hitAngle">THe angle that objects hit by this collider will be launched at</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider should despawn after a given time</param>
        /// <param name="timeActive">The amount of time this actor can be active for</param>
        /// <param name="owner">The owner of this collider. Collision with owner are ignored</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnSphereCollider(Transform parent, float radius, float damage, 
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0,
            GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.parent = parent;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        /// <summary>
        /// Spawns a new sphere collider
        /// </summary>
        /// <param name="parent">The game object this collider will be attached to</param>
        /// <param name="radius">The size of the sphere colliders radius</param>
        /// <param name="hitCollider">The hit collider this collider will copy its values from</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnSphereCollider(Transform parent, float radius, HitColliderBehaviour hitCollider)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = hitCollider.Owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.parent = parent;
            hitObject.transform.localPosition = Vector3.zero;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            HitColliderBehaviour.Copy(hitCollider, hitScript);

            return hitScript;
        }

        /// <summary>
        /// Spawns a new 
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
        public static HitColliderBehaviour SpawnBoxCollider(Vector3 position, Vector3 size, float damage,
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        /// <summary>
        /// Spawns a new box collider
        /// </summary>
        /// <param name="position">The world position of this collider</param>
        /// <param name="size">The dimension of this collider</param>
        /// <param name="hitCollider">The hit collider this collider will copy its values from</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnBoxCollider(Vector3 position, Vector3 size, HitColliderData hitCollider)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = hitCollider.OwnerAlignement + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = hitCollider;

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
        public static HitColliderBehaviour SpawnBoxCollider(Transform parent, Vector3 size, float damage,
            float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.parent = parent;
            hitObject.transform.localPosition = Vector3.zero;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

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
        public static HitColliderBehaviour SpawnBoxCollider(Transform parent, Vector3 size, HitColliderData hitCollider, GameObject owner)
        {
            GameObject hitObject = ObjectPoolBehaviour.Instance.GetObject(owner.name + hitCollider.Name + " BoxCollider", parent, true, true);

            HitColliderBehaviour hitScript;

            hitObject.name = owner.name + hitCollider.Name + " BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.SetParent(parent);
            hitObject.transform.localPosition = Vector3.zero;
            hitObject.transform.localRotation = Quaternion.identity;
            collider.isTrigger = true;
            collider.size = size;

            if (hitObject.TryGetComponent(out hitScript))
                return hitScript;

            hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.ColliderInfo = hitCollider;
            hitScript.Owner = owner;

            return hitScript;
        }

        /// <summary>
        /// Spawns a new capsule collider
        /// </summary>
        /// <param name="position">The world position of this collider</param>
        /// <param name="radius">The length of the radius of this collider</param>
        /// <param name="height">How tall this collider is</param>
        /// <param name="damage">The amount of damage this collider will do to objects</param>
        /// <param name="baseKnockBack">How far will objects hit by this collider will travel</param>
        /// <param name="hitAngle">The angle objects hit by this collider are launched</param>
        /// <param name="rotation">The orientation of this collider</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider will despawn after a given time</param>
        /// <param name="timeActive">The amount of time this collider is going to be active for</param>
        /// <param name="owner">The game object that owns this collider. Collision with this object will be ignored</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCapsuleCollider(Vector3 position, float radius, float height, 
            float damage, float baseKnockBack, float hitAngle, 
            Quaternion rotation, bool despawnAfterTimeLimit, float timeActive = 0,
            GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "CapsuleCollider";
            CapsuleCollider collider = hitObject.AddComponent<CapsuleCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.radius = radius;
            collider.height = height;
            hitObject.transform.rotation = rotation;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        /// <summary>
        /// Spawns a new capsule collider
        /// </summary>
        /// <param name="parent">The game object that this collider is attached to</param>
        /// <param name="radius">The length of the radius of this collider</param>
        /// <param name="height">How tall this collider is</param>
        /// <param name="damage">The amount of damage this collider will do to objects</param>
        /// <param name="baseKnockBack">How far will objects hit by this collider will travel</param>
        /// <param name="hitAngle">The angle objects hit by this collider are launched</param>
        /// <param name="rotation">The orientation of this collider</param>
        /// <param name="despawnAfterTimeLimit">Whether or not this collider will despawn after a given time</param>
        /// <param name="timeActive">The amount of time this collider is going to be active for</param>
        /// <param name="owner">The game object that owns this collider. Collision with this object will be ignored</param>
        /// <returns></returns>
        public static HitColliderBehaviour SpawnCapsuleCollider(Transform parent, float radius, float height,
            float damage, float baseKnockBack, float hitAngle, Quaternion rotation,  bool despawnAfterTimeLimit,
            float timeActive = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "CapsuleCollider";
            CapsuleCollider collider = hitObject.AddComponent<CapsuleCollider>();
            hitObject.transform.parent = parent;
            collider.isTrigger = true;
            collider.radius = radius;
            collider.height = height;
            hitObject.transform.rotation = rotation;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, baseKnockBack, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }
    }
}

