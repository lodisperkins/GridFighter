using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame
{

    public class PlayerAnimationBehaviour : MonoBehaviour
    {
        //the animator attached to this gameobject
        [SerializeField] private Animator _animator;

        public void EnableMoveAnimation()
        {
            if (_animator != null)
            {
                _animator.SetBool("IsMoving", true);
            }

        }

        public void DisableMoveAnimation()
        {
            if (_animator != null)
                _animator.SetBool("IsMoving", false);
        }

        public void EnableAttackAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("OnShoot");
        }
        public void EnableSpawnAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("OnSpawn");
        }
        public void EnableOverdriveAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("OnOverdriveActivated");
        }
        public void EnableHitAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("Hit");
        }
        private void OnTriggerEnter(Collider other)
        {
            if (_animator != null && _animator.GetBool("Squat") == false)
                _animator.SetBool("Squat", true);

        }

        private void OnTriggerExit(Collider other)
        {
            _animator.SetBool("Squat", false);
        }
    }
}
