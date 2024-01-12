using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManagerBehaviour : MonoBehaviour
{
    [SerializeField]
    private QuestData _currentQuest;

    public QuestData CurrentQuest { get => _currentQuest; private set => _currentQuest = value; }


    // Start is called before the first frame update
    void Start()
    {
        InitQuest();
    }

    public virtual void InitQuest()
    {

    }

    // Update is called once per frame
    void Update()
    {
        CurrentQuest.CheckCurrentStepComplete();    
    }
}
