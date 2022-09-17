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
        private float[] _emissionStrengthValues = { 0, 0 };

        public GameObject Explosion { get => _explosion; set => _explosion = value; }
        public float ExplosionChargeTime { get => _explosionChargeTime; set => _explosionChargeTime = value; }

        public void ChargeExplosion(IntVariable playerID)
        {
            if (_emissionStrengthValues[playerID.Value - 1] != 0)
                return;

            GameObject playerCharacter = BlackBoardBehaviour.Instance.GetPlayerFromID(playerID);
            playerCharacter.GetComponent<GridPhysicsBehaviour>().FreezeInPlaceByTimer(_explosionChargeTime, false, true);
            _meshRenderer = playerCharacter.GetComponentInChildren<SkinnedMeshRenderer>();
            Texture emissionTexture = _meshRenderer.material.GetTexture("_Emission");
            _meshRenderer.material.SetTexture("_Emission", null);
            float emission = _meshRenderer.material.GetFloat("_EmissionStrength");

            _emissionStrengthValues[playerID.Value - 1] = emission;

            DOTween.To(() => emission, time => emission = time, _maxEmission, _explosionChargeTime).
                OnUpdate( () =>
                {
                    _meshRenderer.material.SetFloat("_EmissionStrength", emission);
                }).
                OnComplete( () => 
                {
                    playerCharacter.SetActive(false);
                    _meshRenderer.material.SetTexture("_Emission", emissionTexture);
                    Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                    playerCharacter.GetComponent<KnockbackBehaviour>().HasExploded = true;
                    CameraBehaviour.ShakeBehaviour.ShakeRotation();
                });
            
        }

        public void ResetEmission(IntVariable playerID)
        {
            if (_emissionStrengthValues[playerID.Value - 1] == 0)
                return;

            GameObject playerCharacter = BlackBoardBehaviour.Instance.GetPlayerFromID(playerID);
            _meshRenderer = playerCharacter.GetComponentInChildren<SkinnedMeshRenderer>();

            float emission = _emissionStrengthValues[playerID.Value - 1];
            _meshRenderer.material.SetFloat("_EmissionStrength", emission);
            _emissionStrengthValues[playerID.Value - 1] = 0;
        }
    }
}
