using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Lodis.Sound;

namespace Lodis.Gameplay
{
    public class CharacterExplosionBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _explosion;
        [SerializeField] private float _explosionChargeTime;
        private CharacterFeedbackBehaviour _characterFeedback;
        [SerializeField] private float _maxEmission;
        [SerializeField] private AudioClip _chargeSound;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private GridGame.Event _onCharacterExplosion;
        private float[] _emissionStrengthValues = { 0, 0 };
        private TimedAction _chargeAction;
        private IntVariable _lastLoserID;

        public GameObject Explosion { get => _explosion; set => _explosion = value; }
        public float ExplosionChargeTime { get => _explosionChargeTime; set => _explosionChargeTime = value; }
        public TimedAction ChargeAction { get => _chargeAction; private set => _chargeAction = value; }

        public void Start()
        {
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(
                 () => RoutineBehaviour.Instance.StopAction(_chargeAction)
                );
        }

        public void ChargeExplosion(IntVariable playerID)
        {
            GameObject playerCharacter = BlackBoardBehaviour.Instance.GetPlayerFromID(playerID);
            playerCharacter.GetComponent<GridPhysicsBehaviour>().FreezeInPlaceByTimer(_explosionChargeTime, false, true);
            KnockbackBehaviour knockback = playerCharacter.GetComponent<KnockbackBehaviour>();
            
            if (knockback.OutOfBounds)
                return;

            knockback.OutOfBounds = true;

            _characterFeedback = playerCharacter.GetComponentInChildren<CharacterFeedbackBehaviour>();

            float strength = _characterFeedback.EmissionStrength;
            float oldTime = _characterFeedback.TimeBetweenFlashes;

            _characterFeedback.EmissionStrength = _maxEmission;
            _characterFeedback.FlashAllRenderers(BlackBoardBehaviour.Instance.GetPlayerColorByID(playerID));
            _characterFeedback.TimeBetweenFlashes = _explosionChargeTime;
            SoundManagerBehaviour.Instance.PlaySound(_chargeSound);
            SoundManagerBehaviour.Instance.TogglePauseMusic();
            MatchManagerBehaviour.Instance.ChangeTimeScale(0.2f, ExplosionChargeTime, ExplosionChargeTime);

            ChargeAction = RoutineBehaviour.Instance.StartNewTimedAction( args =>
            {
                knockback.HasExploded = true;
                _characterFeedback.EmissionStrength = strength;
               _characterFeedback.TimeBetweenFlashes = oldTime;
                playerCharacter.SetActive(false);
                Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                CameraBehaviour.ShakeBehaviour.ShakeRotation();
                SoundManagerBehaviour.Instance.PlaySound(_explosionSound);
                SoundManagerBehaviour.Instance.TogglePauseMusic();
                _onCharacterExplosion.Raise();

            }, TimedActionCountType.UNSCALEDTIME, ExplosionChargeTime);

            ChargeAction.OnCancel += () =>
            {
                _characterFeedback.EmissionStrength = strength;
                _characterFeedback.TimeBetweenFlashes = oldTime;
            };
        }

        public void ResetEmission(IntVariable playerID)
        {
            _characterFeedback?.ResetAllRenderers();
        }
    }
}
