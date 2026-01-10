using FixedPoints;
using Lodis;
using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

/// <summary>
/// (Electric Abilities) - The opponents barrier takes more damage when they are knocked into it.
/// </summary>
public class Amped : StatusEffect
{
    private RingBarrierBehaviour _ringBarrier;
    private GameObject _ampedHitParticleEffect;
    private Fixed32 _damagePerStack;
    private bool _appliedDamage;

    public Amped(KnockbackBehaviour owner) : base(StatusEffectType.Amped, owner)
    {
        _ringBarrier = BlackBoardBehaviour.Instance.GetRingBarrierForPlayer(owner.Entity);
        _ampedHitParticleEffect = Resources.Load<GameObject>("Effects/AmpedHitEffect");
        _damagePerStack = 10; // Damage per stack can be adjusted as needed
        _ringBarrier.AddOnTakeDamageAction(OnRingBarrierHit);
    }

    private void OnRingBarrierHit()
    {
        if (_appliedDamage || StackCount <= 0)
            return;

        _appliedDamage = true;
        _ringBarrier.TakeDamage(_owner.Entity.Data, _damagePerStack * StackCount, damageType: DamageType.KNOCKBACK);
        MonoBehaviour.Instantiate(_ampedHitParticleEffect, _owner.transform.position, CameraBehaviour.Instance.transform.rotation);
       

        FixedPointTimer.StartNewTimedAction(() => _appliedDamage = false, GridGame.FixedTimeStep);
    }
}
