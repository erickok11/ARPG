using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Temporary on-screen panel that shows life, gold and every stat that isn't zero.
/// Add it to the Player next to CharacterStats. Remove it once real UI exists.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class StatDebugOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [Tooltip("Raise this if the text looks tiny (common on Retina screens).")]
    [SerializeField] private int fontSize = 16;
    [Tooltip("Also list the base value and each modifier next to stats that have modifiers.")]
    [SerializeField] private bool showModifiers = true;

    private static readonly StatType[] AllStats = (StatType[])Enum.GetValues(typeof(StatType));

    private CharacterStats stats;
    private Health health;
    private Wallet wallet;
    private CurrencyPouch pouch;
    private Mana mana;
    private StartingClass startingClass;
    private GUIStyle labelStyle;
    private readonly StringBuilder text = new StringBuilder();

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
        wallet = GetComponent<Wallet>();
        pouch = GetComponent<CurrencyPouch>();
        mana = GetComponent<Mana>();
        startingClass = GetComponent<StartingClass>();
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (labelStyle == null || labelStyle.fontSize != fontSize)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true, wordWrap = false };
            labelStyle.normal.textColor = Color.white;
        }

        text.Clear();

        if (startingClass != null)
            text.AppendLine($"<b>{startingClass.Class}</b>");

        if (health != null)
        {
            string lifeColor = health.Fraction > 0.5f ? "#7f7" : health.Fraction > 0.25f ? "#fd5" : "#f66";
            text.AppendLine($"<b>Life  <color={lifeColor}>{health.Current:0} / {health.Max:0}</color></b>");
            if (health.IsDead)
                text.AppendLine("<b><color=#f66>YOU DIED</color></b>  <color=#aaa>(stop and press Play again)</color>");
        }

        if (mana != null)
            text.AppendLine($"<b>Mana  <color=#79f>{mana.Current:0} / {mana.Max:0}</color></b>");

        if (wallet != null)
            text.AppendLine($"<b>Gold  <color=#fc3>{wallet.Gold}</color></b>");

        if (pouch != null)
        {
            foreach (ShardType type in ShardInfo.All)
                text.Append($"<color={ShardInfo.Hex(type)}>{type} {pouch.Get(type)}</color>     ");
            text.AppendLine();
        }

        text.AppendLine();

        foreach (StatType type in AllStats)
        {
            Stat stat = stats.Get(type);
            if (Mathf.Approximately(stat.Value, 0f) && stat.Modifiers.Count == 0) continue;

            string percent = StatText.IsPercent(type) ? "%" : "";
            text.Append($"{StatText.Name(type)}: <b>{stat.Value:0.##}{percent}</b>");

            if (showModifiers && stat.Modifiers.Count > 0)
            {
                text.Append($"  <color=#8cf>(base {stat.BaseValue:0.##}");
                foreach (StatModifier m in stat.Modifiers)
                    text.Append($", {m}");
                text.Append(")</color>");
            }
            text.AppendLine();
        }

        text.AppendLine();
        text.AppendLine("<color=#aaa>Q W E R  skills     I  inventory     C  crafting     K  skill gems");
        text.AppendLine("1  +30% increased move speed     3  50% less move speed");
        text.AppendLine("2  +50 max life     H  take 20 damage     0  remove test modifiers</color>");

        var content = new GUIContent(text.ToString().TrimEnd());
        Vector2 size = labelStyle.CalcSize(content);

        const float margin = 10f;
        const float padding = 10f;
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(margin, margin, size.x + padding * 2f, size.y + padding * 2f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(new Rect(margin + padding, margin + padding, size.x, size.y), content, labelStyle);
    }
}
