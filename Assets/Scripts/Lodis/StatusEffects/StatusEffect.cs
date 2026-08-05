using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

/// <summary>
/// Some abilities can apply certain effects to the opponent or the user.
/// The effects themselves are divided into different types and are applied based on the element type of the ability used.
/// This is the base class for all effects that manages health and stacks.
/// </summary>
public class StatusEffect
{
    public enum StatusEffectType
    {
        Amped,
        Burning,
        Chilled,
        Hardened,
        Swift,
        Energized,
        Charged
    }

    private StatusEffectType _effectType;
    private int _stackCount;
    private const int MaxStacks = 5;
    private const int StackHealth = 20;
    // The total health of the status effect, used to determine when to remove stacks.
    private Fixed32 _health;

    protected KnockbackBehaviour _owner;
    protected KnockbackBehaviour _opponent;

    public StatusEffectType EffectType
    {
        get { return _effectType; }
    }

    public int StackCount
    {
        get { return _stackCount; }
    }

    public Fixed32 Health
    {
        get { return _health; }
    }

    public StatusEffect(StatusEffectType effectType, KnockbackBehaviour owner)
    {
        _effectType = effectType;
        _stackCount = 0;
        _owner = owner;

        //Set up faster decay for harmful effects
        _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(_owner.Entity).GetComponent<KnockbackBehaviour>();
        _opponent.AddOnTakeDamageAction(OnOpponentTakeDamage);

        //Set up faster decay for helpful effects
        _owner.AddOnTakeDamageAction(OnOwnerTakeDamage);
    }

    public void AddStack()
    {
        if (StackCount < MaxStacks)
        {
            _stackCount++;
            _health += StackHealth;
            OnApplyEffect();
        }
        else
        {
            // Refresh the health of the status effect if at max stacks.
            _health = StackCount * StackHealth;
        }
    }

    public void RemoveStack()
    {
        if (_stackCount > 0)
        {
            _stackCount--;
            OnRemoveEffect();
        }
    }

    public void Clear()
    {
        if (StackCount > 0)
            OnRemoveEffect();

        _stackCount = 0;
        _health = 0;
    }

    protected virtual void OnApplyEffect()
    {
        // Logic to apply the effect
    }

    protected virtual void OnRemoveEffect()
    {
    }

    /// <summary>
    /// Called whhen the opponent takes damage in their knockback behaviour. This speeds up the decay of harmful status effects.
    /// </summary>
    protected virtual void OnOpponentTakeDamage()
    {
        if (_effectType > StatusEffectType.Chilled)
            return;

        HitColliderData hitColliderData = _opponent.LastCollider.ColliderInfo;

        _health -= hitColliderData.Damage;
    }

    /// <summary>
    /// Called when the owner takes damage in their knockback behaviour. This speeds up the decay of helpful status effects.
    /// </summary>
    protected virtual void OnOwnerTakeDamage()
    {
        if (_effectType <= StatusEffectType.Chilled)
            return;

        HitColliderData hitColliderData = _owner.LastCollider.ColliderInfo;

        _health -= hitColliderData.Damage;
    }

    public void Tick(Fixed32 dt)
    {
        if (_stackCount <= 0)    
            return;

        //The status effect decays over time.
        _health -= dt;

        // Remove a stack if the overall status effect health is less than what it should be with this many stacks.
        if (_health < (StackCount - 1) * StackHealth)
        {
            RemoveStack();
        }

        if (_health < 0)
            _health = 0;

        //Debug.Log(dt + " Status Effect Tick: " + _effectType + " Health: " + _health + " Stacks: " + _stackCount);

        OnTick(dt);
    }

    protected virtual void OnTick(Fixed32 dt)
    {
    }

    public virtual void Deserialize(BinaryReader br)
    {
        _stackCount = br.ReadInt32();
        _health = _health.Deserialize(br);
    }

    public virtual void Serialize(BinaryWriter bw)
    {
        bw.Write(_stackCount);
        _health.Serialize(bw);
    }
}
