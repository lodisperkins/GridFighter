using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public abstract class Ability
    {
        public Attack abilityType;
        public string name = "Unassigned";
        public GameObject owner = null;
        public float activeFrames = 0;
        public float restFrames = 0;
        public float startUpFrames = 0;
        public bool canCancel = false;
        public UnityAction onActivate = null;
        public UnityAction onDeactivate = null;
        public abstract void Activate();
        public abstract void Init(GameObject owner);
        public virtual void Deactivate() { }
    }
}
