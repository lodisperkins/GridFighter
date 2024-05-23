using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecisionDisplayBehaviour : MonoBehaviour
{
    public static bool DisplayText;

    [SerializeField]
    private Text _text;

    // Update is called once per frame
    void Update()
    {
        if (DisplayText)
        {
            _text.gameObject.SetActive(true);
            _text.text = "Decisions: \n" + Lodis.AI.AttackDecisionTree.DecisionData + "\n" + Lodis.AI.DefenseDecisionTree.DecisionData;
        }
        else
            _text.gameObject.SetActive(false);

    }
}
