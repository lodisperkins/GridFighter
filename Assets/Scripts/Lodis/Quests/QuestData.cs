using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using UnityEngine.Events;

namespace Lodis.Quest
{
    [System.Serializable]
    public class QuestData
    {
        private List<QuestStep> _steps = new List<QuestStep>();
        [SerializeField]
        private UnityEvent _onQuestComplete;
        private UnityAction<QuestStep> _onStepBegin;
        private UnityAction<QuestStep> _onStepComplete;
        private int _currentStep;
        private bool _finishedQuest;

        public List<QuestStep> Steps { get => _steps; set => _steps = value; }
        public int CurrentStepIndex { get => _currentStep; private set => _currentStep = value; }
        public UnityEvent OnQuestComplete { get => _onQuestComplete; set => _onQuestComplete = value; }

        public QuestStep GetStep(string name)
        {
            QuestStep stepData = _steps.Find(step => step.StepData.StepName == name);
            return stepData;
        }

        public QuestStep GetCurrentStep()
        {
            if (CurrentStepIndex < 0 || CurrentStepIndex >= _steps.Count)
                return null;

            return _steps[CurrentStepIndex];
        }

        public void AddOnStepBeginAction(UnityAction<QuestStep> action)
        {
            _onStepBegin += action;
        }

        public void AddOnStepCompleteAction(UnityAction<QuestStep> action)
        {
            _onStepComplete += action;
        }

        public void UpdateCurrentStep()
        {
            if (_finishedQuest)
                return;

            QuestStep currentStep = Steps[CurrentStepIndex];

            if (currentStep.Status == QuestStatus.INACTIVE && CurrentStepIndex == 0)
            {
                currentStep.Start();
                _onStepBegin?.Invoke(currentStep);
            }

            currentStep.Update();

            if (currentStep.Status == QuestStatus.COMPLETE)
            {
                CurrentStepIndex++;
                _onStepComplete?.Invoke(currentStep);

                if (CurrentStepIndex >= _steps.Count)
                {
                    OnQuestComplete?.Invoke();
                    _finishedQuest = true;
                    return;
                }

                currentStep = GetCurrentStep();

                _onStepBegin?.Invoke(currentStep);

                currentStep.Start();
            }
        }
    }
}