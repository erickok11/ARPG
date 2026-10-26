using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Creates random items and rolls affixes onto them.
/// AddRandomAffix is exactly what the Augment Shard will do.
/// </summary>
public static class ItemGenerator
{
    /// <summary>Chance a dropped item is Rare (3-6 affixes).</summary>
    public static float RareChance = 0.15f;
    /// <summary>Chance a dropped item is Magic (1-2 affixes). The rest are Normal.</summary>
    public static float MagicChance = 0.35f;

    private static readonly List<(AffixData affix, int weight)> candidates = new List<(AffixData, int)>();

    public static Item GenerateRandom(int itemLevel)
    {
        IReadOnlyList<ItemBaseData> bases = ItemDatabase.Bases;
        if (bases.Count == 0) return null;

        ItemBaseData chosenBase = bases[Random.Range(0, bases.Count)];
        var item = new Item(chosenBase.id, itemLevel);

        float roll = Random.value;
        int affixCount = 0;
        if (roll < RareChance) affixCount = Random.Range(3, 7);
        else if (roll < RareChance + MagicChance) affixCount = Random.Range(1, 3);

        for (int i = 0; i < affixCount; i++)
            if (!AddRandomAffix(item)) break;

        return item;
    }

    /// <summary>
    /// Adds one random affix the item is allowed to have. Returns false if nothing can be added
    /// (all prefix and suffix slots are full, or nothing in the pool fits this base and item level).
    /// </summary>
    public static bool AddRandomAffix(Item item)
    {
        if (item?.Base == null) return false;
        if (!TryPickAffix(item, out AffixData affix, out int tierIndex)) return false;

        item.affixes.Add(RollValue(affix, tierIndex));

        if (item.Rarity == ItemRarity.Rare && string.IsNullOrEmpty(item.rareName))
            item.rareName = Item.RandomRareName();

        return true;
    }

    private static bool TryPickAffix(Item item, out AffixData affix, out int tierIndex)
    {
        affix = null;
        tierIndex = -1;
        candidates.Clear();
        int totalWeight = 0;

        foreach (AffixData a in ItemDatabase.Affixes)
        {
            if (!CanRoll(item, a)) continue;
            int weight = EligibleWeight(a, item.itemLevel);
            if (weight <= 0) continue;
            candidates.Add((a, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0) return false;

        int pick = Random.Range(0, totalWeight);
        foreach ((AffixData a, int weight) in candidates)
        {
            if (pick < weight) { affix = a; break; }
            pick -= weight;
        }

        if (affix == null) return false;
        tierIndex = PickTier(affix, item.itemLevel);
        return tierIndex >= 0;
    }

    private static bool CanRoll(Item item, AffixData affix) =>
        item.HasOpenSlot(affix.affixGroup) && !item.HasAffix(affix.id) && MatchesTags(item.Base, affix);

    private static bool MatchesTags(ItemBaseData itemBase, AffixData affix)
    {
        if (affix.allowedTags.Length == 0) return true;
        foreach (string tag in affix.allowedTags)
            if (Array.IndexOf(itemBase.tags, tag) >= 0) return true;
        return false;
    }

    private static int EligibleWeight(AffixData affix, int itemLevel)
    {
        int total = 0;
        foreach (AffixTierData tier in affix.tiers)
            if (tier.minLevel <= itemLevel) total += Math.Max(0, tier.weight);
        return total;
    }

    private static int PickTier(AffixData affix, int itemLevel)
    {
        int total = EligibleWeight(affix, itemLevel);
        if (total <= 0) return -1;

        int pick = Random.Range(0, total);
        for (int i = 0; i < affix.tiers.Length; i++)
        {
            AffixTierData tier = affix.tiers[i];
            if (tier.minLevel > itemLevel) continue;
            int weight = Math.Max(0, tier.weight);
            if (pick < weight) return i;
            pick -= weight;
        }
        return -1;
    }

    private static RolledAffix RollValue(AffixData affix, int tierIndex)
    {
        AffixTierData tier = affix.tiers[tierIndex];
        return new RolledAffix
        {
            affixId = affix.id,
            tierIndex = tierIndex,
            value = Random.Range(tier.min, tier.max + 1),
        };
    }
}
