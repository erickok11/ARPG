using System;
using UnityEngine;

/// <summary>
/// How a skill is delivered.
///   Strike:     hits one enemy in melee range (you walk up to it first).
///   Projectile: fires toward the cursor.
///   Slam:       hits every enemy in a circle in front of you.
/// </summary>
public enum SkillKind { Strike, Projectile, Slam }

/// <summary>One skill, loaded from Resources/Data/skills.json.</summary>
[Serializable]
public class SkillData
{
    public string id;
    public string name;
    public string description;
    public string kind;           // SkillKind name
    public string[] tags;         // e.g. attack, spell, melee, projectile, area, fire
    public string color;          // effect color, e.g. "#ff7a1f"

    // Attribute requirements
    public int reqStr;
    public int reqDex;
    public int reqInt;

    public float manaCost;

    // Attacks: damage and speed come from your attack stats (weapon, Physical Damage, Attack Speed)
    public float damageMultiplier;      // 1.7 = 170% of your attack damage
    public float attackSpeedMultiplier; // 0.85 = 85% of your attack speed

    // Spells: the skill has its own damage and cast time, scaled by your bonuses
    public string damageType;           // DamageType name
    public float minDamage;
    public float maxDamage;
    public float castTime;              // seconds per cast before cast speed bonuses

    // Shape
    public float range;           // Strike: reach. Projectile: travel distance. Slam: how far in front of you.
    public float radius;          // Projectile: hit size. Slam: area radius.
    public float projectileSpeed;
    public int projectileCount;
    public float spreadAngle;     // degrees between projectiles

    [NonSerialized] public SkillKind skillKind;
    [NonSerialized] public DamageType parsedDamageType;
    [NonSerialized] public Color parsedColor;

    // Set by socketed support gems (see SupportData.Apply), never read from the JSON.
    [NonSerialized] public float supportDamageMultiplier = 1f; // 0.7 = 30% less damage
    [NonSerialized] public float extraFireFraction;            // 0.3 = each hit also deals 30% of its damage as Fire
    [NonSerialized] public int pierceCount;                     // enemies a projectile passes through before stopping
    [NonSerialized] public SupportData[] supports = Array.Empty<SupportData>();

    /// <summary>A copy to apply supports to, so the shared skill from the database never changes.</summary>
    public SkillData Clone() => (SkillData)MemberwiseClone();

    public bool HasTag(string tag) => tags != null && Array.IndexOf(tags, tag) >= 0;
    public bool IsAttack => HasTag("attack");
    public bool IsSpell => HasTag("spell");
}

[Serializable]
public class SkillFile
{
    public SkillData[] skills;
}
