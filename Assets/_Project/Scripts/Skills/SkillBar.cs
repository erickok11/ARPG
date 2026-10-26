using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A skill gem the player owns, plus the support gems socketed into it.
/// Level is stored now and used once gem XP is added.
/// </summary>
[Serializable]
public class SkillGem
{
    /// <summary>How many support gems fit in one skill gem.</summary>
    public const int SupportSlots = 3;

    public string skillId;
    public int level = 1;
    public List<SupportGem> supports = new List<SupportGem>();

    [NonSerialized] private SkillData effective;

    public SkillGem(string skillId)
    {
        this.skillId = skillId;
        level = 1;
    }

    /// <summary>The base skill from skills.json, without supports.</summary>
    public SkillData Data => SkillDatabase.Get(skillId);
    public string Name => Data != null ? Data.name : skillId;

    /// <summary>The skill with its socketed supports applied. This is what actually gets used.</summary>
    public SkillData Effective
    {
        get
        {
            if (effective == null && Data != null)
            {
                var data = new List<SupportData>(supports.Count);
                foreach (SupportGem support in supports) data.Add(support.Data);
                effective = SupportData.Apply(Data, data);
            }
            return effective;
        }
    }

    /// <summary>Call after changing the socketed supports.</summary>
    public void Refresh() => effective = null;
}

/// <summary>A support gem the player owns. Socket it into a skill gem to change how that skill works.</summary>
[Serializable]
public class SupportGem
{
    public string supportId;
    public int level = 1;

    public SupportGem(string supportId)
    {
        this.supportId = supportId;
        level = 1;
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
