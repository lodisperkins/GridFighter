using Lodis;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Lodis.Quest
{

    [System.Serializable]
    public class QuestStepData
    {
        [SerializeField]
        private string _stepName;
        [SerializeField]
        [TextArea]
        private string _description;
        [SerializeField]
        private UnityEvent _onStepBegin;
        [SerializeField]
        private UnityEvent _onStepComplete;

        public UnityEvent OnStepComplete { get => _onStepComplete; set => _onStepComplete = value; }
        public string StepName { get => _stepName; set => _stepName = value; }
        public string Description { get => _description; set => _description = value; }
        public UnityEvent OnStepBegin { get => _onStepBegin; set => _onStepBegin = value; }
    }
}