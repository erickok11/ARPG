using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads support gems from Assets/_Project/Resources/Data/supports.json.
/// Reloaded fresh every time you press Play.
/// </summary>
public static class SupportDatabase
{
    private const string SupportsPath = "Data/supports";

    private static Dictionary<string, SupportData> supports;
    private static List<SupportData> supportList;

    public static IReadOnlyList<SupportData> All
    {
        get { EnsureLoaded(); return supportList; }
    }

    public static SupportData Get(string id)
    {
        EnsureLoaded();
        return id != null && supports.TryGetValue(id, out SupportData s) ? s : null;
    }

    /// <summary>Same colors as skill gems: red = Strength, green = Dexterity, blue = Intelligence.</summary>
    public static Color GemColor(SupportData support) =>
        support == null ? Color.white : SkillDatabase.GemColor(support.reqStr, support.reqDex, support.reqInt);

    public static string GemHex(SupportData support) => "#" + ColorUtility.ToHtmlStringRGB(GemColor(support));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        supports = null;
        supportList = null;
    }

    private static void EnsureLoaded()
    {
        if (supports != null) return;

        supports = new Dictionary<string, SupportData>();
        supportList = new List<SupportData>();

        TextAsset json = Resources.Load<TextAsset>(SupportsPath);
        if (json == null)
        {
            Debug.LogError($"SupportDatabase: couldn't find Resources/{SupportsPath}.json");
            return;
        }

        SupportFile file = JsonUtility.FromJson<SupportFile>(json.text);
        if (file?.supports == null) return;

        foreach (SupportData s in file.supports)
        {
            if (string.IsNullOrEmpty(s.id)) continue;

            if (supports.ContainsKey(s.id))
            {
                Debug.LogError($"SupportDatabase: duplicate support id '{s.id}'.");
                continue;
            }

            // Safe defaults for anything left out of the JSON.
            s.requiresTags ??= Array.Empty<string>();
            s.name = string.IsNullOrEmpty(s.name) ? s.id : s.name;
            if (s.manaMultiplier <= 0f) s.manaMultiplier = 1f;

            supports[s.id] = s;
            supportList.Add(s);
        }

        Debug.Log($"SupportDatabase loaded {supportList.Count} supports.");
    }
}
