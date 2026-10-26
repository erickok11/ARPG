using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary skills screen. Press K to open/close.
///   Top:   your Q / W / E / R slots (click one to empty it).
///   Left:  every skill gem you own. Click Q, W, E or R to put it on that slot.
///          Click a socketed support to take it out.
///   Right: your loose support gems. Click Socket, then click an empty support slot on a skill.
/// </summary>
[RequireComponent(typeof(SkillBar), typeof(SkillUser))]
public class SkillsWindow : MonoBehaviour, IGameWindow
{
    [SerializeField] private bool startOpen = false;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private float width = 900f;
    [SerializeField] private float height = 580f;
    [Tooltip("Shows a button that gives one of every skill and support gem for testing.")]
    [SerializeField] private bool showDebugButton = true;

    private SkillBar bar;
    private SkillUser user;
    private CharacterStats stats;

    private bool isOpen;
    private Vector2 scroll;
    private Vector2 supportScroll;
    private float skillColumnWidth;
    private SupportGem selected;   // support picked with Socket, waiting for a slot
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
        if (!open)
        {
            selected = null;
            ClickBlocker.Clear(this);
        }
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
            if (selected != null) selected = null;
            else SetOpen(false);
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

        // Only loose supports can be selected.
        if (selected != null && bar.HostOf(selected) != null) selected = null;

        pending = null;
        float innerWidth = window.width - 24f;
        skillColumnWidth = Mathf.Floor(innerWidth * 0.6f);
        GUILayout.BeginArea(new Rect(window.x + 12f, window.y + 10f, innerWidth, window.height - 20f));

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

        GUILayout.BeginHorizontal();

        // Left: skill gems
        GUILayout.BeginVertical(GUILayout.Width(skillColumnWidth));
        GUILayout.Label($"<b>Skill gems</b>  <color=#888>{bar.Gems.Count} owned · Q W E R puts one on that slot</color>", rowLabelStyle);
        scroll = GUILayout.BeginScrollView(scroll);

        if (bar.Gems.Count == 0)
            GUILayout.Label("<color=#666>No gems yet. Monsters sometimes drop them.</color>", labelStyle);

        for (int i = 0; i < bar.Gems.Count; i++)
            DrawGem(bar.Gems[i]);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.Space(8f);

        // Right: loose support gems
        GUILayout.BeginVertical();
        int loose = 0;
        foreach (SupportGem support in bar.Supports)
            if (bar.HostOf(support) == null) loose++;

        GUILayout.Label($"<b>Support gems</b>  <color=#888>{loose} loose · {bar.Supports.Count - loose} socketed</color>", rowLabelStyle);
        GUILayout.Label(selected == null
            ? "<color=#888>Click Socket, then an empty support slot on a skill.</color>"
            : $"<color=#ffd34d>Socketing {selected.Name}</color><color=#888>: click a highlighted slot, or Esc to cancel.</color>",
            labelStyle);
        supportScroll = GUILayout.BeginScrollView(supportScroll);

        if (loose == 0)
            GUILayout.Label("<color=#666>No loose supports. Monsters sometimes drop them.</color>", labelStyle);

