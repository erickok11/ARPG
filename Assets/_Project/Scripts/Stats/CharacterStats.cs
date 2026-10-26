using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds every stat for one character (player or monster).
/// Items, passive nodes and buffs all talk to this component (support gems change the skill instead).
/// </summary>
public class CharacterStats : MonoBehaviour
{
    [Serializable]
    public struct BaseStat
    {
        public StatType type;
        public float value;
    }

    [Tooltip("Starting values. Any stat not listed starts at 0.")]
    [SerializeField]
    private List<BaseStat> baseStats = new List<BaseStat>
    {
        new BaseStat { type = StatType.MaxLife, value = 100f },
        new BaseStat { type = StatType.MaxMana, value = 50f },
        new BaseStat { type = StatType.ManaRegen, value = 2f },
        new BaseStat { type = StatType.MovementSpeed, value = 6f },
        new BaseStat { type = StatType.PhysicalDamage, value = 10f },
        new BaseStat { type = StatType.AttackSpeed, value = 1.2f },
        new BaseStat { type = StatType.CritChance, value = 5f },
        new BaseStat { type = StatType.CritMultiplier, value = 150f },
    };

    /// <summary>Fires when any stat's final value may have changed.</summary>
    public event Action<StatType> StatChanged;

    private readonly Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public Stat Get(StatType type)
    {
        EnsureInitialized();
        return stats[type];
    }

    public float GetValue(StatType type) => Get(type).Value;

    public void AddModifier(StatType type, StatModifier modifier) => Get(type).AddModifier(modifier);

    public bool RemoveModifier(StatType type, StatModifier modifier) => Get(type).RemoveModifier(modifier);

    /// <summary>Removes everything a source granted, across all stats.</summary>
    public void RemoveAllFromSource(object source)
    {
        EnsureInitialized();
        foreach (Stat stat in stats.Values)
            stat.RemoveAllFromSource(source);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            var stat = new Stat(type);
            stat.Changed += s => StatChanged?.Invoke(s.Type);
            stats[type] = stat;
        }

        foreach (BaseStat entry in baseStats)
            stats[entry.type].BaseValue = entry.value;
    }

    [ContextMenu("Log All Stats")]
    private void LogAllStats()
    {
        EnsureInitialized();
        var lines = new System.Text.StringBuilder($"{name} stats:\n");
        foreach (Stat stat in stats.Values)
        {
            if (Mathf.Approximately(stat.Value, 0f) && stat.Modifiers.Count == 0) continue;
            lines.Append($"  {stat.Type}: {stat.Value:0.##} (base {stat.BaseValue:0.##}");
            foreach (StatModifier m in stat.Modifiers)
                lines.Append($", {m}");
            lines.Append(")\n");
        }
        Debug.Log(lines.ToString());
    }
}
