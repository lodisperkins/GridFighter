using FixedPoints;
using Lodis.AI;
using Lodis.FX;
using Lodis.Input;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace Lodis.Gameplay
{
    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_RequestingBackup : Ability
    {
        private Secur_TBehaviour _secur_T;
        private NetworkAttackNPCBehaviour _officer;
        private IControllable _controller;
        private FixedTimeAction _despawnTimer;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            MatchManagerBehaviour.Instance.AddOnMatchOverAction(CleanUpBots);
            _controller = Owner.GetComponentInParent<IControllable>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);


            SetTimeUnit(FixedTimeAction.UnitOfTime.PauseScaled);
            FXManagerBehaviour.Instance.StartSuperMoveVisual(_controller.PlayerID, abilityData.startUpTime);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            FXManagerBehaviour.Instance.EnableSuperBackground(_controller.PlayerID);
            if (_secur_T == null)
            {
                GameObject botInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, Owner.transform.position, Quaternion.identity);
                _secur_T = botInstance.GetComponent<Secur_TBehaviour>();
            }
            else
            {
                _secur_T.Entity.AddToGame();
                _secur_T.ResetTimer();
            }

            GameObject opp = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);

            Fixed32 speed = abilityData.GetCustomStatValue("ProjectileSpeed");

            _secur_T.Initialize(Owner, opp.GetComponent<EntityDataBehaviour>(), GetColliderData(0), speed);
            _secur_T.FireRange = 6;
            _secur_T.FollowSpeed = 0;
            _secur_T.FixedTransform.WorldPosition = Owner.FixedTransform.WorldPosition + FVector3.Right * OwnerMoveScript.GetAlignmentX() + FVector3.Up * Fixed32.PointFive;
            _secur_T.LookAtTarget = false;


            if (_officer == null)
            {
                GameObject officerInstance = MonoBehaviour.Instantiate(abilityData.Effects[0], Owner.transform.position, Quaternion.identity);
                _officer = officerInstance.GetComponent<NetworkAttackNPCBehaviour>();
            }
            else
            {
                _officer.Entity.AddToGame();
            }

            _officer.Owner = Owner;
            _officer.MovementBehaviour.Position = OwnerMoveScript.CurrentPanel.Position + new FVector2(OwnerMoveScript.GetAlignmentX(), 0);
            _officer.MovementBehaviour.Alignment = OwnerMoveScript.Alignment;

            if (_despawnTimer == null)
            {
                _despawnTimer = FixedPointTimer.StartNewTimedAction(CleanUpBots, 16);
            }
            else
            {
                _despawnTimer.Reset();
            }
        }

        protected void CleanUpBots()
        {
            if (_secur_T != null)
            {
                _secur_T.Deactivate();
            }

            if (_officer != null)
            {
                _officer.Entity.RemoveFromGame();
            }

            FXManagerBehaviour.Instance.DisableSuperBackground();
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            CleanUpBots();
        }
    }
}