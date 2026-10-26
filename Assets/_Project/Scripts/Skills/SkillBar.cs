using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A skill gem the player owns. Level is stored now and used once gem XP is added.</summary>
[Serializable]
public class SkillGem
{
    public string skillId;
    public int level = 1;

    public SkillGem(string skillId)
    {
        this.skillId = skillId;
        level = 1;
    }

    public SkillData Data => SkillDatabase.Get(skillId);
    public string Name => Data != null ? Data.name : skillId;
}

/// <summary>
/// The gems the player owns, and which ones sit on the Q / W / E / R slots.
/// </summary>
public class SkillBar : MonoBehaviour
{
    public const int SlotCount = 4;
    public static readonly string[] SlotKeys = { "Q", "W", "E", "R" };

    private readonly List<SkillGem> gems = new List<SkillGem>();
    private readonly SkillGem[] slots = new SkillGem[SlotCount];

    /// <summary>Fires when gems are added or slots change.</summary>
    public event Action Changed;

    public IReadOnlyList<SkillGem> Gems => gems;

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
}
