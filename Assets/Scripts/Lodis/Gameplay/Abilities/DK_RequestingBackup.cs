using FixedPoints;
using Lodis.AI;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_RequestingBackup : Ability
    {
        private Secur_TBehaviour _secur_T;
        private NetworkAttackNPCBehaviour _officer; 

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            MatchManagerBehaviour.Instance.AddOnMatchOverAction(CleanUpBots);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
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
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            CleanUpBots();
        }
    }
}