using FixedPoints;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using UnityEngine;

public class AnimationOrientationBehaviour : StateMachineBehaviour
{
    [SerializeField] private bool _resetWhenExiting = true;

    private GridMovementBehaviour _gridMovement;

    private void Awake()
    {
        MatchManagerBehaviour manager = MatchManagerBehaviour.Instance;

        if (manager != null)
        {
            manager.AddOnMatchRestartAction(() =>
            {
                if (_gridMovement)
                    _gridMovement.AlwaysLookAtOpposingSide = true;
            });
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);

        _gridMovement = animator.gameObject.GetComponentInParent<GridMovementBehaviour>();

        _gridMovement.AlwaysLookAtOpposingSide = false;

        if (_gridMovement.Alignment == GridAlignment.RIGHT)
            _gridMovement.FixedTransform.WorldRotation = FQuaternion.Euler(0, 90, 0);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);

        _gridMovement.AlwaysLookAtOpposingSide = _resetWhenExiting;
    }
}
