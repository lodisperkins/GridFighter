using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using Lodis.Movement;
using FixedPoints;

public class AnimationSpeedBehaviour : StateMachineBehaviour
{
    [SerializeField] private bool _forceLeftRotation;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterAnimationBehaviour animationBehaviour = animator.gameObject.GetComponent<CharacterAnimationBehaviour>();
        animationBehaviour.CalculateAnimationSpeed();

        if (_forceLeftRotation)
        {
            GridMovementBehaviour gridMovementBehaviour = animationBehaviour.Entity.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.AlwaysLookAtOpposingSide = false;
            gridMovementBehaviour.FixedTransform.WorldRotation = FQuaternion.Euler(0, 90, 0);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterAnimationBehaviour animationBehaviour = animator.gameObject.GetComponent<CharacterAnimationBehaviour>();
        animationBehaviour.ResetTargetSpeed();

        if (_forceLeftRotation)
        {
            GridMovementBehaviour gridMovementBehaviour = animationBehaviour.Entity.GetComponent<GridMovementBehaviour>();
            gridMovementBehaviour.AlwaysLookAtOpposingSide = true;
        }
    }
}
