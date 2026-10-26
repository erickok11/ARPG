using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>One affix that has been rolled onto a specific item.</summary>
[Serializable]
public class RolledAffix
{
    public string affixId;
    public int tierIndex;
    public int value;

    public AffixData Data => ItemDatabase.GetAffix(affixId);

    public AffixTierData Tier
    {
        get
        {
            AffixData data = Data;
            if (data == null || tierIndex < 0 || tierIndex >= data.tiers.Length) return null;
            return data.tiers[tierIndex];
        }
    }

    public string Describe()
    {
        AffixData data = Data;
        return data == null ? $"Unknown affix '{affixId}'" : string.Format(data.text, value);
    }
}

/// <summary>
/// A specific item instance: which base it is, its item level, and its rolled affixes.
/// Rarity comes from affix count: 0 = Normal, 1-2 = Magic, 3-6 = Rare.
/// </summary>
[Serializable]
public class Item
{
    public const int MaxPrefixes = 3;
    public const int MaxSuffixes = 3;

    public string baseId;
    public int itemLevel;
    public string rareName;
    public List<RolledAffix> affixes = new List<RolledAffix>();

    public Item() { }

    public Item(string baseId, int itemLevel)
    {
        this.baseId = baseId;
        this.itemLevel = Mathf.Max(1, itemLevel);
    }

    public ItemBaseData Base => ItemDatabase.GetBase(baseId);
    public ItemSlot Slot => Base.itemSlot;

    public ItemRarity Rarity =>
        affixes.Count == 0 ? ItemRarity.Normal :
        affixes.Count <= 2 ? ItemRarity.Magic :
        ItemRarity.Rare;

    public int PrefixCount => CountGroup(AffixGroup.Prefix);
    public int SuffixCount => CountGroup(AffixGroup.Suffix);

    public bool HasOpenSlot(AffixGroup group) =>
        group == AffixGroup.Prefix ? PrefixCount < MaxPrefixes : SuffixCount < MaxSuffixes;

    public bool HasAffix(string affixId)
    {
        foreach (RolledAffix a in affixes)
            if (a.affixId == affixId) return true;
        return false;
    }

    public string DisplayName
    {
        get
        {
            ItemBaseData b = Base;
            string baseName = b != null ? b.name : baseId;

            switch (Rarity)
            {
                case ItemRarity.Magic:
                    string prefix = null, suffix = null;
                    foreach (RolledAffix a in affixes)
                    {
                        AffixData d = a.Data;
                        AffixTierData t = a.Tier;
                        if (d == null || t == null) continue;
                        if (d.affixGroup == AffixGroup.Prefix && prefix == null) prefix = t.name;
                        else if (d.affixGroup == AffixGroup.Suffix && suffix == null) suffix = t.name;
                    }
                    var sb = new StringBuilder();
                    if (!string.IsNullOrEmpty(prefix)) sb.Append(prefix).Append(' ');
                    sb.Append(baseName);
                    if (!string.IsNullOrEmpty(suffix)) sb.Append(' ').Append(suffix);
                    return sb.ToString();

                case ItemRarity.Rare:
                    return string.IsNullOrEmpty(rareName) ? baseName : rareName;

                default:
                    return baseName;
            }
        }
    }

    public string RarityHex => "#" + ColorUtility.ToHtmlStringRGB(RarityColor(Rarity));

