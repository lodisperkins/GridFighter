using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Quest
{
    public class QuestManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private QuestStepData[] _stepData;
        [SerializeField]
        private UnityEvent _onQuestComplete;
        private QuestData _currentQuest =  new QuestData();
        private bool _questComplete;

        public QuestData CurrentQuest { get => _currentQuest; private set => _currentQuest = value; }
        public QuestStepData[] StepData { get => _stepData; set => _stepData = value; }
        public bool QuestComplete { get => _questComplete; private set => _questComplete = value; }


        // Start is called before the first frame update
        public virtual void Start()
        {
            CurrentQuest.OnQuestComplete = _onQuestComplete;
            CurrentQuest.OnQuestComplete.AddListener(() => QuestComplete = true);

            InitQuest();
        }

        public virtual void InitQuest()
        {

        }

        // Update is called once per frame
        void Update()
        {
            CurrentQuest.UpdateCurrentStep();
        }
    }
}