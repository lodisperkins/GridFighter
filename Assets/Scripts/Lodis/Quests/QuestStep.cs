using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public enum QuestStatus
    {
        INACTIVE,
        ACTIVE,
        COMPLETE
    }

    public abstract class QuestStep
    {
        private QuestStepData _stepData;
        private GameObject _owner;
        private QuestStatus _status;

        public QuestStepData StepData { get => _stepData; set => _stepData = value; }
        public QuestStatus Status { get => _status; set => _status = value; }

        public QuestStep(QuestStepData data, GameObject owner)
        {
            StepData = data;
            _owner = owner;
        }

        // Start is called before the first frame update
        public virtual void OnStart()
        {

        }

        // Start is called before the first frame update
        public void Start()
        {
            _status = QuestStatus.ACTIVE;
            _stepData.OnStepBegin?.Invoke();
            OnStart();
        }

        // Update is called once per frame
        public virtual void OnUpdate()
        {

        }

        // Update is called once per frame
        public void Update()
        {
            if (_status != QuestStatus.ACTIVE)
                return;

            OnUpdate();
        }

        public void Complete()
        {
            StepData.OnStepComplete?.Invoke();
            _status = QuestStatus.COMPLETE;
            OnComplete();
        }

        // Update is called once per frame
        public virtual void OnComplete()
        {
        }

        // Update is called once per frame
        public virtual void OnCancel()
        {

        }

        // Update is called once per frame
        public void Cancel()
        {
            _status = QuestStatus.INACTIVE;
        }
    }
}