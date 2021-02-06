using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace GridGame.GamePlay
{
    public class RoutineBehaviour : MonoBehaviour
    {
        //Holds the list of actions that will be done by the gameobject
        [FormerlySerializedAs("OnActionsBegin")] [SerializeField]
        private GridGame.Event onActionsBegin;
        [FormerlySerializedAs("OnActionsCompleted")] [SerializeField]
        private GridGame.Event onActionsCompleted;
        //Time it takes to invoke the actions event
        [FormerlySerializedAs("action_delay")] [SerializeField]
        public float actionDelay;
        //Number of times the actions event will be invoked
        [FormerlySerializedAs("action_limit")] [SerializeField]
        public int actionLimit;
        [SerializeField]
        private bool hasLimit;
        public int numberOfActionsLeft;
        private bool _isOnActionsBeginNotNull;
        private bool _isOnActionsCompletedNotNull;
        public bool shouldStop;
        private void OnEnable()
        {
            _isOnActionsBeginNotNull = onActionsBegin != null;
            _isOnActionsCompletedNotNull = onActionsCompleted != null;
            StartCoroutine(PerformActions());
        }

        public void ResetActions()
        {
            StopAllCoroutines();
            StartCoroutine(PerformActions());
        }
        private IEnumerator PerformActions()
        {
            for (var i = 0; i <= actionLimit; i++)
            {
                if(shouldStop)
                {
                    yield break;
                }
                numberOfActionsLeft = actionLimit - i;
                if (_isOnActionsBeginNotNull)
                {
                    onActionsBegin.Raise(gameObject);
                }
                if (hasLimit == false)
                {
                    i--;
                }
                yield return new WaitForSeconds(actionDelay);
            }
            if (_isOnActionsCompletedNotNull)
            {
                onActionsCompleted.Raise(gameObject);
            }
        }
    }
}
