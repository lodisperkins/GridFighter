using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    public enum CancelPhase
    {
        STARTUP,
        ACTIVE,
        RECOVER,
        STARTUPANDACTIVE,
        STARTUPANDRECOVER,
        ACTIVEANDRECOVER,
        ALL
    }

    [System.Serializable]
    public class CancellationRule
    {
        public CancelPhase Phase;

        [Tooltip("If true, this ability can be only be cancelled after a successful hit.")]
        public bool CanOnlyCancelOnOpponentHit = true;

        [Header("Movement")]
        [Tooltip("If true, this ability can be canceled when the player inputs movement")]
        public bool CanCancelOnMove = false;

        [Header("Ability Activation")]
        [Tooltip("If true, this ability can be canceled if it is used again")]
        public bool CanCancelIntoSelf = false;
        [Tooltip("If true, this ability can be canceled if a special move is used")]
        public bool CanCancelIntoSpecial = false;
        [Tooltip("If true, this ability can be canceled if a normal move is used")]
        public bool CanCancelIntoNormal = false;

        [Header("Damage")]
        [Tooltip("If true, this ability will be canceled when the user is hit")]
        public bool cancelOnHit = false;
        [Tooltip("If true, this ability will be canceled when the user is flinching")]
        public bool cancelOnFlinch = true;
        [Tooltip("If true, this ability will be canceled when the user is in knockback")]
        public bool cancelOnKnockback = true;

        /// <summary>
        /// Checks to see if the cancellation phase that's been set matches the given phase.
        /// </summary>
        /// <param name="phase">The phase of the ability that will be used to check if this rule applies to it.</param>
        /// <returns></returns>
        public bool ComparePhase(AbilityPhase phase)
        {
            int currentPhase = (int)Phase;
            int comparePhase = (int)phase;

            //True if the phases match or the phase is all.
            if (currentPhase == comparePhase || currentPhase == 6)
                return true;

            //If the rule is not a multi-rule then return false.
            if (currentPhase <= 2)
                return false;

            //Multi-rule checks

            //If the phase in question is STARTUP and the current phase is less than STARTUPANDRECOVER then it's a valid phase.
            if (comparePhase == 0 && currentPhase <= 4)
                return true;

            //If the phase in question is ACTIVE and the current phase is STARTUPANDACTIVE or ACTIVEANDRECOVER then it's a valid phase.
            if (comparePhase == 1 && (currentPhase == 3 || currentPhase == 5))
                return true;


            //If the phase in question is RECOVER and the current phase is more than STARTUPANDACTIVE then it's a valid phase.
            if (comparePhase == 2 && currentPhase > 3)
                return true;

            return false;
        }
    }
}