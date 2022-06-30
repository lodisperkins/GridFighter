using UnityEngine;

namespace Lodis.Utility
{
    public abstract class DelayedAction
    {
        protected bool IsActive;
        public object[] args;
        public DelayedEvent Event;

        public bool GetEnabled() { return IsActive; }
        public void Enable() { IsActive = true; }
        public void Disable() { IsActive = false; }

        public abstract bool TryInvokeEvent();
    }
}
