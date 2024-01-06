using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManagerBehaviour : MonoBehaviour
{
    [SerializeField]
    private QuestData _currentQuest;

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
        _currentQuest.CheckCurrentStepComplete();    
    }
}
