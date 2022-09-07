using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Movement;
using Lodis.ScriptableObjects;

namespace Lodis.Gameplay
{
    public class CharacterExplosionBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _explosion;
        [SerializeField] private float _explosionChargeTime;
        private Renderer _meshRenderer;
        [SerializeField] private float _maxEmission;

        public GameObject Explosion { get => _explosion; set => _explosion = value; }
        public float ExplosionChargeTime { get => _explosionChargeTime; set => _explosionChargeTime = value; }

        public void ChargeExplosion(IntVariable playerID)
        {
            GameObject playerCharacter = BlackBoardBehaviour.Instance.GetPlayerFromID(playerID);
            playerCharacter.GetComponent<GridPhysicsBehaviour>().FreezeInPlaceByTimer(_explosionChargeTime, false, true);
            _meshRenderer = playerCharacter.GetComponentInChildren<SkinnedMeshRenderer>();

            float emission = _meshRenderer.material.GetFloat("_EmissionStrength");

            DOTween.To(() => emission, time => emission = time, _maxEmission, _explosionChargeTime).
                OnUpdate( () =>
                {
                    _meshRenderer.material.SetFloat("_EmissionStrength", emission);
                }).
                OnComplete( () => 
                {
                    playerCharacter.SetActive(false);
                    Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                    CameraBehaviour.ShakeBehaviour.ShakeRotation();
                });
            
        }
    }
}
