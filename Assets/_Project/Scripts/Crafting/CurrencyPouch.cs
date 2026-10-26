using System;
using UnityEngine;

/// <summary>The three crafting shards. Placeholder names, easy to rename later.</summary>
public enum ShardType { Augment, Reforge, Reroll }

/// <summary>Display names, descriptions and colors for each shard.</summary>
public static class ShardInfo
{
    public static readonly ShardType[] All = (ShardType[])Enum.GetValues(typeof(ShardType));

    public static string Name(ShardType type) => type switch
    {
        ShardType.Augment => "Augment Shard",
        ShardType.Reforge => "Reforge Shard",
        ShardType.Reroll => "Reroll Shard",
        _ => type.ToString(),
    };

    public static string Description(ShardType type) => type switch
    {
        ShardType.Augment => "Adds a random affix to an item.",
        ShardType.Reforge => "Turns one affix you choose into a different random affix.",
        ShardType.Reroll => "Rerolls the values of every affix within its current tier.",
        _ => "",
    };

    public static Color GetColor(ShardType type) => type switch
    {
        ShardType.Augment => new Color(0.45f, 0.85f, 1f),   // cyan
        ShardType.Reforge => new Color(1f, 0.55f, 0.25f),   // orange
        ShardType.Reroll => new Color(0.78f, 0.55f, 1f),    // purple
        _ => Color.white,
    };

    public static string Hex(ShardType type) => "#" + ColorUtility.ToHtmlStringRGB(GetColor(type));
}

/// <summary>How many of each shard the player is carrying.</summary>
public class CurrencyPouch : MonoBehaviour
{
    [SerializeField] private int augmentShards;
    [SerializeField] private int reforgeShards;
    [SerializeField] private int rerollShards;

    /// <summary>Fires whenever any shard count changes.</summary>
    public event Action Changed;

    public int Get(ShardType type) => Slot(type);

    public void Add(ShardType type, int amount = 1)
    {
        if (amount <= 0) return;
        Slot(type) += amount;
        Changed?.Invoke();
    }

    /// <summary>Spends shards if there are enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpend(ShardType type, int amount = 1)
    {
        ref int count = ref Slot(type);
        if (amount < 0 || count < amount) return false;
        count -= amount;
        Changed?.Invoke();
        return true;
    }

    private ref int Slot(ShardType type)
    {
        switch (type)
        {
            case ShardType.Augment: return ref augmentShards;
            case ShardType.Reforge: return ref reforgeShards;
            default: return ref rerollShards;
        }
    }
}
