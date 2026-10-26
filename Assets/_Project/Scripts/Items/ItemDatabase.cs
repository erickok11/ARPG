using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads item bases and affixes from:
///   Assets/_Project/Resources/Data/item_bases.json
///   Assets/_Project/Resources/Data/affixes.json
/// Reloaded fresh every time you press Play, so you can edit the JSON and test immediately.
/// </summary>
public static class ItemDatabase
{
    private const string BasesPath = "Data/item_bases";
    private const string AffixesPath = "Data/affixes";

    private static Dictionary<string, ItemBaseData> bases;
    private static Dictionary<string, AffixData> affixes;
    private static List<ItemBaseData> baseList;
    private static List<AffixData> affixList;

    public static IReadOnlyList<ItemBaseData> Bases
    {
        get { EnsureLoaded(); return baseList; }
    }

    public static IReadOnlyList<AffixData> Affixes
    {
        get { EnsureLoaded(); return affixList; }
    }

    public static ItemBaseData GetBase(string id)
    {
        EnsureLoaded();
        return id != null && bases.TryGetValue(id, out ItemBaseData b) ? b : null;
    }

    public static AffixData GetAffix(string id)
    {
        EnsureLoaded();
        return id != null && affixes.TryGetValue(id, out AffixData a) ? a : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        bases = null;
        affixes = null;
    }

    private static void EnsureLoaded()
    {
        if (bases != null) return;

        bases = new Dictionary<string, ItemBaseData>();
        affixes = new Dictionary<string, AffixData>();
        baseList = new List<ItemBaseData>();
        affixList = new List<AffixData>();

        LoadBases();
        LoadAffixes();
        Debug.Log($"ItemDatabase loaded {baseList.Count} item bases and {affixList.Count} affixes.");
    }

    private static void LoadBases()
    {
        TextAsset json = Resources.Load<TextAsset>(BasesPath);
        if (json == null)
        {
            Debug.LogError($"ItemDatabase: couldn't find Resources/{BasesPath}.json");
            return;
        }

        ItemBaseFile file = JsonUtility.FromJson<ItemBaseFile>(json.text);
        if (file?.bases == null) return;

        foreach (ItemBaseData b in file.bases)
        {
            if (string.IsNullOrEmpty(b.id)) continue;
            b.tags ??= Array.Empty<string>();
            b.implicits ??= Array.Empty<ModifierData>();

            if (!Enum.TryParse(b.slot, out b.itemSlot))
            {
                Debug.LogError($"ItemDatabase: base '{b.id}' has unknown slot '{b.slot}'.");
                continue;
            }

            bool valid = true;
            foreach (ModifierData m in b.implicits)
                valid &= ParseModifier(m, b.id);
            if (!valid) continue;

            if (bases.ContainsKey(b.id))
            {
                Debug.LogError($"ItemDatabase: duplicate base id '{b.id}'.");
                continue;
            }
            bases[b.id] = b;
            baseList.Add(b);
        }
    }

    private static void LoadAffixes()
    {
        TextAsset json = Resources.Load<TextAsset>(AffixesPath);
        if (json == null)
        {
            Debug.LogError($"ItemDatabase: couldn't find Resources/{AffixesPath}.json");
            return;
        }

        AffixFile file = JsonUtility.FromJson<AffixFile>(json.text);
        if (file?.affixes == null) return;

        foreach (AffixData a in file.affixes)
        {
            if (string.IsNullOrEmpty(a.id)) continue;
            a.allowedTags ??= Array.Empty<string>();
            a.tiers ??= Array.Empty<AffixTierData>();

            if (!Enum.TryParse(a.group, out a.affixGroup) ||
                !Enum.TryParse(a.stat, out a.statType) ||
                !Enum.TryParse(a.type, out a.modType))
            {
                Debug.LogError($"ItemDatabase: affix '{a.id}' has a bad group/stat/type ({a.group} / {a.stat} / {a.type}).");
                continue;
            }
            if (a.tiers.Length == 0)
            {
                Debug.LogError($"ItemDatabase: affix '{a.id}' has no tiers.");
                continue;
            }
            if (affixes.ContainsKey(a.id))
            {
                Debug.LogError($"ItemDatabase: duplicate affix id '{a.id}'.");
                continue;
            }
            affixes[a.id] = a;
            affixList.Add(a);
        }
    }

    private static bool ParseModifier(ModifierData m, string owner)
    {
        if (Enum.TryParse(m.stat, out m.statType) && Enum.TryParse(m.type, out m.modType))
            return true;

        Debug.LogError($"ItemDatabase: '{owner}' has a bad implicit ({m.stat} / {m.type}).");
        return false;
    }
}
