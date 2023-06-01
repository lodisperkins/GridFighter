using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Utility;
using Lodis.Movement;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Throws three bombs in a V shape in front of the player that detonate after a short time.
    /// </summary>
    public class DK_ScatterBomb : SummonAbility
    {
        List<PanelBehaviour> _targetPanels = new List<PanelBehaviour>();
        List<Transform> _bombs = new List<Transform>();
        private float _bombTimer;
        private GameObject _explosionEffect;
        private TimedAction _explosionTimer;
        private HitColliderData _hitColliderData;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _explosionEffect = (GameObject)Resources.Load("Effects/SmallExplosion");
            _bombTimer = abilityData.GetCustomStatValue("BombTimer");
            //Stop the timer to prevent the explosion from happening
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _bombs?.Clear();
            _targetPanels?.Clear();

            SmoothMovement = true;

            PanelPositions[0] = OwnerMoveScript.Position + Vector2.right * OwnerMoveScript.transform.forward.x;
            PanelPositions[1] = OwnerMoveScript.Position + Vector2.right * 2 * OwnerMoveScript.transform.forward.x;
            PanelPositions[2] = OwnerMoveScript.Position + Vector2.right * 2 * OwnerMoveScript.transform.forward.x;
            PanelPositions[3] = OwnerMoveScript.Position + Vector2.right * 3 * OwnerMoveScript.transform.forward.x;
        }

        private void DetonateBombs()
        {
            foreach (GridMovementBehaviour entity in ActiveEntities)
            {
                if (!entity.TryGetComponent<Collider>(out _))
                    HitColliderSpawner.SpawnBoxCollider(entity.transform, Vector3.one, _hitColliderData, owner);

                Object.Instantiate(_explosionEffect, entity.transform.position, Camera.main.transform.rotation);
                ObjectPoolBehaviour.Instance.ReturnGameObject(entity.gameObject, _hitColliderData.TimeActive + 0.1f);
            }

            _bombs.Clear();
            CameraBehaviour.ShakeBehaviour.ShakeRotation(0.5f);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);

            //Starts the bomb countdown
            _explosionTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => DetonateBombs(), TimedActionCountType.SCALEDTIME, _bombTimer);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();
            RoutineBehaviour.Instance.StopAction(_explosionTimer);
        }
    }
}