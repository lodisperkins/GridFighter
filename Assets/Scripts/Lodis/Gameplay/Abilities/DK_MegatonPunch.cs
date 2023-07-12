using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_MegatonPunch : Ability
    {
        private float _distance;
        private bool _comboStarted;
        private Movement.GridMovementBehaviour _opponentMovement;
        private AnimationClip _comboClip;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _distance = abilityData.GetCustomStatValue("Distance");
            abilityData.GetAdditionalAnimation(0, out _comboClip);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<Movement.GridMovementBehaviour>();

            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
            OwnerMoveScript.MoveToPanel(OwnerMoveScript.Position + _distance * Vector2.right * OwnerMoveScript.GetAlignmentX(), false, GridScripts.GridAlignment.ANY, true, false, true);
        }

        private void StartCombo()
        {
            PauseAbilityTimer();

            OwnerMoveScript.CancelMovement();

            OwnerAnimationScript.PlayAnimation(_comboClip, 1, true);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

        }

        public override void Update()
        {
            base.Update();

            if (OwnerMoveScript.Position + Vector2.right * -OwnerMoveScript.GetAlignmentX() == _opponentMovement.Position && !_comboStarted)
            {
                StartCombo();
                _comboStarted = true;
            }
        }
    }
}