using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour
    {
        private Gameplay.MovesetBehaviour _moveset;
        [SerializeField]
        private Gameplay.BasicAbilityType _attackType;
        [SerializeField]
        private float _attackDelay;
        [SerializeField]
        private float _attackStrength;
        [SerializeField]
        private float _zDirection;
        private Gameplay.PlayerStateManagerBehaviour _playerState;

        // Start is called before the first frame update
        void Start()
        {
            _moveset = GetComponent<Gameplay.MovesetBehaviour>();
            _playerState = GetComponent<Gameplay.PlayerStateManagerBehaviour>();
            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            while (_playerState.CurrentState == Gameplay.PlayerState.IDLE)
            {
                yield return new WaitForSeconds(_attackDelay);
                _zDirection = Mathf.Clamp(_zDirection, -1, 1);
                _moveset.UseBasicAbility(_attackType, (_attackStrength, _zDirection));
            }
        }
    }
}
