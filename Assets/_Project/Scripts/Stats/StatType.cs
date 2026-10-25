/// <summary>
/// Every stat a character can have.
///
/// IMPORTANT: Unity saves enum values as numbers, not names. Never renumber or
/// reorder existing entries, or saved prefabs/data will point at the wrong stat.
/// To add a stat, give it a new unused number in the matching group.
/// </summary>
public enum StatType
{
    // Attributes (0-99)
    Strength = 0,
    Dexterity = 1,
    Intelligence = 2,

    // Resources (100-199)
    MaxLife = 100,
    LifeRegen = 101,        // life per second
    MaxMana = 102,
    ManaRegen = 103,        // mana per second

    // Offense (200-299)
    PhysicalDamage = 200,
    FireDamage = 201,
    ColdDamage = 202,
    LightningDamage = 203,
    ChaosDamage = 204,
    AttackSpeed = 210,      // attacks per second
    CastSpeed = 211,        // casts per second
    CritChance = 220,       // percent
    CritMultiplier = 221,   // percent (150 = crits deal 150% damage)

    // Defense (300-399)
    Armor = 300,
    Evasion = 301,
    EnergyShield = 302,
    FireResistance = 310,   // percent
    ColdResistance = 311,
    LightningResistance = 312,
    ChaosResistance = 313,

    // Utility (400-499)
    MovementSpeed = 400,    // units per second
}
