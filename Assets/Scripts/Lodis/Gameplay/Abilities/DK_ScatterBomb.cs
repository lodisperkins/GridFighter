using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Utility;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Throws three bombs in a V shape in front of the player that detonate after a short time.
    /// </summary>
    public class DK_ScatterBomb : ProjectileAbility
    {
        List<PanelBehaviour> _targetPanels = new List<PanelBehaviour>();
        List<Transform> _bombs = new List<Transform>();
        private float _bombMoveDuration;
        private float _bombTimer;
        private GameObject _explosionEffect;
        private TimedAction _explosionTimer;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            _bombMoveDuration = abilityData.GetCustomStatValue("BombMoveDuration");
            _explosionEffect = (GameObject)Resources.Load("Effects/SmallExplosion");
            _bombTimer = abilityData.GetCustomStatValue("BombTimer");
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _bombs?.Clear();
            _targetPanels?.Clear();
        }

        private void DetonateBombs()
        {
            foreach (Transform t in _bombs)
            {
                if (!t.TryGetComponent<Collider>(out _))
                    HitColliderSpawner.SpawnBoxCollider(t, Vector3.one, ProjectileColliderData, owner);

                Object.Instantiate(_explosionEffect, t.position, Camera.main.transform.rotation);
                t.GetComponent<MeshRenderer>().enabled = false;
                ObjectPoolBehaviour.Instance.ReturnGameObject(t.gameObject, ProjectileColliderData.TimeActive + 0.1f);
                t.DOKill();
            }

            _bombs.Clear();
            CameraBehaviour.ShakeBehaviour.ShakeRotation(0.5f);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            PanelBehaviour panel = null;

            //Gets the panels in front of the character to form a "v" shape with the bombs
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(OwnerMoveScript.Position + Vector2.right * OwnerMoveScript.transform.forward.x, out panel))
                _targetPanels.Add(panel);
            if (BlackBoardBehaviour.Instance.Grid.GetPanel((OwnerMoveScript.Position + Vector2.right * 2 * OwnerMoveScript.transform.forward.x) + Vector2.up, out panel))
                _targetPanels.Add(panel);
            if (BlackBoardBehaviour.Instance.Grid.GetPanel((OwnerMoveScript.Position + Vector2.right * 2 * OwnerMoveScript.transform.forward.x) + Vector2.down, out panel))
                _targetPanels.Add(panel);
            if (BlackBoardBehaviour.Instance.Grid.GetPanel((OwnerMoveScript.Position + Vector2.right * 3 * OwnerMoveScript.transform.forward.x), out panel))
                _targetPanels.Add(panel);

            //Spawn each bomb and move them into position
            for (int i = 0; i < _targetPanels.Count; i++)
            {
                _bombs.Add(ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation).transform);
                _bombs[i].GetComponent<Renderer>().enabled = true;
                _bombs[i].DOMove(_targetPanels[i].transform.position, _bombMoveDuration);
            }

            //Starts the bomb countdown
            _explosionTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => DetonateBombs(), TimedActionCountType.SCALEDTIME, _bombTimer);
        }

        public override void StopAbility()
        {
            base.StopAbility();

            //Stop the timer to prevent the explosion from happening
            RoutineBehaviour.Instance.StopAction(_explosionTimer);

            //Disable each bomb in the scene
            foreach (Transform t in _bombs)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(t.gameObject);
            }

            _targetPanels?.Clear();

        }
    }
}