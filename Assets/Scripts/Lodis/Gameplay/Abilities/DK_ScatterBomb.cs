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

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            _bombMoveDuration = abilityData.GetCustomStatValue("BombMoveDuration");
            _explosionEffect = (GameObject)Resources.Load("Effects/Explosion");
            _bombTimer = abilityData.GetCustomStatValue("BombTimer");
            base.Init(newOwner);
        }

        private void DetonateBombs()
        {
            foreach(Transform t in _bombs)
            {
                HitColliderSpawner.SpawnBoxCollider(t, Vector3.one, ProjectileCollider);
                MonoBehaviour.Instantiate(_explosionEffect, t);
            }
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            PanelBehaviour panel = null;

            if (BlackBoardBehaviour.Instance.Grid.GetPanel(_ownerMoveScript.Position + Vector2.right * _ownerMoveScript.transform.forward.x, out panel))
                _targetPanels.Add(panel);
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(_ownerMoveScript.Position + Vector2.right * _ownerMoveScript.transform.forward.x + Vector2.up, out panel))
                _targetPanels.Add(panel);
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(_ownerMoveScript.Position + Vector2.right * _ownerMoveScript.transform.forward.x + Vector2.down, out panel))
                _targetPanels.Add(panel);

            for (int i = 0; i < abilityData.GetCustomStatValue("BombCount"); i++)
            {
                _bombs.Add(MonoBehaviour.Instantiate(abilityData.visualPrefab, SpawnTransform.position, SpawnTransform.rotation).transform);
                _bombs[i].DOMove(_targetPanels[i].Position, _bombMoveDuration);
                //MonoBehaviour.Destroy(_bombs[i], _bombTimer);
            }

            RoutineBehaviour.Instance.StartNewTimedAction(args => DetonateBombs(), TimedActionCountType.SCALEDTIME, _bombTimer);
        }
    }
}