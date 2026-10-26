using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipSlot { Weapon, Offhand, Helmet, BodyArmor, Gloves, Boots, Belt, Amulet, Ring1, Ring2 }

/// <summary>
/// The player's equipped items. Equipping an item adds all its modifiers to CharacterStats,
/// using the item itself as the modifier source so unequipping removes exactly those.
/// </summary>
[RequireComponent(typeof(CharacterStats), typeof(Inventory))]
public class Equipment : MonoBehaviour
{
    private readonly Dictionary<EquipSlot, Item> equipped = new Dictionary<EquipSlot, Item>();
    private CharacterStats stats;
    private Inventory inventory;

    public event Action Changed;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        inventory = GetComponent<Inventory>();
    }

    public Item Get(EquipSlot slot) => equipped.TryGetValue(slot, out Item item) ? item : null;

    /// <summary>Equip an item from the backpack. Whatever was in that slot goes back to the backpack.</summary>
    public bool EquipFromInventory(Item item)
    {
        if (item?.Base == null || !inventory.Contains(item)) return false;

        EquipSlot slot = ChooseSlot(item.Slot);
        inventory.Remove(item);

        Item previous = Get(slot);
        if (previous != null)
        {
            stats.RemoveAllFromSource(previous);
            inventory.Add(previous); // there's room: we just removed the new item
        }

        equipped[slot] = item;
        ApplyModifiers(item);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Move an equipped item back to the backpack.</summary>
    public bool Unequip(EquipSlot slot)
    {
        Item item = Get(slot);
        if (item == null) return false;

        if (inventory.IsFull)
        {
            Debug.Log("Inventory is full.");
            return false;
        }

        stats.RemoveAllFromSource(item);
        equipped.Remove(slot);
        inventory.Add(item);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Re-apply an equipped item's modifiers after it changes (e.g. crafting it while worn).</summary>
    public void RefreshItem(Item item)
    {
        if (item == null || !equipped.ContainsValue(item)) return;
        stats.RemoveAllFromSource(item);
        ApplyModifiers(item);
        Changed?.Invoke();
    }

    private void ApplyModifiers(Item item)
    {
        foreach ((StatType stat, ModifierType type, float value) in item.GetModifiers())
            stats.AddModifier(stat, new StatModifier(value, type, item));
    }

    private EquipSlot ChooseSlot(ItemSlot itemSlot)
    {
        switch (itemSlot)
        {
            case ItemSlot.Weapon: return EquipSlot.Weapon;
            case ItemSlot.Offhand: return EquipSlot.Offhand;
            case ItemSlot.Helmet: return EquipSlot.Helmet;
            case ItemSlot.BodyArmor: return EquipSlot.BodyArmor;
            case ItemSlot.Gloves: return EquipSlot.Gloves;
            case ItemSlot.Boots: return EquipSlot.Boots;
            case ItemSlot.Belt: return EquipSlot.Belt;
            case ItemSlot.Amulet: return EquipSlot.Amulet;
            case ItemSlot.Ring:
                if (Get(EquipSlot.Ring1) == null) return EquipSlot.Ring1;
                if (Get(EquipSlot.Ring2) == null) return EquipSlot.Ring2;
                return EquipSlot.Ring1;
            default:
                return EquipSlot.Weapon;
        }
    }
}
