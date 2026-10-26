using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Level and experience, shared by skill and support gems.</summary>
[Serializable]
public abstract class Gem
{
    public int level = 1;
    public int experience;   // towards the next level

    public int ExperienceToNext => Leveling.GemExperienceToNext(level);
    public bool IsMaxLevel => level >= Leveling.MaxGemLevel;
    public float LevelProgress => IsMaxLevel ? 1f : (float)experience / ExperienceToNext;

    /// <summary>Adds experience. Returns true if the gem gained a level.</summary>
    public bool AddExperience(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return false;

        int startLevel = level;
        experience += amount;
        while (!IsMaxLevel && experience >= ExperienceToNext)
        {
            experience -= ExperienceToNext;
            level++;
        }
        if (IsMaxLevel) experience = 0;
        return level > startLevel;
    }
}

/// <summary>
/// A skill gem the player owns, plus the support gems socketed into it.
/// Higher levels deal more damage and cost a little more mana (see Leveling).
/// </summary>
[Serializable]
public class SkillGem : Gem
{
    /// <summary>How many support gems fit in one skill gem.</summary>
    public const int SupportSlots = 3;

    /// <summary>Spread used when a support adds projectiles to a skill that fires only one (like Fireball).</summary>
    private const float DefaultSpread = 10f;

    public string skillId;
    public List<SupportGem> supports = new List<SupportGem>();

    [NonSerialized] private SkillData effective;

    public SkillGem(string skillId)
    {
        this.skillId = skillId;
    }

    /// <summary>The base skill from skills.json, without level or supports.</summary>
    public SkillData Data => SkillDatabase.Get(skillId);
    public string Name => Data != null ? Data.name : skillId;

    /// <summary>The skill with its level and socketed supports applied. This is what actually gets used.</summary>
    public SkillData Effective
    {
        get
        {
            if (effective == null && Data != null) effective = Build(Data);
            return effective;
        }
    }

    /// <summary>Call after changing the level or the socketed supports.</summary>
    public void Refresh() => effective = null;

    private SkillData Build(SkillData baseSkill)
    {
        SkillData s = baseSkill.Clone();
        s.bonusDamageMultiplier = Leveling.SkillGemDamage(level);
        s.manaCost *= Leveling.SkillGemManaCost(level);
        s.extraFireFraction = 0f;
        s.pierceCount = 0;

        var applied = new List<SupportData>();
        foreach (SupportGem gem in supports)
        {
            SupportData support = gem.Data;
            if (support == null || !support.CanSupport(baseSkill)) continue;
            support.ApplyTo(s);
            s.bonusDamageMultiplier *= Leveling.SupportGemDamage(gem.level);
            applied.Add(support);
        }

        // Without a spread, extra projectiles would fly on top of each other.
        if (s.projectileCount > 1 && s.spreadAngle <= 0f) s.spreadAngle = DefaultSpread;
        s.manaCost = Mathf.Round(s.manaCost);
        s.supports = applied.ToArray();
        return s;
    }
}

/// <summary>
/// A support gem the player owns. Socket it into a skill gem to change how that skill works.
/// Each level adds a little damage to the skill (see Leveling).
/// </summary>
[Serializable]
public class SupportGem : Gem
{
    public string supportId;

    public SupportGem(string supportId)
    {
        this.supportId = supportId;
    }

    public SupportData Data => SupportDatabase.Get(supportId);
    public string Name => Data != null ? Data.name : supportId;
}

/// <summary>
/// The gems the player owns, which skill gems sit on the Q / W / E / R slots,
/// and which support gems are socketed into which skill gems.
/// </summary>
public class SkillBar : MonoBehaviour
{
    public const int SlotCount = 4;
    public static readonly string[] SlotKeys = { "Q", "W", "E", "R" };

    private readonly List<SkillGem> gems = new List<SkillGem>();
    private readonly SkillGem[] slots = new SkillGem[SlotCount];
    private readonly List<SupportGem> supports = new List<SupportGem>();

    /// <summary>Fires when gems are added or slots change.</summary>
    public event Action Changed;
    /// <summary>Fires when a gem levels up: (gem name, new level).</summary>
    public event Action<string, int> GemLeveledUp;

    public IReadOnlyList<SkillGem> Gems => gems;
    /// <summary>Every support gem owned, socketed or not.</summary>
    public IReadOnlyList<SupportGem> Supports => supports;

    public SkillGem Get(int slot) => slot >= 0 && slot < SlotCount ? slots[slot] : null;

