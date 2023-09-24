using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class StatDescriptionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _lhsWinStat;
        [SerializeField]
        private Text _lhsDamageStat;
        [SerializeField]
        private Text _rhsWinStat;
        [SerializeField]
        private Text _rhsDamageStat;

        // Update is called once per frame
        void Update()
        {
            _lhsWinStat.text = MatchManagerBehaviour.Instance.LhsWins.ToString();
            _rhsWinStat.text = MatchManagerBehaviour.Instance.RhsWins.ToString();
            _lhsDamageStat.text = ((int)BlackBoardBehaviour.Instance.LHSTotalDamage).ToString();
            _rhsDamageStat.text = ((int)BlackBoardBehaviour.Instance.RHSTotalDamage).ToString();
        }
    }
}