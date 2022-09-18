using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;

namespace Lodis.Gameplay
{
    public class CharacterExplosionBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _explosion;
        [SerializeField] private float _explosionChargeTime;
        private CharacterFeedbackBehaviour _characterFeedback;
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
            _characterFeedback = playerCharacter.GetComponentInChildren<CharacterFeedbackBehaviour>();

            float strength = _characterFeedback.EmissionStrength;
            float oldTime = _characterFeedback.TimeBetweenFlashes;

            _characterFeedback.EmissionStrength = _maxEmission;
            _characterFeedback.FlashAllRenderers(BlackBoardBehaviour.Instance.GetPlayerColorByID(playerID));
            _characterFeedback.TimeBetweenFlashes = _explosionChargeTime;

            RoutineBehaviour.Instance.StartNewTimedAction( args => 
            {
                playerCharacter.SetActive(false);
                Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                playerCharacter.GetComponent<KnockbackBehaviour>().HasExploded = true;
                CameraBehaviour.ShakeBehaviour.ShakeRotation();

                _characterFeedback.EmissionStrength = strength;
               _characterFeedback.TimeBetweenFlashes = oldTime;
            }, TimedActionCountType.SCALEDTIME, ExplosionChargeTime);
        }

        public void ResetEmission(IntVariable playerID)
        {
            _characterFeedback?.ResetAllRenderers();
        }
    }
}
