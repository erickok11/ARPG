using UnityEngine;

/// <summary>
/// Experience curves and per-level gem bonuses. Change the numbers here to change the pacing.
/// </summary>
public static class Leveling
{
    public const int MaxCharacterLevel = 100;
    public const int MaxGemLevel = 20;

    /// <summary>Experience needed to go from this character level to the next (80, 243, 464, 735...).</summary>
    public static int ExperienceToNext(int level) => Mathf.RoundToInt(80f * Mathf.Pow(level, 1.6f));

    /// <summary>Gems need a bit more experience per level than characters.</summary>
    public static int GemExperienceToNext(int level) => Mathf.RoundToInt(100f * Mathf.Pow(level, 1.6f));

    /// <summary>Experience for killing a monster of this level (8, 20, 33, 49...).</summary>
    public static int MonsterExperience(int level) => Mathf.RoundToInt(8f * Mathf.Pow(level, 1.3f));

    /// <summary>Skill gems: 10% more damage per level after 1.</summary>
    public static float SkillGemDamage(int level) => 1f + 0.10f * (level - 1);

    /// <summary>Skill gems: 4% more mana cost per level after 1.</summary>
    public static float SkillGemManaCost(int level) => 1f + 0.04f * (level - 1);

    /// <summary>Support gems: 3% more damage per level after 1, on top of what the support does.</summary>
    public static float SupportGemDamage(int level) => 1f + 0.03f * (level - 1);
}
