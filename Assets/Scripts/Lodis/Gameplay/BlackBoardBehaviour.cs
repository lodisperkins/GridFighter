using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class BlackBoardBehaviour : MonoBehaviour
    {
        public static GridScripts.GridBehaviour Grid { get; private set; }

        private void Awake()
        {
            Grid = FindObjectOfType<GridScripts.GridBehaviour>();
        }
    }
}

