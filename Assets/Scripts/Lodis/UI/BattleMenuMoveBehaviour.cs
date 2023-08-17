using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.UI
{
    public class BattleMenuMoveBehaviour : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _leftPosition;
        [SerializeField]
        private RectTransform _rightPosition;
        private Vector3 _defaultPosition;
        [SerializeField]
        private RectTransform _rectTransform;
        [SerializeField]
        private UIColorManagerBehaviour _colorManager;

        // Start is called before the first frame update
        void Awake()
        {
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(() =>
            {
                _rectTransform.position = _defaultPosition;
                _colorManager.Alignment = GridScripts.GridAlignment.ANY;
                _colorManager.SetColors();
            });
            _defaultPosition = _rectTransform.position;
        }

        public void Move()
        {
            if (MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P2WINS)
            {
                _rectTransform.position = _leftPosition.position;
                _colorManager.Alignment = GridScripts.GridAlignment.RIGHT;
            }
            else if (MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P1WINS)
            {
                _rectTransform.position = _rightPosition.position;
                _colorManager.Alignment = GridScripts.GridAlignment.LEFT;
            }
            _colorManager.SetColors();
        }
    }
}