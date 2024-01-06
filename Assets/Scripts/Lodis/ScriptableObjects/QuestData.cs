using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using UnityEngine.Events;

[System.Serializable]
public class QuestData
{
    [SerializeField]
    private QuestStepData[] _steps;
    [SerializeField]
    private UnityEvent _onQuestComplete;
    private int _currentStep;

    public QuestStepData[] Steps { get => _steps; set => _steps = value; }
    public int CurrentStep { get => _currentStep; private set => _currentStep = value; }
    public UnityEvent OnQuestComplete { get => _onQuestComplete; set => _onQuestComplete = value; }

    public QuestStepData GetStep(string name)
    {
        QuestStepData stepData = _steps.FindValue<QuestStepData>(step => step.StepName == name);
        return stepData;
    }

    public bool CheckCurrentStepComplete(params object[] args)
    {
        bool stepComplete = Steps[CurrentStep].CompletionCondition(args);

        if (stepComplete)
        {
            CurrentStep++;
            Steps[CurrentStep].OnStepComplete?.Invoke();
        }

        return stepComplete;
    }
}
