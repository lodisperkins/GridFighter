using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class MatchTimerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _timerText;
        [SerializeField]
        private FloatVariable _matchTime;
        [SerializeField]
        private float _matchTimeRemaining;

        // Start is called before the first frame update
        void Start()
        {
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchRestartAction(() => _matchTimeRemaining = _matchTime.Value);
            _matchTimeRemaining = _matchTime.Value;
        }

        // Update is called once per frame
        void Update()
        {
            _matchTimeRemaining -= Time.deltaTime;
            _timerText.text = Mathf.Ceil(_matchTimeRemaining).ToString();
        }
    }
}
