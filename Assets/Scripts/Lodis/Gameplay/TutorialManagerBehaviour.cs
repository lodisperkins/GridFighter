using Lodis.AI;
using Lodis.Gameplay;
using Lodis.Quest;
using Lodis.Sound;
using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManagerBehaviour : QuestManagerBehaviour
{
    [SerializeField]
    private TextTypeBehaviour _textTyper;
    [SerializeField]
    private GameObject _textCanvas;
    [SerializeField]
    private AudioClip _completeSound;
    [SerializeField]
    private ComboCounterBehaviour _comboCounter;
    [SerializeField]
    private AITrainingBehaviour _trainingBehaviour;

    public override void Start()
    {
        base.Start();
        CurrentQuest.AddOnStepBeginAction(UpdateText);
        CurrentQuest.AddOnStepCompleteAction(GenerateRandomCompliment);
        CurrentQuest.AddOnStepCompleteAction(step => MatchManagerBehaviour.Instance.SetPlayerControlsActive(false));
    }

    public void EnableTextCanvas(float delay)
    {
        StartCoroutine(StartEnableTextTimer(delay));
    }

    private IEnumerator StartEnableTextTimer(float delay)
    {
        yield return new WaitForSeconds(delay);

        _textCanvas.SetActive(true);
    }

    public override void InitQuest()
    {
        GameObject playerRef = BlackBoardBehaviour.Instance.Player1;

        CurrentQuest.Steps.Add(new MoveStep(StepData[0], playerRef));
        CurrentQuest.Steps.Add(new NormalAttackStep(StepData[1], playerRef));
        CurrentQuest.Steps.Add(new ComboStep(StepData[2], playerRef));
        CurrentQuest.Steps.Add(new StrongAttackStep(StepData[3], playerRef));
        CurrentQuest.Steps.Add(new SpecialAttackStep(StepData[4], playerRef));
        CurrentQuest.Steps.Add(new ReloadStep(StepData[5], playerRef));


        CurrentQuest.Steps.Add(new BurstStep(StepData[6], playerRef));
    }

    public void TryEnableAI()
    {
        if (CurrentQuest.GetCurrentStep().StepData.StepName == "burst")
            _trainingBehaviour.SetAIState(1);
    }

    public void TryClosingTextBox()
    {
        if (QuestComplete)
            _textCanvas.gameObject.SetActive(false);
    }

    private void GenerateRandomCompliment(QuestStep step)
    {
        SoundManagerBehaviour.Instance.PlaySound(_completeSound);

        int choice = Random.Range(0,3);

        if (choice == 0)
            choice = 4;

        _comboCounter.DisplayComboMessage(choice);
    }

    private void UpdateText(QuestStep step)
    {
        _textTyper.SetTextToType(CurrentQuest.GetCurrentStep().StepData.Description);

        if (!_textTyper.gameObject.activeInHierarchy)
            return;

        _textTyper.BeginTyping(0);
    }
}
