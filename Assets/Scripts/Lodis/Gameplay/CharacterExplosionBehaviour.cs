using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Lodis.Sound;
using Lodis.FX;
using Types;

namespace Lodis.Gameplay
{
    public class CharacterExplosionBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _explosion;
        [SerializeField] private Fixed32 _explosionChargeTime;
        private CharacterFeedbackBehaviour _characterFeedback;
        [SerializeField] private float _maxEmission;
        [SerializeField] private AudioClip _chargeSound;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private CustomEventSystem.Event _onCharacterExplosion;
        private CharacterVoiceBehaviour _characterVoice;
        private float[] _emissionStrengthValues = { 0, 0 };
        private TimedAction _chargeAction;
        private IntVariable _lastLoserID;

        public GameObject Explosion { get => _explosion; set => _explosion = value; }
        public Fixed32 ExplosionChargeTime { get => _explosionChargeTime; set => _explosionChargeTime = value; }
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
            _characterVoice = playerCharacter.GetComponentInChildren<CharacterVoiceBehaviour>();
            float strength = _characterFeedback.EmissionStrength;
            float oldTime = _characterFeedback.TimeBetweenFlashes;

            _characterFeedback.EmissionStrength = _maxEmission;
            _characterFeedback.FlashAllRenderers(BlackBoardBehaviour.Instance.GetPlayerColorByID(playerID));
            _characterFeedback.TimeBetweenFlashes = _explosionChargeTime;

            FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(false);
            SoundManagerBehaviour.Instance.PlaySound(_chargeSound);
            SoundManagerBehaviour.Instance.TogglePauseMusic();


            MatchManagerBehaviour.Instance.ChangeTimeScale(new Types.Fixed32(13107), ExplosionChargeTime, ExplosionChargeTime);
            _characterVoice.PlayDeathSound();

            CameraBehaviour.Instance.ClampX = false;
            CameraBehaviour.Instance.ZoomAmount = 2;
            CameraBehaviour.Instance.AlignmentFocus = GridScripts.GridAlignment.ANY;
            BlackBoardBehaviour.Instance.DisableAllAbilityColliders();

            ChargeAction = RoutineBehaviour.Instance.StartNewTimedAction( args =>
            {
                knockback.HasExploded = true;
                _characterFeedback.EmissionStrength = strength;
               _characterFeedback.TimeBetweenFlashes = oldTime;
                playerCharacter.SetActive(false);

                GameObject explosion = Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                ParticleColorManagerBehaviour colorManager = explosion.GetComponent<ParticleColorManagerBehaviour>();
                colorManager.Alignment = playerID == 1 ? GridScripts.GridAlignment.LEFT : GridScripts.GridAlignment.RIGHT;
                colorManager.SetColors();

                CameraBehaviour.ShakeBehaviour.ShakeRotation(1, 4, 90);

                SoundManagerBehaviour.Instance.PlaySound(_explosionSound, 2);
                SoundManagerBehaviour.Instance.TogglePauseMusic();

                _onCharacterExplosion.Raise();
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);

            }, TimedActionCountType.UNSCALEDTIME, ExplosionChargeTime);

            ChargeAction.OnCancel += () =>
            {
                _characterFeedback.EmissionStrength = strength;
                _characterFeedback.TimeBetweenFlashes = oldTime;
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);
                CameraBehaviour.Instance.ClampX = true;
                CameraBehaviour.Instance.ZoomAmount = 0;
            };
        }

        public void ResetEmission(IntVariable playerID)
        {
            _characterFeedback?.ResetAllRenderers();
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ZoomAmount = 0;
        }
    }
}
