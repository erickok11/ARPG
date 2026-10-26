using UnityEngine;

public enum DamageType { Physical, Fire, Cold, Lightning, Chaos }

/// <summary>The outcome of one hit.</summary>
public struct DamageResult
{
    public float Amount;
    public bool IsCrit;
    public DamageType Type;
}

/// <summary>
/// Turns an attacker's stats into a damage number.
/// Armor, evasion and resistances will plug in here later.
/// </summary>
public static class DamageCalculator
{
    /// <summary>Each hit rolls between 80% and 120% of the attacker's damage.</summary>
    private const float MinRoll = 0.8f;
    private const float MaxRoll = 1.2f;

    /// <summary>
    /// Attack damage: your Physical Damage stat (base + weapon + gear) times the skill's multiplier.
    /// The basic attack uses a multiplier of 1.
    /// </summary>
    public static DamageResult RollAttack(CharacterStats attacker, float damageMultiplier = 1f)
    {
        float damage = attacker.GetValue(StatType.PhysicalDamage) * damageMultiplier * Random.Range(MinRoll, MaxRoll);
        return Finish(attacker, damage, DamageType.Physical);
    }

    /// <summary>
    /// Spell damage: the skill's own base damage, run through your bonuses for that damage type
    /// (e.g. "increased Fire Damage" boosts Fireball). damageMultiplier comes from supports.
    /// </summary>
    public static DamageResult RollSpell(CharacterStats attacker, DamageType type, float minDamage, float maxDamage,
                                         float damageMultiplier = 1f)
    {
        float baseRoll = Random.Range(minDamage, Mathf.Max(minDamage, maxDamage));
        float damage = attacker.Get(StatFor(type)).Evaluate(baseRoll) * damageMultiplier;
        return Finish(attacker, damage, type);
    }

    /// <summary>
    /// Extra damage of another type based on a hit that was already rolled
    /// (e.g. "30% of damage as extra Fire"). It crits when the original hit crits.
    /// </summary>
    public static DamageResult Extra(DamageResult from, DamageType type, float fraction)
    {
        return new DamageResult
        {
            Amount = Mathf.Max(1f, Mathf.Round(from.Amount * fraction)),
            IsCrit = from.IsCrit,
            Type = type,
        };
    }

    public static StatType StatFor(DamageType type) => type switch
    {
        DamageType.Fire => StatType.FireDamage,
        DamageType.Cold => StatType.ColdDamage,
        DamageType.Lightning => StatType.LightningDamage,
        DamageType.Chaos => StatType.ChaosDamage,
        _ => StatType.PhysicalDamage,
    };

    private static DamageResult Finish(CharacterStats attacker, float damage, DamageType type)
    {
        bool isCrit = Random.value * 100f < attacker.GetValue(StatType.CritChance);
        if (isCrit)
            damage *= attacker.GetValue(StatType.CritMultiplier) / 100f;

        return new DamageResult
        {
            Amount = Mathf.Max(1f, Mathf.Round(damage)),
            IsCrit = isCrit,
            Type = type,
        };
    }
}