    public int SlotOf(SkillGem gem)
    {
        if (gem == null) return -1;
        for (int i = 0; i < SlotCount; i++)
            if (slots[i] == gem) return i;
        return -1;
    }

    /// <summary>Adds a gem you picked up. If a slot is empty, the gem goes straight onto it.</summary>
    public bool AddGem(SkillGem gem, bool autoAssign = true)
    {
        if (gem?.Data == null)
        {
            Debug.LogWarning($"SkillBar: unknown skill '{gem?.skillId}'.");
            return false;
        }

        gems.Add(gem);
        if (autoAssign)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null) continue;
                slots[i] = gem;
                break;
            }
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>Puts a gem on a slot. If it was already on another slot, the two slots swap.</summary>
    public void Assign(SkillGem gem, int slot)
    {
        if (gem == null || !gems.Contains(gem) || slot < 0 || slot >= SlotCount) return;

        int current = SlotOf(gem);
        SkillGem displaced = slots[slot];
        slots[slot] = gem;
        if (current >= 0 && current != slot) slots[current] = displaced;
        Changed?.Invoke();
    }

    public void Unassign(int slot)
    {
        if (slot < 0 || slot >= SlotCount || slots[slot] == null) return;
        slots[slot] = null;
        Changed?.Invoke();
    }

    /// <summary>
    /// Gives experience to every skill gem on the Q / W / E / R slots and the supports socketed in them.
    /// Gems that aren't on the bar don't gain experience.
    /// </summary>
    public void GrantExperience(int amount)
    {
        bool anyLeveled = false;
        foreach (SkillGem gem in slots)
        {
            if (gem == null) continue;

            bool leveled = false;
            if (gem.AddExperience(amount))
            {
                leveled = true;
                GemLeveledUp?.Invoke(gem.Name, gem.level);
            }
            foreach (SupportGem support in gem.supports)
            {
                if (!support.AddExperience(amount)) continue;
                leveled = true;
                GemLeveledUp?.Invoke(support.Name, support.level);
            }

            if (!leveled) continue;
            gem.Refresh();
            anyLeveled = true;
        }
        if (anyLeveled) Changed?.Invoke();
    }

    // -----------------------------------------------------------------
    // Support gems
    // -----------------------------------------------------------------

    /// <summary>Adds a support gem you picked up. It stays loose until you socket it.</summary>
    public bool AddSupport(SupportGem support)
    {
        if (support?.Data == null)
        {
            Debug.LogWarning($"SkillBar: unknown support '{support?.supportId}'.");
            return false;
        }

        supports.Add(support);
        Changed?.Invoke();
        return true;
    }

    /// <summary>The skill gem a support is socketed into, or null if it's loose.</summary>
    public SkillGem HostOf(SupportGem support)
    {
        foreach (SkillGem gem in gems)
            if (gem.supports.Contains(support)) return gem;
        return null;
    }

    public bool CanSocket(SkillGem gem, SupportGem support, out string reason)
    {
        reason = null;
        if (gem?.Data == null || support?.Data == null || !gems.Contains(gem) || !supports.Contains(support))
        {
            reason = "Unknown gem";
            return false;
        }
        if (gem.supports.Contains(support))
        {
            reason = "Already socketed here";
            return false;
        }
        if (gem.supports.Count >= SkillGem.SupportSlots)
        {
            reason = "No empty support slot";
            return false;
        }
        if (!support.Data.CanSupport(gem.Data))
        {
            reason = $"Only supports {SkillDatabase.TagText(support.Data.requiresTags)} skills";
            return false;
        }
        foreach (SupportGem other in gem.supports)
        {
            if (other.supportId != support.supportId) continue;
            reason = $"Already has {support.Name}";
            return false;
        }
        return true;
    }

    /// <summary>Sockets a support into a skill gem, taking it out of any other skill gem first.</summary>
    public bool Socket(SkillGem gem, SupportGem support)
    {
        if (!CanSocket(gem, support, out _)) return false;

        RemoveFromHost(support);
        gem.supports.Add(support);
        gem.Refresh();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Takes a support out of its skill gem. It stays owned, as a loose support.</summary>
    public void Unsocket(SupportGem support)
    {
        if (RemoveFromHost(support)) Changed?.Invoke();
    }

    private bool RemoveFromHost(SupportGem support)
    {
        SkillGem host = HostOf(support);
        if (host == null) return false;
        host.supports.Remove(support);
        host.Refresh();
        return true;
    }
}
