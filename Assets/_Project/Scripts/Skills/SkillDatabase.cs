using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Loads skills from Assets/_Project/Resources/Data/skills.json.
/// Reloaded fresh every time you press Play.
/// </summary>
public static class SkillDatabase
{
    private const string SkillsPath = "Data/skills";

    private static Dictionary<string, SkillData> skills;
    private static List<SkillData> skillList;

    public static IReadOnlyList<SkillData> All
    {
        get { EnsureLoaded(); return skillList; }
    }

    public static SkillData Get(string id)
    {
        EnsureLoaded();
        return id != null && skills.TryGetValue(id, out SkillData s) ? s : null;
    }

    /// <summary>Gem color by main attribute: red = Strength, green = Dexterity, blue = Intelligence.</summary>
    public static Color GemColor(SkillData skill) =>
        skill == null ? Color.white : GemColor(skill.reqStr, skill.reqDex, skill.reqInt);

    /// <summary>Gem color for a set of attribute requirements (skill and support gems share these).</summary>
    public static Color GemColor(int reqStr, int reqDex, int reqInt)
    {
        if (reqStr >= reqDex && reqStr >= reqInt && reqStr > 0) return new Color(0.95f, 0.38f, 0.32f);
        if (reqDex >= reqInt && reqDex > 0) return new Color(0.42f, 0.88f, 0.42f);
        if (reqInt > 0) return new Color(0.42f, 0.62f, 1f);
        return new Color(0.85f, 0.85f, 0.85f);
    }

    public static string GemHex(SkillData skill) => "#" + ColorUtility.ToHtmlStringRGB(GemColor(skill));

    /// <summary>"attack, melee" -> "Attack, Melee"</summary>
    public static string TagText(SkillData skill) => TagText(skill?.tags);

    public static string TagText(string[] tags)
    {
        if (tags == null || tags.Length == 0) return "";
        TextInfo text = CultureInfo.InvariantCulture.TextInfo;
        var parts = new string[tags.Length];
        for (int i = 0; i < tags.Length; i++) parts[i] = text.ToTitleCase(tags[i]);
        return string.Join(", ", parts);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        skills = null;
        skillList = null;
    }

    private static void EnsureLoaded()
    {
        if (skills != null) return;

        skills = new Dictionary<string, SkillData>();
        skillList = new List<SkillData>();

        TextAsset json = Resources.Load<TextAsset>(SkillsPath);
        if (json == null)
        {
            Debug.LogError($"SkillDatabase: couldn't find Resources/{SkillsPath}.json");
            return;
        }

        SkillFile file = JsonUtility.FromJson<SkillFile>(json.text);
        if (file?.skills == null) return;

        foreach (SkillData s in file.skills)
        {
            if (string.IsNullOrEmpty(s.id)) continue;

            if (!Enum.TryParse(s.kind, out s.skillKind))
            {
                Debug.LogError($"SkillDatabase: skill '{s.id}' has unknown kind '{s.kind}'.");
                continue;
            }
            if (!Enum.TryParse(string.IsNullOrEmpty(s.damageType) ? "Physical" : s.damageType, out s.parsedDamageType))
            {
                Debug.LogError($"SkillDatabase: skill '{s.id}' has unknown damage type '{s.damageType}'.");
                continue;
            }
            if (skills.ContainsKey(s.id))
            {
                Debug.LogError($"SkillDatabase: duplicate skill id '{s.id}'.");
                continue;
            }

            // Safe defaults for anything left out of the JSON.
            s.tags ??= Array.Empty<string>();
            s.name = string.IsNullOrEmpty(s.name) ? s.id : s.name;
            if (s.damageMultiplier <= 0f) s.damageMultiplier = 1f;
            if (s.attackSpeedMultiplier <= 0f) s.attackSpeedMultiplier = 1f;
            if (s.castTime <= 0f) s.castTime = 0.8f;
            if (s.projectileCount < 1) s.projectileCount = 1;
            if (s.projectileSpeed <= 0f) s.projectileSpeed = 15f;
            if (s.range <= 0f) s.range = 2f;
            if (s.radius <= 0f) s.radius = s.skillKind == SkillKind.Slam ? 2f : 0.4f;
            s.bonusDamageMultiplier = 1f;
            s.supports = Array.Empty<SupportData>();
            s.parsedColor = ColorUtility.TryParseHtmlString(s.color, out Color c) ? c : Color.white;

            skills[s.id] = s;
            skillList.Add(s);
        }

        Debug.Log($"SkillDatabase loaded {skillList.Count} skills.");
    }
}
