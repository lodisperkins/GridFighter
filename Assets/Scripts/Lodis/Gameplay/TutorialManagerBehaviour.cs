using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManagerBehaviour : QuestManagerBehaviour
{
    [SerializeField]
    private TextTypeBehaviour _textTyper;
    [SerializeField]
    private GameObject _textCanvas;

    private void Start()
    {
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
        _textTyper.SetTextToType(CurrentQuest.Steps[0].Description);
    }


}
