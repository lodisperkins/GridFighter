using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Input
{
    public interface IControllable
    {
        /// <summary>
        /// The ID number of the player using this component
        /// </summary>
        IntVariable PlayerID
        {
            get;set;
        }

        GameObject Character
        {
            get;
            set;
        }

        bool Enabled
        {
            get;
            set;
        }
    }
}
