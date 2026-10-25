using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single stat: a base value plus any number of modifiers.
/// The final value is cached and only recalculated when something changes.
/// </summary>
public class Stat
{
    public StatType Type { get; }

    /// <summary>Fires whenever the base value or modifiers change.</summary>
    public event Action<Stat> Changed;

    private readonly List<StatModifier> modifiers = new List<StatModifier>();
    private float baseValue;
    private float cachedValue;
    private bool dirty = true;

    public Stat(StatType type, float baseValue = 0f)
    {
        Type = type;
        this.baseValue = baseValue;
    }

    public float BaseValue
    {
        get => baseValue;
        set
        {
            if (Mathf.Approximately(baseValue, value)) return;
            baseValue = value;
            MarkDirty();
        }
    }

    public IReadOnlyList<StatModifier> Modifiers => modifiers;

    public float Value
    {
        get
        {
            if (dirty)
            {
                cachedValue = Calculate();
                dirty = false;
            }
            return cachedValue;
        }
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        MarkDirty();
    }

    public bool RemoveModifier(StatModifier modifier)
    {
        if (!modifiers.Remove(modifier)) return false;
        MarkDirty();
        return true;
    }

    /// <summary>Removes every modifier granted by a source (e.g. when unequipping an item).</summary>
    public int RemoveAllFromSource(object source)
    {
        int removed = modifiers.RemoveAll(m => m.Source == source);
        if (removed > 0) MarkDirty();
        return removed;
    }

    private void MarkDirty()
    {
        dirty = true;
        Changed?.Invoke(this);
    }

    private float Calculate()
    {
        float flat = 0f;
        float increased = 0f;
        float more = 1f;

        foreach (StatModifier m in modifiers)
        {
            switch (m.Type)
            {
                case ModifierType.Flat:
                    flat += m.Value;
                    break;
                case ModifierType.Increased:
                    increased += m.Value;
                    break;
                case ModifierType.More:
                    more *= 1f + m.Value / 100f;
                    break;
            }
        }

        // Reduced can't push a stat below zero by itself.
        float increasedMultiplier = Mathf.Max(0f, 1f + increased / 100f);
        return (baseValue + flat) * increasedMultiplier * more;
    }
}