        foreach (SupportGem support in bar.Supports)
            if (bar.HostOf(support) == null) DrawSupport(support);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        Action action = pending;
        pending = null;
        action?.Invoke();
    }

    private void DrawGem(SkillGem gem)
    {
        SkillData skill = gem.Effective;
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
        GUILayout.Label(RequirementText(skill.reqStr, skill.reqDex, skill.reqInt), rowLabelStyle);
        GUILayout.Label($"<color=#aaa>{skill.description}</color>", labelStyle);
        DrawSupportSlots(gem);

        GUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    private void DrawSupportSlots(SkillGem gem)
    {
        string reason = null;
        bool canSocket = selected != null && bar.CanSocket(gem, selected, out reason);
        float slotWidth = Mathf.Floor((skillColumnWidth - 40f) / SkillGem.SupportSlots);

        GUILayout.BeginHorizontal();
        for (int i = 0; i < SkillGem.SupportSlots; i++)
        {
            if (i < gem.supports.Count)
            {
                SupportGem support = gem.supports[i];
                string text = $"<color={SupportDatabase.GemHex(support.Data)}>{support.Name}</color>  <color=#777>x</color>";
                if (GUILayout.Button(text, buttonStyle, GUILayout.Width(slotWidth)))
                    pending = () => bar.Unsocket(support);
            }
            else if (canSocket)
            {
                SupportGem support = selected;
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.83f, 0.3f);
                if (GUILayout.Button("<color=#ffd34d>Socket here</color>", buttonStyle, GUILayout.Width(slotWidth)))
                    pending = () => { if (bar.Socket(gem, support)) selected = null; };
                GUI.backgroundColor = previous;
            }
            else
            {
                GUILayout.Label("<color=#555>empty support slot</color>", rowLabelStyle, GUILayout.Width(slotWidth));
            }
        }
        GUILayout.EndHorizontal();

        if (selected != null && !canSocket && !string.IsNullOrEmpty(reason))
            GUILayout.Label($"<color=#f66>Can't socket {selected.Name}: {reason}</color>", labelStyle);
    }

    private void DrawSupport(SupportGem support)
    {
        SupportData data = support.Data;
        if (data == null) return;

        bool isSelected = support == selected;
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b><color={SupportDatabase.GemHex(data)}>{data.name}</color></b>", rowLabelStyle);
        GUILayout.FlexibleSpace();
        Color previous = GUI.backgroundColor;
        if (isSelected) GUI.backgroundColor = new Color(1f, 0.83f, 0.3f);
        if (GUILayout.Button(isSelected ? "Cancel" : "Socket", buttonStyle, GUILayout.Width(70f)))
            pending = () => selected = isSelected ? null : support;
        GUI.backgroundColor = previous;
        GUILayout.EndHorizontal();

        GUILayout.Label($"<color=#888>For {SkillDatabase.TagText(data.requiresTags)} skills  ·  {data.manaMultiplier * 100f:0}% mana cost</color>", labelStyle);
        GUILayout.Label(RequirementText(data.reqStr, data.reqDex, data.reqInt), rowLabelStyle);
        GUILayout.Label($"<color=#aaa>{data.description}</color>", labelStyle);

        GUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    private string UseText(SkillData skill)
    {
        float more = skill.supportDamageMultiplier;
        string damage = skill.IsAttack
            ? $"{skill.damageMultiplier * more * 100f:0}% attack damage"
            : $"{skill.minDamage * more:0}-{skill.maxDamage * more:0} {skill.parsedDamageType} damage";
        string count = skill.projectileCount > 1 ? $" × {skill.projectileCount} projectiles" : "";
        string extras = "";
        if (skill.extraFireFraction > 0f) extras += $"  ·  +{skill.extraFireFraction * 100f:0}% as Fire";
        if (skill.pierceCount > 0) extras += $"  ·  pierces {skill.pierceCount}";
        if (skill.skillKind == SkillKind.Slam) extras += $"  ·  {skill.radius:0.0}m radius";
        return $"{damage}{count}{extras}  ·  {user.UseTime(skill):0.00}s per use";
    }

    private string RequirementText(int reqStr, int reqDex, int reqInt)
    {
        string text = "<color=#888>Requires</color> ";
        bool any = false;
        AppendRequirement(ref text, ref any, "Str", StatType.Strength, reqStr);
        AppendRequirement(ref text, ref any, "Dex", StatType.Dexterity, reqDex);
        AppendRequirement(ref text, ref any, "Int", StatType.Intelligence, reqInt);
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
        foreach (SupportData support in SupportDatabase.All)
            bar.AddSupport(new SupportGem(support.id));
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
