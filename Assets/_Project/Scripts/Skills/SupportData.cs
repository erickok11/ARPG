using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One support gem, loaded from Resources/Data/supports.json.
/// Socketed into a skill gem, it changes how that skill works (more projectiles, bigger area...)
/// and makes it cost more mana. Effects a support doesn't use are simply left out of the JSON.
/// </summary>
[Serializable]
public class SupportData
{
    /// <summary>Spread used when a support adds projectiles to a skill that fires only one (like Fireball).</summary>
    private const float DefaultSpread = 10f;

    public string id;
    public string name;
    public string description;
    public string[] requiresTags;   // the skill must have every one of these tags

    // Attribute requirements
    public int reqStr;
    public int reqDex;
    public int reqInt;

    public float manaMultiplier;    // 1.3 = the skill costs 30% more mana

    // Effects
    public float damageMore;        // 35 = 35% more damage, -30 = 30% less
    public int addedProjectiles;
    public int pierceCount;
    public float radiusIncreased;   // percent
    public float extraFirePercent;  // percent of each hit dealt again as Fire damage
    public float attackSpeedMore;   // percent
    public float castSpeedMore;     // percent

    /// <summary>True if this support works with the skill (the skill has every required tag).</summary>
    public bool CanSupport(SkillData skill)
    {
        if (skill == null) return false;
        foreach (string tag in requiresTags)
            if (!skill.HasTag(tag)) return false;
        return true;
    }

    /// <summary>
    /// Builds the skill as it is actually used: a copy of the base skill with every compatible support applied.
    /// </summary>
    public static SkillData Apply(SkillData skill, IReadOnlyList<SupportData> supports)
    {
        SkillData s = skill.Clone();
        s.supportDamageMultiplier = 1f;
        s.extraFireFraction = 0f;
        s.pierceCount = 0;

        var applied = new List<SupportData>();
        foreach (SupportData support in supports)
        {
            if (support == null || !support.CanSupport(skill)) continue;
            applied.Add(support);

            s.manaCost *= support.manaMultiplier;
            s.supportDamageMultiplier *= 1f + support.damageMore / 100f;
            s.projectileCount += support.addedProjectiles;
            s.pierceCount += support.pierceCount;
            s.radius *= 1f + support.radiusIncreased / 100f;
            s.extraFireFraction += support.extraFirePercent / 100f;
            s.attackSpeedMultiplier *= 1f + support.attackSpeedMore / 100f;
            s.castTime /= 1f + support.castSpeedMore / 100f;
        }

        // Without a spread, extra projectiles would fly on top of each other.
        if (s.projectileCount > 1 && s.spreadAngle <= 0f) s.spreadAngle = DefaultSpread;
        s.manaCost = Mathf.Round(s.manaCost);
        s.supports = applied.ToArray();
        return s;
    }
}

[Serializable]
public class SupportFile
{
    public SupportData[] supports;
}
