using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GridGame
{
    public class GunBehaviour : MonoBehaviour
    {
        //the bullet the gun will be firing
        [FormerlySerializedAs("Bullet")] [SerializeField]
        private GameObject bullet;
        //the player the owns this gun
        [FormerlySerializedAs("Owner")] public string owner;
        //temporary gameobject used to modify the bullets transform
        private GameObject _tempBullet;
        //the amount the bullet force is scale up by
        [FormerlySerializedAs("Bullet_Force_Scale")] [SerializeField]
        public int bulletForceScale;
        //the total amount of force used to move the bullet
        public Vector3 _bulletForce;
        //the amount of time between firing bullets
        [FormerlySerializedAs("Bullet_Delay")] [SerializeField]
        public float bulletDelay;
        //the amount of bullet to fire
        [FormerlySerializedAs("Bullet_Count")] public int bulletCount;
        [SerializeField]
        private bool _hasLimitedAmmo = true;
        private int _currentAmmo;
        //unity event raised when the gun is out of ammo
        [FormerlySerializedAs("OutOfAmmo")] [SerializeField]
        private UnityEvent outOfAmmo;
        //temporary transform for the bullet
        private Transform _tempTransform;
        //temporary rigidbody for the bullet
        private Rigidbody _tempRigidBody;
        //If this gun is turret it fires automatically, otherwise it waits for player input tp fire
        [SerializeField] private bool _isTurret;
        [SerializeField] private Event _onShotFired;
        // the amount of damge the bullet should deal
        [FormerlySerializedAs("DamageVal")] public int damageVal;
        [SerializeField]
        public BlockBehaviour block;
        private BulletBehaviour _bulletScript;
        public int CurrentAmmo
        {
            get { return _currentAmmo; }
        }

        public UnityEvent OutOfAmmo
        {
            get { return outOfAmmo; }
            set { outOfAmmo = value; }
        }

        public bool IsTurret
        {
            get
            {
                return _isTurret;
            }

            set
            {
                _isTurret = value;
            }
        }

        // Use this for initialization
        void Start()
        {
            if (IsTurret)
            {
                StartCoroutine(Fire());
            }

            _currentAmmo = bulletCount;
        }
        //fires a bullet with a specified interval of time
        public IEnumerator Fire()
        {
            for (int i = 0; i < bulletCount; i++)
            {
                if (IsTurret)
                {
                    if(_hasLimitedAmmo== false)
                    {
                        i--;
                    }
                    FireBullet();
                    _currentAmmo = bulletCount - i;
                    yield return new WaitForSeconds(bulletDelay);
                    continue;
                }
                yield break;
            }
            outOfAmmo.Invoke();
        }
        public void ChangeBullet(GameObject newBullet)
        {
            bullet = newBullet;
        }
        public void FireBullet(Vector3 position)
        {
            _tempBullet = Instantiate(bullet, position, transform.rotation);
            _bulletScript = _tempBullet.GetComponent<BulletBehaviour>();
            _bulletScript.Owner = owner;
            _tempBullet.transform.Rotate(new Vector3(90, 0));
            _bulletScript.block = block;
            _tempRigidBody = _tempBullet.GetComponent<Rigidbody>();
            _bulletForce = transform.forward * bulletForceScale;
            _bulletScript.bulletForce = _bulletForce;
            if(!_bulletScript.OverrideGunForce)
            {
                _tempRigidBody.AddForce(_bulletForce);
            }
            _onShotFired.Raise(gameObject);
        }
        public void FireBullet()
        {
            _tempBullet = Instantiate(bullet, transform.position, transform.rotation);
            _bulletScript = _tempBullet.GetComponent<BulletBehaviour>();
            _bulletScript.Owner = owner;
            _tempBullet.transform.Rotate(new Vector3(90, 0));
            _tempRigidBody = _tempBullet.GetComponent<Rigidbody>();
            _bulletForce = transform.forward * bulletForceScale;
            if (!_bulletScript.OverrideGunForce)
            {
                _tempRigidBody.AddForce(_bulletForce);
            }
            _onShotFired.Raise(gameObject);
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}
