using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;

public class AnimationSpeedBehaviour : StateMachineBehaviour
{

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterAnimationBehaviour animationBehaviour = animator.gameObject.GetComponent<CharacterAnimationBehaviour>();
        animationBehaviour.CalculateAnimationSpeed();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        CharacterAnimationBehaviour animationBehaviour = animator.gameObject.GetComponent<CharacterAnimationBehaviour>();
        animationBehaviour.ResetTargetSpeed();

    }
}
