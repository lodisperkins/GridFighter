using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.Gameplay
{

    public class TrainingBackgroundBehaviour : MonoBehaviour
    {
        private BackgroundColorBehaviour _bgColor;

        // Start is called before the first frame update
        void Start()
        {
            _bgColor = GetComponent<BackgroundColorBehaviour>();
            _bgColor.SetPrimaryColor(BlackBoardBehaviour.Instance.Player1Color);
            _bgColor.SetSecondaryColor(BlackBoardBehaviour.Instance.Player2Color);
        }
    }
}