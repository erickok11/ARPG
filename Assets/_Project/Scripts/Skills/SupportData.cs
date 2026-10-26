using System;
using UnityEngine;

/// <summary>
/// One support gem, loaded from Resources/Data/supports.json.
/// Socketed into a skill gem, it changes how that skill works (more projectiles, bigger area...)
/// and makes it cost more mana. Effects a support doesn't use are simply left out of the JSON.
/// </summary>
[Serializable]
public class SupportData
{
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

    /// <summary>Applies this support's effects to a skill (a copy from SkillGem.Effective, never the shared one).</summary>
    public void ApplyTo(SkillData s)
    {
        s.manaCost *= manaMultiplier;
        s.bonusDamageMultiplier *= 1f + damageMore / 100f;
        s.projectileCount += addedProjectiles;
        s.pierceCount += pierceCount;
        s.radius *= 1f + radiusIncreased / 100f;
        s.extraFireFraction += extraFirePercent / 100f;
        s.attackSpeedMultiplier *= 1f + attackSpeedMore / 100f;
        s.castTime /= 1f + castSpeedMore / 100f;
    }
}

[Serializable]
public class SupportFile
{
    public SupportData[] supports;
}
