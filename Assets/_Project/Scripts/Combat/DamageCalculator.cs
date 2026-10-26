using UnityEngine;

/// <summary>The outcome of one hit.</summary>
public struct DamageResult
{
    public float Amount;
    public bool IsCrit;
}

/// <summary>
/// Turns an attacker's stats into a damage number.
/// Armor, resistances and elemental damage will plug in here later.
/// </summary>
public static class DamageCalculator
{
    /// <summary>Each hit rolls between 80% and 120% of the attacker's damage.</summary>
    private const float MinRoll = 0.8f;
    private const float MaxRoll = 1.2f;

    public static DamageResult RollAttack(CharacterStats attacker)
    {
        float damage = attacker.GetValue(StatType.PhysicalDamage) * Random.Range(MinRoll, MaxRoll);

        bool isCrit = Random.value * 100f < attacker.GetValue(StatType.CritChance);
        if (isCrit)
            damage *= attacker.GetValue(StatType.CritMultiplier) / 100f;

        return new DamageResult
        {
            Amount = Mathf.Max(1f, Mathf.Round(damage)),
            IsCrit = isCrit,
        };
    }
}
