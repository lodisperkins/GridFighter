using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UpdatableTextBehaviour : MonoBehaviour {
    [SerializeField]
    private Text _text;
    [SerializeField]
    private List<string> _textList;
	public void ChangeText(int index)
    {
        _text.text = _textList[index];
    }
}