    public static Color RarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Magic => new Color(0.53f, 0.65f, 1f),
        ItemRarity.Rare => new Color(1f, 0.88f, 0.4f),
        _ => new Color(0.88f, 0.88f, 0.88f),
    };

    /// <summary>Every stat bonus this item grants: base implicits first, then affixes.</summary>
    public IEnumerable<(StatType stat, ModifierType type, float value)> GetModifiers()
    {
        ItemBaseData b = Base;
        if (b != null)
            foreach (ModifierData m in b.implicits)
                yield return (m.statType, m.modType, m.value);

        foreach (RolledAffix a in affixes)
        {
            AffixData d = a.Data;
            if (d != null) yield return (d.statType, d.modType, a.value);
        }
    }

    public string BuildTooltip()
    {
        ItemBaseData b = Base;
        string hex = RarityHex;
        var sb = new StringBuilder();

        sb.Append($"<size=17><b><color={hex}>{DisplayName}</color></b></size>\n");
        if (Rarity == ItemRarity.Rare && b != null)
            sb.Append($"<color={hex}>{b.name}</color>\n");
        sb.Append($"<color=#888>{(b != null ? b.itemSlot.ToString() : "?")}     Item Level {itemLevel}</color>");

        if (b != null && b.implicits.Length > 0)
        {
            sb.Append("\n<color=#555>——————————</color>");
            foreach (ModifierData m in b.implicits)
                sb.Append($"\n<color=#ddd>{StatText.Format(m.statType, m.modType, m.value)}</color>");
        }

        if (affixes.Count > 0)
        {
            sb.Append("\n<color=#555>——————————</color>");
            AppendGroup(sb, AffixGroup.Prefix);
            AppendGroup(sb, AffixGroup.Suffix);
        }

        return sb.ToString();
    }

    private void AppendGroup(StringBuilder sb, AffixGroup group)
    {
        foreach (RolledAffix a in affixes)
        {
            AffixData d = a.Data;
            if (d == null || d.affixGroup != group) continue;
            sb.Append($"\n<color=#8af>{a.Describe()}</color>  <color=#555>tier {a.tierIndex + 1}/{d.tiers.Length}</color>");
        }
    }

    private int CountGroup(AffixGroup group)
    {
        int count = 0;
        foreach (RolledAffix a in affixes)
        {
            AffixData d = a.Data;
            if (d != null && d.affixGroup == group) count++;
        }
        return count;
    }

    private static readonly string[] RareFirstWords =
        { "Doom", "Storm", "Grim", "Ash", "Blood", "Dusk", "Iron", "Sorrow", "Gale", "Bone", "Rune", "Ember", "Hollow", "Wrath" };
    private static readonly string[] RareSecondWords =
        { "Bane", "Grip", "Song", "Ward", "Fang", "Crown", "Mark", "Veil", "Brand", "Coil", "Spiral", "Shell", "Heart", "Thirst" };

    public static string RandomRareName() =>
        $"{RareFirstWords[UnityEngine.Random.Range(0, RareFirstWords.Length)]} " +
        $"{RareSecondWords[UnityEngine.Random.Range(0, RareSecondWords.Length)]}";
}

/// <summary>Turns stats and modifiers into readable text, e.g. "+12% Fire Resistance".</summary>
public static class StatText
{
    private static readonly Dictionary<StatType, string> names = new Dictionary<StatType, string>();

    /// <summary>"PhysicalDamage" -> "Physical Damage"</summary>
    public static string Name(StatType stat)
    {
        if (!names.TryGetValue(stat, out string name))
        {
            name = Regex.Replace(stat.ToString(), "(?<!^)([A-Z])", " $1");
            names[stat] = name;
        }
        return name;
    }

    public static bool IsPercent(StatType stat) =>
        stat is StatType.FireResistance or StatType.ColdResistance or StatType.LightningResistance
            or StatType.ChaosResistance or StatType.CritChance or StatType.CritMultiplier;

    public static string Format(StatType stat, ModifierType type, float value)
    {
        string name = Name(stat);
        switch (type)
        {
            case ModifierType.Flat:
                string percent = IsPercent(stat) ? "%" : "";
                return $"{(value >= 0 ? "+" : "")}{value:0.##}{percent} {name}";
            case ModifierType.Increased:
                return value >= 0 ? $"{value:0.##}% increased {name}" : $"{-value:0.##}% reduced {name}";
            case ModifierType.More:
                return value >= 0 ? $"{value:0.##}% more {name}" : $"{-value:0.##}% less {name}";
            default:
                return $"{value} {name}";
        }
    }
}
