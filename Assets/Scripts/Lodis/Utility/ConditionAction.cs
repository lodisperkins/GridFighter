using UnityEngine;

namespace Lodis.Utility
{
    
    public class ConditionAction : DelayedAction
    {
        public Condition EventCheck;

        public ConditionAction() { }
        
        public override bool TryInvokeEvent()
        {
            if (!EventCheck.Invoke())
                return false;
            
            Event.Invoke(args);
            return true;
        }
    }
}
