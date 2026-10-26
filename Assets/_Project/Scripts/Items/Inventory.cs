using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The player's backpack: a simple list of items with a capacity.</summary>
public class Inventory : MonoBehaviour
{
    [SerializeField] private int capacity = 40;

    private readonly List<Item> items = new List<Item>();

    public IReadOnlyList<Item> Items => items;
    public int Capacity => capacity;
    public bool IsFull => items.Count >= capacity;

    public event Action Changed;

    public bool Add(Item item)
    {
        if (item == null || IsFull) return false;
        items.Add(item);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(Item item)
    {
        if (!items.Remove(item)) return false;
        Changed?.Invoke();
        return true;
    }

    public bool Contains(Item item) => items.Contains(item);
}
