using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WN_EnergyMine : SummonAbility
    {
        private HitColliderData _explosionColliderData;
        private float _travelDistance;
        private  float _despawnTime;
        private TimedAction _despawnAction;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _travelDistance = abilityData.GetCustomStatValue("TravelDistance");

            Vector2 direction = owner.transform.forward;

            PanelPositions[0] = OwnerMoveScript.Position + direction * _travelDistance;

            _despawnTime = abilityData.GetCustomStatValue("DespawnTime");
            _explosionColliderData = GetColliderData(0);
            SmoothMovement = true;
            RoutineBehaviour.Instance.StopAction(_despawnAction);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Spawn remote bomb if none are out.
            if (ActiveEntities.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            base.OnActivate(args);
            //The base activate func fires a single instance of the projectile when called


            HitColliderBehaviour colliderBehaviour = ActiveEntities[0].GetComponent<HitColliderBehaviour>();
            colliderBehaviour.ColliderInfo = _explosionColliderData;
            colliderBehaviour.Owner = owner;

            //Collider collider = ActiveEntities[0].GetComponentInChildren<Collider>();
            //collider.enabled = false;
            //ActiveEntities[0].AddOnMoveEndTempAction(() =>
            //{
            //    collider.enabled = true;
            //});

            _despawnAction = RoutineBehaviour.Instance.StartNewTimedAction(context => DisableAllEntities(), TimedActionCountType.SCALEDTIME, _despawnTime);
        }
    }
}