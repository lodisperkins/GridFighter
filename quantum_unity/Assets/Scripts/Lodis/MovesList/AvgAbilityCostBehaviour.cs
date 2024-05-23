using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Numerics;
using System;
namespace Lodis.UI
{
    public class AvgAbilityCostBehaviour : MonoBehaviour
    {
        [Tooltip("The text box to update with the average cost.")]
        [SerializeField]
        private Text _valText;
        [Tooltip("The text box to update with the average cost.")]
        [SerializeField]
        private Text _descriptionText;
        [SerializeField]
        private string _meterDescription;
        [Tooltip("The special ability buttons to use for calculating the average.")]
        [SerializeField]
        private MoveDescriptionBehaviour[] _abilityButtons;

        void Start()
        {
            _descriptionText.text = _meterDescription + ": ";
        }

        private double CalculateAverage()
        {
            double average = 0;  

            for (int i = 0; i < _abilityButtons.Length; i++)
            {
                average += _abilityButtons[i].Data.EnergyCost;
            }

            average /= _abilityButtons.Length;

            return average;
        }

        // Update is called once per frame
        void Update()
        {
            double average = CalculateAverage();

            average = Math.Round(average, 1);
            _valText.text = average.ToString();

            _valText.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)average];
        }
    }
}