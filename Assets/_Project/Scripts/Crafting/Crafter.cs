using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What happened when a shard was used.</summary>
public class CraftResult
{
    public bool Success;
    public string Message;
    /// <summary>The affixes that were added or changed (used to highlight them in the UI).</summary>
    public readonly List<RolledAffix> Changed = new List<RolledAffix>();
}

/// <summary>
/// The pure crafting rules. These only change the item; they don't check or spend currency.
/// </summary>
public static class ItemCrafting
{
    public static bool CanAugment(Item item) => ItemGenerator.HasPossibleAffix(item);

    /// <summary>Adds one random affix. Returns the new affix, or null if nothing could be added.</summary>
    public static RolledAffix Augment(Item item)
    {
        if (!ItemGenerator.AddRandomAffix(item)) return null;
        return item.affixes[item.affixes.Count - 1];
    }

    /// <summary>
    /// Replaces the affix at index with a different random affix, keeping its place in the list.
    /// Returns the new affix, or null (and leaves the item unchanged) if nothing else can roll.
    /// </summary>
    public static RolledAffix Reforge(Item item, int index)
    {
        if (item?.Base == null || index < 0 || index >= item.affixes.Count) return null;

        RolledAffix old = item.affixes[index];
        item.affixes.RemoveAt(index);

        if (!ItemGenerator.AddRandomAffix(item, old.affixId))
        {
            item.affixes.Insert(index, old);
            return null;
        }

        int last = item.affixes.Count - 1;
        RolledAffix added = item.affixes[last];
        item.affixes.RemoveAt(last);
        item.affixes.Insert(index, added);
        return added;
    }

    /// <summary>Rerolls every affix's value within its tier. Returns false if the item has no affixes.</summary>
    public static bool Reroll(Item item)
    {
        if (item == null || item.affixes.Count == 0) return false;
        foreach (RolledAffix a in item.affixes)
            ItemGenerator.RerollValue(a);
        return true;
    }
}

/// <summary>
/// Uses shards on items: checks the player can pay (1 shard + gold), applies the craft,
/// spends the currency only if it worked, and refreshes stats if the item is equipped.
/// </summary>
[RequireComponent(typeof(Wallet), typeof(CurrencyPouch), typeof(Equipment))]
public class Crafter : MonoBehaviour
{
    [Header("Gold cost per use = base + (per level × item level)")]
    [SerializeField] private int augmentBaseCost = 10;
    [SerializeField] private float augmentCostPerLevel = 1f;
    [SerializeField] private int reforgeBaseCost = 15;
    [SerializeField] private float reforgeCostPerLevel = 1.5f;
    [SerializeField] private int rerollBaseCost = 20;
    [SerializeField] private float rerollCostPerLevel = 2f;

    /// <summary>Fires after any successful craft.</summary>
    public event Action<Item> ItemCrafted;

    private Wallet wallet;
    private CurrencyPouch pouch;
    private Equipment equipment;

    private void Awake()
    {
        wallet = GetComponent<Wallet>();
        pouch = GetComponent<CurrencyPouch>();
        equipment = GetComponent<Equipment>();
    }

    public int GoldCost(ShardType type, Item item)
    {
        int level = item != null ? item.itemLevel : 1;
        (int baseCost, float perLevel) = type switch
        {
            ShardType.Augment => (augmentBaseCost, augmentCostPerLevel),
            ShardType.Reforge => (reforgeBaseCost, reforgeCostPerLevel),
            _ => (rerollBaseCost, rerollCostPerLevel),
        };
        return Mathf.Max(0, baseCost + Mathf.RoundToInt(perLevel * level));
    }

    public CraftResult Augment(Item item)
    {
        if (!CheckCanPay(ShardType.Augment, item, out int cost, out CraftResult failure)) return failure;

        if (!ItemCrafting.CanAugment(item))
        {
            return Fail(item.affixes.Count >= Item.MaxPrefixes + Item.MaxSuffixes
                ? "This item already has the maximum of 6 affixes."
                : "No more affixes can roll on this item.");
        }

        RolledAffix added = ItemCrafting.Augment(item);
        if (added == null) return Fail("No more affixes can roll on this item.");

        Pay(ShardType.Augment, cost);
        return Succeed(item, $"Augmented: added \"{added.Describe()}\"", new[] { added });
    }

    public CraftResult Reforge(Item item, int affixIndex)
    {
        if (!CheckCanPay(ShardType.Reforge, item, out int cost, out CraftResult failure)) return failure;
        if (affixIndex < 0 || affixIndex >= item.affixes.Count) return Fail("Pick an affix to reforge.");

        string oldText = item.affixes[affixIndex].Describe();
        RolledAffix added = ItemCrafting.Reforge(item, affixIndex);
        if (added == null) return Fail("No other affix can roll in its place.");

        Pay(ShardType.Reforge, cost);
        return Succeed(item, $"Reforged \"{oldText}\" into \"{added.Describe()}\"", new[] { added });
    }

    public CraftResult Reroll(Item item)
    {
        if (!CheckCanPay(ShardType.Reroll, item, out int cost, out CraftResult failure)) return failure;
        if (item.affixes.Count == 0) return Fail("This item has no affixes to reroll. Augment it first.");

        ItemCrafting.Reroll(item);

        Pay(ShardType.Reroll, cost);
        return Succeed(item, "Rerolled the values of every affix.", item.affixes);
    }

    private bool CheckCanPay(ShardType type, Item item, out int cost, out CraftResult failure)
    {
        cost = 0;
        failure = null;

        if (item?.Base == null)
        {
            failure = Fail("Select an item first.");
            return false;
        }
        if (pouch.Get(type) <= 0)
        {
            failure = Fail($"You don't have any {ShardInfo.Name(type)}s.");
            return false;
        }

        cost = GoldCost(type, item);
        if (wallet.Gold < cost)
        {
            failure = Fail($"Not enough gold: this costs {cost}, you have {wallet.Gold}.");
            return false;
        }
        return true;
    }

    private void Pay(ShardType type, int gold)
    {
        pouch.TrySpend(type, 1);
        wallet.TrySpend(gold);
    }

    private CraftResult Succeed(Item item, string message, IEnumerable<RolledAffix> changed)
    {
        equipment.RefreshItem(item); // does nothing if the item isn't equipped
        ItemCrafted?.Invoke(item);

        var result = new CraftResult { Success = true, Message = message };
        result.Changed.AddRange(changed);
        return result;
    }

    private static CraftResult Fail(string message) => new CraftResult { Success = false, Message = message };
}
