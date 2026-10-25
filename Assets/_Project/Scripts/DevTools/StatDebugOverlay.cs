using System.Text;
using UnityEngine;

/// <summary>
/// Temporary on-screen panel that shows life and stats live in the Game view.
/// Add it to the Player next to CharacterStats. Remove it once real UI exists.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class StatDebugOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [Tooltip("Raise this if the text looks tiny (common on Retina screens).")]
    [SerializeField] private int fontSize = 18;
    [SerializeField]
    private StatType[] shownStats =
    {
        StatType.MaxLife,
        StatType.LifeRegen,
        StatType.MaxMana,
        StatType.MovementSpeed,
        StatType.PhysicalDamage,
        StatType.AttackSpeed,
        StatType.CritChance,
        StatType.CritMultiplier,
    };

    private CharacterStats stats;
    private Health health;
    private GUIStyle labelStyle;
    private readonly StringBuilder text = new StringBuilder();

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
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

        if (health != null)
        {
            string lifeColor = health.Fraction > 0.5f ? "#7f7" : health.Fraction > 0.25f ? "#fd5" : "#f66";
            text.AppendLine($"<b>Life  <color={lifeColor}>{health.Current:0} / {health.Max:0}</color></b>");
            text.AppendLine();
        }

        foreach (StatType type in shownStats)
        {
            Stat stat = stats.Get(type);
            text.Append($"{type}: <b>{stat.Value:0.##}</b>");

            if (stat.Modifiers.Count > 0)
            {
                text.Append($"  <color=#8cf>(base {stat.BaseValue:0.##}");
                foreach (StatModifier m in stat.Modifiers)
                    text.Append($", {m}");
                text.Append(")</color>");
            }
            text.AppendLine();
        }

        text.AppendLine();
        text.AppendLine("<color=#aaa>1  +30% increased move speed");
        text.AppendLine("3  50% less move speed");
        text.AppendLine("2  +50 max life");
        text.AppendLine("H  take 20 damage");
        text.Append("0  remove test modifiers</color>");

        var content = new GUIContent(text.ToString());
        Vector2 size = labelStyle.CalcSize(content);

        const float margin = 10f;
        const float padding = 10f;
        GUI.Box(new Rect(margin, margin, size.x + padding * 2f, size.y + padding * 2f), GUIContent.none);
        GUI.Label(new Rect(margin + padding, margin + padding, size.x, size.y), content, labelStyle);
    }
}
