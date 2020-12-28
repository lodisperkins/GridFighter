using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame
{
    public class CoreBehaviour : MonoBehaviour
    {
        
        [SerializeField]
        private Event _onExplosion;
        [SerializeField]
        private List<GameObject> _pieces;
        [SerializeField]
        private HealthBehaviour _healthRef;
        private VariableScripts.IntVariable _healthStamp;
        System.Random random;
        [SerializeField]
        private float _explosionForce;
        [SerializeField]
        private float _explosionRadius;
        [SerializeField]
        private float _upwardsModifier;
        private int currentPiece;
        // Use this for initialization
        void Start()
        {
            _healthStamp = VariableScripts.IntVariable.CreateInstance(_healthRef.health.Val - 10);
            currentPiece = 0;
            if(name == "P1 Core")
            {
                BlackBoard.p1Core = gameObject;
            }
            else
            {
                BlackBoard.p2Core = gameObject;
            }
        }
        private void Awake()
        {
           
        }
        public void BlowPieceAway()
        {
            if(currentPiece < _pieces.Count)
            {
                GameObject temp = _pieces[currentPiece];
                _pieces.Remove(temp);
                temp.GetComponent<Rigidbody>().isKinematic = false;
                temp.GetComponent<Rigidbody>().AddExplosionForce(_explosionForce, temp.transform.position, _explosionRadius, _upwardsModifier, ForceMode.Impulse);
                _onExplosion.Raise(gameObject);
                Destroy(temp, 3);
                currentPiece++;
            }
        }
        private void Update()
        {
            if (_healthRef.health.Val <= _healthStamp.Val)
            {
                _healthStamp.Val = _healthRef.health.Val - 10;
                BlowPieceAway();
            }
        }
    }
}
