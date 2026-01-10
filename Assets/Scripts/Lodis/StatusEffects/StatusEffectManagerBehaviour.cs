using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles applying and removing status effects on a KnockbackBehaviour owner.
/// </summary>
public class StatusEffectManagerBehaviour : SimulationBehaviour
{
    [SerializeField] private KnockbackBehaviour _owner;

    //---
    private StatusEffect _currentHarmfulEffect;
    private StatusEffect _currentHelpfulEffect;

    public delegate void StatusEffectEvent(StatusEffect.StatusEffectType effect);
    private StatusEffectEvent _onAddedStatusEffect;
    private StatusEffectEvent _onRemovedStatusEffect;

    private int lastHelpfulEffectStacks = 0;
    private int lastHarmfulEffectStacks = 0;

    /// <summary>
    /// The current status effect that is affecting the owner negatively.
    /// </summary>
    public StatusEffect CurrentHarmfulEffect { get => _currentHarmfulEffect; private set => _currentHarmfulEffect = value; }

    /// <summary>
    /// The current status effect that is affecting the owner positively.
    /// </summary>
    public StatusEffect CurrentHelpfulEffect { get => _currentHelpfulEffect; private set => _currentHelpfulEffect = value; }

    public override void Deserialize(BinaryReader br)
    {
        _currentHarmfulEffect?.Deserialize(br);
        _currentHelpfulEffect?.Deserialize(br);
        lastHarmfulEffectStacks = br.ReadInt32();
        lastHelpfulEffectStacks = br.ReadInt32();
    }

    public override void Serialize(BinaryWriter bw)
    {
        _currentHarmfulEffect?.Serialize(bw);
        _currentHelpfulEffect?.Serialize(bw);
        bw.Write(lastHarmfulEffectStacks);
        bw.Write(lastHelpfulEffectStacks);
    }

    public override void Init()
    {
        base.Init();

        if (_owner != null)
            _owner.StatusEffectManager = this;

        if (MatchManagerBehaviour.Instance)
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ClearAllStatusEffects);
    }

    /// <summary>
    /// Adds the given amount of stacks of the given status effect type to the owner.
    /// </summary>
    /// <param name="effectType">The specific type of effect to apply.</param>
    /// <param name="stackCount">How many levels of the effect to apply. Effect are stronger and last longer the higher the level.</param>
    public void ApplyStatusEffect(StatusEffect.StatusEffectType effectType, int stackCount = 1)
    {
        bool isHarmfulEffect = effectType <= StatusEffect.StatusEffectType.Chilled;

        // Clear the harmful effect if a new one is being applied.
        if (isHarmfulEffect && _currentHarmfulEffect != null && _currentHarmfulEffect.EffectType != effectType)
        {
            _currentHarmfulEffect.Clear();
        }
        else if (!isHarmfulEffect && _currentHelpfulEffect != null && _currentHelpfulEffect.EffectType != effectType)
        {
            _currentHelpfulEffect.Clear();
        }


        switch (effectType)
        {
            case StatusEffect.StatusEffectType.Amped:
                if (_currentHarmfulEffect == null || _currentHarmfulEffect.EffectType != StatusEffect.StatusEffectType.Amped)
                {
                    _currentHarmfulEffect = new Amped(_owner);
                }
                break;
            case StatusEffect.StatusEffectType.Burning:
                if (_currentHarmfulEffect == null || _currentHarmfulEffect.EffectType != StatusEffect.StatusEffectType.Burning)
                {
                    _currentHarmfulEffect = new Burning(_owner);
                }
                break;

        }

        if (isHarmfulEffect)
        {
            for (int i = 0; i < stackCount; i++)
                _currentHarmfulEffect.AddStack();
        }
        else
        {
            for (int i = 0; i < stackCount; i++)
                _currentHelpfulEffect.AddStack();
        }

        _onAddedStatusEffect?.Invoke(effectType);
    }

    public void AddOnStatusEffectAddedListener(StatusEffectEvent listener)
    {
        _onAddedStatusEffect += listener;
    }

    public void AddOnStatusEffectRemovedListener(StatusEffectEvent listener)
    {
        _onRemovedStatusEffect += listener;
    }

    public void ClearAllStatusEffects()
    {
        _currentHarmfulEffect?.Clear();
        _currentHelpfulEffect?.Clear();
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        _currentHarmfulEffect?.Tick(dt);
        _currentHelpfulEffect?.Tick(dt);

        if (_currentHarmfulEffect != null && lastHarmfulEffectStacks != _currentHarmfulEffect.StackCount)
        {
            _onRemovedStatusEffect?.Invoke(_currentHarmfulEffect.EffectType);

            if (_currentHarmfulEffect.StackCount == 0)
            {
                _currentHarmfulEffect = null;
            }

            lastHarmfulEffectStacks = _currentHarmfulEffect != null ? _currentHarmfulEffect.StackCount : 0;
        }

        if (_currentHelpfulEffect != null && lastHelpfulEffectStacks != _currentHelpfulEffect.StackCount)
        {
            _onRemovedStatusEffect?.Invoke(_currentHelpfulEffect.EffectType);

            if (_currentHelpfulEffect.StackCount == 0)
            {
                _currentHelpfulEffect = null;
            }

            lastHelpfulEffectStacks = _currentHelpfulEffect != null ? _currentHelpfulEffect.StackCount : 0;
        }
    }
}
