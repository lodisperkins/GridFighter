using Lodis;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class QuestStepData
{
    [SerializeField]
    private string _stepName;
    [SerializeField]
    [TextArea]
    private string _description;
    private Condition _completionCondition;
    [SerializeField]
    private UnityEvent _onStepComplete;

    public Condition CompletionCondition { get => _completionCondition; set => _completionCondition = value; }
    public UnityEvent OnStepComplete { get => _onStepComplete; set => _onStepComplete = value; }
    public string StepName { get => _stepName; set => _stepName = value; }
}
