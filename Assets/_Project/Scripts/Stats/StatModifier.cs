/// <summary>
/// The three PoE-style modifier types.
///   Flat:      +10 damage                  (added to the base)
///   Increased: 20 means "20% increased"    (all Increased values are summed, then applied once)
///   More:      25 means "25% more"         (each More multiplies separately; use -25 for "25% less")
///
/// final = (base + sum of Flat) * (1 + sum of Increased / 100) * (1 + More1 / 100) * (1 + More2 / 100) ...
/// </summary>
public enum ModifierType
{
    Flat,
    Increased,
    More,
}

/// <summary>
/// One bonus applied to one stat, e.g. "+30% increased movement speed from Boots of Haste".
/// Source is whatever granted it (an item, a passive node, a buff) so it can be removed later.
/// </summary>
public class StatModifier
{
    public float Value { get; }
    public ModifierType Type { get; }
    public object Source { get; }

    public StatModifier(float value, ModifierType type, object source)
    {
        Value = value;
        Type = type;
        Source = source;
    }

    public override string ToString()
    {
        return Type switch
        {
            ModifierType.Flat => $"{(Value >= 0 ? "+" : "")}{Value}",
            ModifierType.Increased => Value >= 0 ? $"{Value}% increased" : $"{-Value}% reduced",
            ModifierType.More => Value >= 0 ? $"{Value}% more" : $"{-Value}% less",
            _ => Value.ToString(),
        };
    }
}
