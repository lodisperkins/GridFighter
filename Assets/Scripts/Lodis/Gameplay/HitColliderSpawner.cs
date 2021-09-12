using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public static class HitColliderSpawner
    {

        public static HitColliderBehaviour SpawnSphereCollider(Vector3 position, float radius, float damage,
            float knockBackScale, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner,false, false, true);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnSphereCollider(Transform parent, float radius, float damage, 
            float knockBackScale, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0,
            GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + " SphereCollider";
            SphereCollider collider = hitObject.AddComponent<SphereCollider>();
            hitObject.transform.parent = parent;
            collider.isTrigger = true;
            collider.radius = radius;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnBoxCollider(Vector3 position, Vector3 size, float damage,
            float knockBackScale, float hitAngle, bool despawnAfterTimeLimit,
            float timeActive = 0, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.position = position;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnBoxCollider(Transform parent, Vector3 size, float damage,
            float knockBackScale, float hitAngle, bool despawnAfterTimeLimit,
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
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnBoxCollider(Transform parent, Vector3 size, HitColliderBehaviour hitCollider, GameObject owner = null)
        {
            GameObject hitObject = new GameObject();
            hitObject.name = owner.name + "BoxCollider";
            BoxCollider collider = hitObject.AddComponent<BoxCollider>();
            hitObject.transform.parent = parent;
            hitObject.transform.localPosition = Vector3.zero;
            collider.isTrigger = true;
            collider.size = size;

            HitColliderBehaviour hitScript = hitObject.AddComponent<HitColliderBehaviour>();
            HitColliderBehaviour.Copy(hitCollider, hitScript);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnCapsuleCollider(Vector3 position, float radius, float height, 
            float damage, float knockBackScale, float hitAngle, 
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
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }

        public static HitColliderBehaviour SpawnCapsuleCollider(Transform parent, float radius, float height,
            float damage, float knockBackScale, float hitAngle, Quaternion rotation,  bool despawnAfterTimeLimit,
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
            hitScript.Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive,
                owner, false, false, true);

            return hitScript;
        }
    }
}

