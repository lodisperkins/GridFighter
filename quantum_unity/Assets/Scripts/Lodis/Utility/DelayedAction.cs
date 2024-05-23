using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Utility
{
    public abstract class DelayedAction
    {
        protected bool IsActive;
        public object[] args;
        public DelayedEvent Event;
        /// <summary>
        /// Event called when action is stopped before completion.
        /// </summary>
        public UnityAction OnCancel;

        public bool GetEnabled() { return IsActive; }
        public void Enable() { IsActive = true; }
        public void Disable() { IsActive = false; }

        public abstract bool TryInvokeEvent();
    }
}
