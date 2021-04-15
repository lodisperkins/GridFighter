﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public static class HitColliderSpawner
    {

        public static HitColliderBehaviour SpawnSphereCollider(Vector3 position, float radius, float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnSphereCollider(Transform parent, float radius, float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.parent = parent;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnBoxCollider(Vector3 position, Vector3 size, float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnBoxCollider(Transform parent, Vector3 size, float damage, float knockBackScale, float hitAngle, bool despawnAfterFrameLimit, float activeFrames, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.parent = parent;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnCapsuleCollider(Vector3 position, float radius, float height, float damage, float knockBackScale, float hitAngle, Quaternion rotation, bool despawnAfterFrameLimit, float activeFrames = 0,  GameObject owner = null)
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
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnCapsuleCollider(Transform parent, float radius, float height, float damage, float knockBackScale, float hitAngle, Quaternion rotation, bool despawnAfterFrameLimit, float activeFrames = 0,  GameObject owner = null)
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
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterFrameLimit, activeFrames, owner);

            return hitScript;
        }
    }
}

