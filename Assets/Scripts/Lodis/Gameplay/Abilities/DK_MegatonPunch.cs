﻿using Lodis.FX;
using Lodis.GridScripts;
using Lodis.Utility;
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
        private float _slowMotionTimeScale;
        private float _slowMotionTime;
        private TimedAction _endTimer;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _distance = abilityData.GetCustomStatValue("Distance");
            abilityData.GetAdditionalAnimation(0, out _comboClip);


            OwnerAnimationScript.AddEventListener("Punch1", () => SpawnCollider(0));
            OwnerAnimationScript.AddEventListener("Punch2", () => SpawnCollider(0));
            OwnerAnimationScript.AddEventListener("Punch3", () => SpawnCollider(1));

            _slowMotionTimeScale = abilityData.GetCustomStatValue("SlowMotionTimeScale");
            _slowMotionTime = abilityData.GetCustomStatValue("SlowMotionTime");

            OwnerAnimationScript.AddEventListener("SlowMotionStart", () =>
            { 
                MatchManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale, 0.01f, _slowMotionTime);
            });


            OwnerAnimationScript.AddEventListener("Punch4", () =>
            {
                SpawnCollider(2);
                _endTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => EndAbility(), TimedActionCountType.SCALEDTIME, abilityData.recoverTime);
            });
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _comboStarted = false;

            _opponentMovement = BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner).GetComponent<Movement.GridMovementBehaviour>();

            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
            OwnerMoveScript.MoveToPanel(OwnerMoveScript.Position + _distance * Vector2.right * OwnerMoveScript.GetAlignmentX(), false, GridScripts.GridAlignment.ANY, true, false, true);

            OwnerMoveScript.AddOnMoveEndTempAction(EndAbility);

            FXManagerBehaviour.Instance.StartSuperMoveVisual(BlackBoardBehaviour.Instance.GetIDFromPlayer(owner), abilityData.startUpTime);
        }

        private void StartCombo()
        {
            PauseAbilityTimer();

            _comboStarted = true;
            OwnerMoveScript.DisableMovement(condition => !_comboStarted, false, true);

            OwnerAnimationScript.PlayAnimation(_comboClip, 1, true);
        }

        public void SpawnCollider(int colliderIndex)
        {
            Vector3 spawnPosition = owner.transform.position + (Vector3.right * OwnerMoveScript.GetAlignmentX()) + Vector3.up;
            HitColliderSpawner.SpawnBoxCollider(spawnPosition, Vector3.one, GetColliderData(colliderIndex), owner);
        }

        public override void FixedUpdate()
        {

            float distance = Vector3.Distance(owner.transform.position, _opponentMovement.transform.position);

            if (distance <= 1.5f && !_comboStarted)
            {
                StartCombo();
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            _comboStarted = false;
            OwnerMoveScript.EnableMovement();
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            RoutineBehaviour.Instance.StopAction(_endTimer);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            RoutineBehaviour.Instance.StopAction(_endTimer);

            FXManagerBehaviour.Instance.StopAllSuperMoveVisuals();
            MatchManagerBehaviour.Instance.SuperInUse = false;
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}