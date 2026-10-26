using System;

// ---------------------------------------------------------------------------
// Plain data classes that mirror the JSON files in Resources/Data/.
// Enum-like fields are stored as strings in JSON (readable, and safe if enums
// are reordered) and parsed into the [NonSerialized] typed fields on load.
// ---------------------------------------------------------------------------

public enum ItemSlot { Weapon, Offhand, Helmet, BodyArmor, Gloves, Boots, Belt, Amulet, Ring }

public enum AffixGroup { Prefix, Suffix }

public enum ItemRarity { Normal, Magic, Rare }

[Serializable]
public class ModifierData
{
    public string stat;   // StatType name, e.g. "MaxLife"
    public string type;   // "Flat", "Increased" or "More"
    public float value;

    [NonSerialized] public StatType statType;
    [NonSerialized] public ModifierType modType;
}

[Serializable]
public class ItemBaseData
{
    public string id;
    public string name;
    public string slot;           // ItemSlot name
    public string[] tags;         // used to decide which affixes can roll
    public ModifierData[] implicits;

    [NonSerialized] public ItemSlot itemSlot;
}

[Serializable]
public class ItemBaseFile
{
    public ItemBaseData[] bases;
}

[Serializable]
public class AffixTierData
{
    public string name;   // shown in magic item names, e.g. "Healthy" or "of the Fox"
    public int minLevel;  // lowest item level this tier can roll on
    public int min;
    public int max;
    public int weight;    // higher = more common
}

[Serializable]
public class AffixData
{
    public string id;
    public string group;          // "Prefix" or "Suffix"
    public string stat;           // StatType name
    public string type;           // ModifierType name
    public string text;           // tooltip text, {0} = rolled value
    public string[] allowedTags;  // can roll on bases with ANY of these tags (empty = any base)
    public AffixTierData[] tiers; // keep these ordered from weakest to strongest

    [NonSerialized] public AffixGroup affixGroup;
    [NonSerialized] public StatType statType;
    [NonSerialized] public ModifierType modType;
}

[Serializable]
public class AffixFile
{
    public AffixData[] affixes;
}
