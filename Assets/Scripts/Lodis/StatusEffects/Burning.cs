using Lodis.Movement;
using Types;

/// <summary>
/// (Fire Abilities) - Damages the opponent over time.
/// </summary>
public class Burning : StatusEffect
{
    private Fixed32 _damagePerStack;
    private bool _appliedDamage;

    public Burning(KnockbackBehaviour owner) : base(StatusEffectType.Burning, owner)
    {
        _damagePerStack = 1; // Damage per stack can be adjusted as needed
    }

    protected override void OnTick(Fixed32 dt)
    {
        base.OnTick(dt);

        _owner.TakeDamageRaw(_damagePerStack * StackCount * dt);
    }
}
