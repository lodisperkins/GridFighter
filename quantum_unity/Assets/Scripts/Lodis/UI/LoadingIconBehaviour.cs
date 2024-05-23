using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingIconBehaviour : MonoBehaviour
{
    [SerializeField]
    private Image _topLeft;
    [SerializeField]
    private Image _topRight;
    [SerializeField]
    private Image _bottomLeft;
    [SerializeField]
    private Image _bottomRight;
    [SerializeField]
    private Color[] _randomColors;

    private int _lastIndex;

    public void SetRandomColor()
    {
        int rand1 = Random.Range(0, _randomColors.Length);

        if (rand1 == _lastIndex)
        {
            rand1++;

            if (rand1 >= _randomColors.Length)
            {
                rand1 = 0;
            }
        }

        Color topColor = _randomColors[rand1];

        _topLeft.color = topColor;
        _topRight.color = topColor;

        _bottomLeft.color = topColor;
        _bottomRight.color = topColor;

        _lastIndex = rand1;
    }
}
