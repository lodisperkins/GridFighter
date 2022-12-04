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

        Vector2 AttackDirection
        {
            get;
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
