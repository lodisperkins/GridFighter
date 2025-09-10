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
        [SerializeField] private float _maxEmission;
        [SerializeField] private AudioClip _chargeSound;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private CustomEventSystem.Event _onCharacterExplosion;

        //---
        private CharacterVoiceBehaviour _characterVoice;
        private float[] _emissionStrengthValues = { 0, 0 };
        private CharacterFeedbackBehaviour[] _characterFeedbacks = { null, null };
        private GridMovementBehaviour[] _characterMovement = { null, null };
        private TimedAction _chargeAction;
        private IntVariable _lastLoserID;

        public delegate void CharacterExplosionEvent(int index);
        public event CharacterExplosionEvent OnCharacterExplosionStart;
        public event CharacterExplosionEvent OnCharacterExplosion;

        public GameObject Explosion { get => _explosion; set => _explosion = value; }
        public Fixed32 ExplosionChargeTime { get => _explosionChargeTime; set => _explosionChargeTime = value; }
        public TimedAction ChargeAction { get => _chargeAction; private set => _chargeAction = value; }
        public bool ExplodingPlayer1 { get; private set; }
        public bool ExplodingPlayer2 { get; private set; }

        public void Start()
        {
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(
                 () => RoutineBehaviour.Instance.StopAction(_chargeAction)
                );
        }

        public void ChargeExplosion(IntVariable playerID)
        {
            GameObject playerCharacter = BlackBoardBehaviour.Instance.GetPlayerFromID(playerID);

            GridPhysicsBehaviour physics = playerCharacter.GetComponent<GridPhysicsBehaviour>();
            physics.FreezeInPlaceByTimer(_explosionChargeTime, false, true);

            _characterMovement[playerID.Value] = physics.MovementBehaviour;
            //_characterMovement[playerID.Value].TickEnabled = false;

            KnockbackBehaviour knockback = playerCharacter.GetComponent<KnockbackBehaviour>();
            
            if (knockback.OutOfBounds)
                return;

            if (playerID.Value == 0)
                ExplodingPlayer1 = true;
            else
                ExplodingPlayer2 = true;

            OnCharacterExplosionStart?.Invoke(playerID);

            knockback.OutOfBounds = true;

            _characterFeedbacks[playerID.Value] = playerCharacter.GetComponentInChildren<CharacterFeedbackBehaviour>();
            _characterVoice = playerCharacter.GetComponentInChildren<CharacterVoiceBehaviour>();

            float strength = _characterFeedbacks[playerID.Value].EmissionStrength;
            float oldTime = _characterFeedbacks[playerID.Value].TimeBetweenFlashes;

            _characterFeedbacks[playerID.Value].EmissionStrength = _maxEmission;
            _characterFeedbacks[playerID.Value].FlashAllRenderers(BlackBoardBehaviour.Instance.GetPlayerColorByID(playerID));
            _characterFeedbacks[playerID.Value].TimeBetweenFlashes = _explosionChargeTime;

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
                _characterFeedbacks[playerID.Value].EmissionStrength = strength;
               _characterFeedbacks[playerID.Value].TimeBetweenFlashes = oldTime;
                playerCharacter.SetActive(false);

                GameObject explosion = Instantiate(_explosion, playerCharacter.transform.position, playerCharacter.transform.rotation);
                ParticleColorManagerBehaviour colorManager = explosion.GetComponent<ParticleColorManagerBehaviour>();
                colorManager.Alignment = playerID == 0 ? GridScripts.GridAlignment.LEFT : GridScripts.GridAlignment.RIGHT;
                colorManager.SetColors();

                CameraBehaviour.ShakeBehaviour.ShakeRotation(1, 4, 90);

                SoundManagerBehaviour.Instance.PlaySound(_explosionSound, 2);
                SoundManagerBehaviour.Instance.TogglePauseMusic();

                _onCharacterExplosion.Raise();
                OnCharacterExplosion?.Invoke(playerID);
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);

                if (playerID.Value == 0)
                    ExplodingPlayer1 = false;
                else
                    ExplodingPlayer2 = false;

            }, TimedActionCountType.UNSCALEDTIME, ExplosionChargeTime);

            ChargeAction.OnCancel += () =>
            {
                _characterFeedbacks[playerID.Value].EmissionStrength = strength;
                _characterFeedbacks[playerID.Value].TimeBetweenFlashes = oldTime;
                FXManagerBehaviour.Instance.SetEnvironmentLightsEnabled(true);
                CameraBehaviour.Instance.ClampX = true;
                CameraBehaviour.Instance.ZoomAmount = 0;
            };
        }

        private void EnableMovement(params object[] args)
        {
            _characterMovement[0].TickEnabled = true;
            _characterMovement[1].TickEnabled = true;
        }

        public void ResetEmission(IntVariable playerID)
        {
            for (int i = 0; i < _characterFeedbacks.Length; i++)
            {
                GridMovementBehaviour movement = _characterMovement[i];
                if (_characterFeedbacks[i] != null)
                {
                    _characterFeedbacks[i].EmissionStrength = _emissionStrengthValues[i];
                    _characterFeedbacks[i].ResetAllRenderers();
                    //RoutineBehaviour.Instance.StartNewTimedAction(a => movement.TickEnabled = true, TimedActionCountType.UNSCALEDTIME, 0.01f);
                }
            }

            
            CameraBehaviour.Instance.ClampX = true;
            CameraBehaviour.Instance.ZoomAmount = 0;
        }
    }
}
