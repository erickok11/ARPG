using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary skills screen. Press K to open/close.
///   Top:  your Q / W / E / R slots (click one to empty it).
///   List: every gem you own, with its requirements. Click Q, W, E or R to put it on that slot.
/// </summary>
[RequireComponent(typeof(SkillBar), typeof(SkillUser))]
public class SkillsWindow : MonoBehaviour, IGameWindow
{
    [SerializeField] private bool startOpen = false;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private float width = 760f;
    [SerializeField] private float height = 540f;
    [Tooltip("Shows a button that gives one of every gem for testing.")]
    [SerializeField] private bool showDebugButton = true;

    private SkillBar bar;
    private SkillUser user;
    private CharacterStats stats;

    private bool isOpen;
    private Vector2 scroll;
    private Action pending;
    private GUIStyle labelStyle;
    private GUIStyle rowLabelStyle;
    private GUIStyle buttonStyle;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        bar = GetComponent<SkillBar>();
        user = GetComponent<SkillUser>();
        stats = GetComponent<CharacterStats>();
        isOpen = startOpen;
    }

    private void OnEnable() => GameWindows.Register(this);

    private void OnDisable()
    {
        GameWindows.Unregister(this);
        ClickBlocker.Clear(this);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (!open) ClickBlocker.Clear(this);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.kKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
            if (isOpen) GameWindows.CloseOthers(this);
        }
        else if (isOpen && kb.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        GUI.depth = -10;
        EnsureStyles();

        float w = Mathf.Min(width, Screen.width - 20f);
        float h = Mathf.Min(height, Screen.height - 20f);
        var window = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        ClickBlocker.Set(this, window);
        Fill(window, new Color(0.03f, 0.03f, 0.05f, 0.95f));

        pending = null;
        GUILayout.BeginArea(new Rect(window.x + 12f, window.y + 10f, window.width - 24f, window.height - 20f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>Skills</b>   <color=#888>K to close</color>", rowLabelStyle);
        GUILayout.FlexibleSpace();
        if (showDebugButton && GUILayout.Button("<color=#aaa>Debug: one of every gem</color>", buttonStyle, GUILayout.Width(210f)))
            pending = DebugGiveAllGems;
        GUILayout.EndHorizontal();

        GUILayout.Label(
            $"<color=#888>Your attributes:</color>  <color=#f76>Str {stats.GetValue(StatType.Strength):0}</color>   " +
            $"<color=#7e7>Dex {stats.GetValue(StatType.Dexterity):0}</color>   <color=#79f>Int {stats.GetValue(StatType.Intelligence):0}</color>",
            rowLabelStyle);
        GUILayout.Space(6f);

        // Skill bar slots
        GUILayout.Label("<b>Skill bar</b>  <color=#888>click a slot to empty it</color>", rowLabelStyle);
        GUILayout.BeginHorizontal();
        for (int slot = 0; slot < SkillBar.SlotCount; slot++)
        {
            SkillGem gem = bar.Get(slot);
            string text = gem == null
                ? $"<b>{SkillBar.SlotKeys[slot]}</b>  <color=#555>empty</color>"
                : $"<b>{SkillBar.SlotKeys[slot]}</b>  <color={SkillDatabase.GemHex(gem.Data)}>{gem.Name}</color>";
            int s = slot;
            if (GUILayout.Button(text, buttonStyle, GUILayout.Height(32f)) && gem != null)
                pending = () => bar.Unassign(s);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        // Owned gems
        GUILayout.Label($"<b>Your gems</b>  <color=#888>{bar.Gems.Count} owned · click Q, W, E or R to put a gem on that slot</color>", rowLabelStyle);
        scroll = GUILayout.BeginScrollView(scroll);

        if (bar.Gems.Count == 0)
            GUILayout.Label("<color=#666>No gems yet. Monsters sometimes drop them.</color>", labelStyle);

        for (int i = 0; i < bar.Gems.Count; i++)
            DrawGem(bar.Gems[i]);

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        Action action = pending;
        pending = null;
        action?.Invoke();
    }

    private void DrawGem(SkillGem gem)
    {
        SkillData skill = gem.Data;
        if (skill == null) return;

        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b><color={SkillDatabase.GemHex(skill)}>{skill.name}</color></b>  <color=#777>Level {gem.level}</color>", rowLabelStyle);
        GUILayout.FlexibleSpace();

        int current = bar.SlotOf(gem);
        for (int slot = 0; slot < SkillBar.SlotCount; slot++)
        {
            Color previous = GUI.backgroundColor;
            if (slot == current) GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            int s = slot;
            if (GUILayout.Button(SkillBar.SlotKeys[slot], buttonStyle, GUILayout.Width(34f)))
                pending = () => bar.Assign(gem, s);
            GUI.backgroundColor = previous;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label($"<color=#888>{SkillDatabase.TagText(skill)}  ·  {skill.manaCost:0} mana  ·  {UseText(skill)}</color>", labelStyle);
        GUILayout.Label(RequirementText(skill), rowLabelStyle);
        GUILayout.Label($"<color=#aaa>{skill.description}</color>", labelStyle);

        GUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    private string UseText(SkillData skill)
    {
        string damage = skill.IsAttack
            ? $"{skill.damageMultiplier * 100f:0}% attack damage"
            : $"{skill.minDamage:0}-{skill.maxDamage:0} {skill.parsedDamageType} damage";
        string count = skill.projectileCount > 1 ? $" × {skill.projectileCount} projectiles" : "";
        return $"{damage}{count}  ·  {user.UseTime(skill):0.00}s per use";
    }

    private string RequirementText(SkillData skill)
    {
        string text = "<color=#888>Requires</color> ";
        bool any = false;
        AppendRequirement(ref text, ref any, "Str", StatType.Strength, skill.reqStr);
        AppendRequirement(ref text, ref any, "Dex", StatType.Dexterity, skill.reqDex);
        AppendRequirement(ref text, ref any, "Int", StatType.Intelligence, skill.reqInt);
        return any ? text : "<color=#888>No requirements</color>";
    }

    private void AppendRequirement(ref string text, ref bool any, string label, StatType stat, int required)
    {
        if (required <= 0) return;
        bool met = stats.GetValue(stat) + 0.001f >= required;
        text += $"{(any ? ",  " : "")}<color={(met ? "#7e7" : "#f66")}>{required} {label}</color>";
        any = true;
    }

    private void DebugGiveAllGems()
    {
        foreach (SkillData skill in SkillDatabase.All)
            bar.AddGem(new SkillGem(skill.id));
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null && labelStyle.fontSize == fontSize) return;

        labelStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = fontSize };
        labelStyle.normal.textColor = Color.white;
        rowLabelStyle = new GUIStyle(labelStyle) { wordWrap = false };
        buttonStyle = new GUIStyle(GUI.skin.button) { richText = true, fontSize = fontSize };
    }
}
