﻿namespace Ilumisoft.VisualStateMachine
{
    using UnityEngine;

    [System.Serializable]
    public class LabelSettings
    {
        [SerializeField]
        private bool isEnabled = true;

        /// <summary>
        /// Enables or disables the grid
        /// </summary>
        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.isEnabled = value;
        }
    }
}