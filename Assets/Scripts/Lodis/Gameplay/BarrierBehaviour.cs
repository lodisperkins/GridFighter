using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;

namespace Lodis.Gameplay
{
    public class BarrierBehaviour : HealthBehaviour
    {
        [Tooltip("All layers that will be visible if placed behind this barrier")]
        [SerializeField]
        private LayerMask _visibleLayers;
        private float _rangeToIgnoreUpAngle;
        [Tooltip("The name of the gameobject that owns this barrier")]
        [SerializeField]
        private string _owner = "";

        public string Owner { get => _owner; set => _owner = value; }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            _material = _healthRenderer.material;
            Movement = GetComponent<Movement.GridMovementBehaviour>();
        }

        /// <summary>
        /// Inherited from health behaviour.
        /// Barriers only take damage from  owners if the type is knock back damage.
        /// </summary>
        /// <param name="attacker">The name of the object that is attacking</param>
        /// <param name="damage">The amount of damage this attack would do. Ignored if damage type isn't knock back</param>
        /// <param name="baseKnockBack">How far this object will be knocked back. Ignored for barriers</param>
        /// <param name="hitAngle">The angle to launch this object. Ignore for barriers</param>
        /// <param name="damageType">The type of damage being received</param>
        /// <returns>The amount of damage taken. Returns 0 if the attacker was the owner and if the type wasn't knock back </returns>
        public override float TakeDamage(string attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (attacker == Owner && damageType == DamageType.KNOCKBACK || attacker != Owner && damageType != DamageType.KNOCKBACK || Owner == "")
                return base.TakeDamage(attacker, damage, baseKnockBack, hitAngle, damageType);

            return 0;
        }

        public override float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            if (attacker.name == Owner && info.TypeOfDamage == DamageType.KNOCKBACK || attacker.name != Owner && info.TypeOfDamage != DamageType.KNOCKBACK || Owner == "")
                return base.TakeDamage(info, attacker);

            return 0;
        }

       

        // Update is called once per frame
        public void FixedUpdate()
        {
            //Make the material transparent if there is an object behind the barrier
            //if (Physics.Raycast(transform.position, Vector3.forward, BlackBoardBehaviour.Instance.Grid.PanelScale.z + 1, _visibleLayers))
            //    _material.color = new Color(1, 1, 1, 0.5f);
            //else
            //    _material.color = new Color(1, 1, 1, 1);

            _material.SetColor("_EmissionColor", _healthGradient.Evaluate(Health / MaxHealth.Value) * 1.2f);
        }
    }
}
