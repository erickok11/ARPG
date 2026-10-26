using UnityEngine;

/// <summary>
/// Temporary bottom-of-screen HUD: life bar, mana bar and the Q / W / E / R skill slots.
///   Red slot text  = you don't meet the attribute requirements.
///   Blue slot text = not enough mana.
///   Dark sweep     = still swinging or casting.
/// </summary>
[RequireComponent(typeof(SkillBar), typeof(SkillUser))]
public class SkillBarHUD : MonoBehaviour
{
    [SerializeField] private float slotSize = 86f;
    [SerializeField] private float slotGap = 8f;
    [SerializeField] private int fontSize = 13;
    [SerializeField] private float messageDuration = 1.6f;

    private SkillBar bar;
    private SkillUser user;
    private Health health;
    private Mana mana;

    private string message;
    private float messageUntil;
    private GUIStyle textStyle;
    private GUIStyle centerStyle;

    private void Awake()
    {
        bar = GetComponent<SkillBar>();
        user = GetComponent<SkillUser>();
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
    }

    private void OnEnable() => user.UseFailed += ShowMessage;

    private void OnDisable()
    {
        user.UseFailed -= ShowMessage;
        ClickBlocker.Clear(this);
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + messageDuration;
    }

    private void OnGUI()
    {
        EnsureStyles();

        float total = SkillBar.SlotCount * slotSize + (SkillBar.SlotCount - 1) * slotGap;
        float x0 = (Screen.width - total) / 2f;
        float slotY = Screen.height - slotSize - 14f;
        float manaY = slotY - 22f;
        float lifeY = manaY - 20f;

        // Clicking the HUD shouldn't make you walk there.
        ClickBlocker.Set(this, new Rect(x0 - 4f, lifeY - 4f, total + 8f, Screen.height - lifeY));

        if (Event.current.type != EventType.Repaint) return;

        if (health != null)
            DrawBar(new Rect(x0, lifeY, total, 16f), health.Fraction, new Color(0.8f, 0.15f, 0.15f),
                    $"Life {health.Current:0} / {health.Max:0}");
        if (mana != null)
            DrawBar(new Rect(x0, manaY, total, 16f), mana.Fraction, new Color(0.2f, 0.4f, 0.95f),
                    $"Mana {mana.Current:0} / {mana.Max:0}");

        for (int slot = 0; slot < SkillBar.SlotCount; slot++)
            DrawSlot(slot, new Rect(x0 + slot * (slotSize + slotGap), slotY, slotSize, slotSize));

        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
        {
            var rect = new Rect(x0 - 100f, lifeY - 30f, total + 200f, 24f);
            centerStyle.normal.textColor = new Color(1f, 0.55f, 0.5f);
            GUI.Label(rect, message, centerStyle);
        }
    }

    private void DrawSlot(int slot, Rect rect)
    {
        SkillGem gem = bar.Get(slot);
        SkillData skill = gem?.Effective;

        Fill(rect, new Color(0f, 0f, 0f, 0.8f));
        Color border = skill != null ? SkillDatabase.GemColor(skill) : new Color(0.3f, 0.3f, 0.3f);
        Outline(rect, border);

        textStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
        GUI.Label(new Rect(rect.x + 5f, rect.y + 3f, 20f, 18f), SkillBar.SlotKeys[slot], textStyle);
        if (gem != null && gem.supports.Count > 0)
        {
            textStyle.normal.textColor = new Color(1f, 0.83f, 0.3f);
            GUI.Label(new Rect(rect.xMax - 26f, rect.y + 3f, 24f, 18f), $"+{gem.supports.Count}", textStyle);
        }

        if (skill == null)
        {
            centerStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
            GUI.Label(rect, "empty", centerStyle);
            return;
        }

        bool meetsRequirements = user.MeetsRequirements(skill, out _);
        bool hasMana = user.HasManaFor(skill);

        Color nameColor = !meetsRequirements ? new Color(1f, 0.4f, 0.4f)
                        : !hasMana ? new Color(0.5f, 0.65f, 1f)
                        : Color.white;

        centerStyle.normal.textColor = nameColor;
        GUI.Label(new Rect(rect.x + 4f, rect.y + 16f, rect.width - 8f, rect.height - 36f), skill.name, centerStyle);

        textStyle.normal.textColor = new Color(0.5f, 0.65f, 1f);
        GUI.Label(new Rect(rect.x + 5f, rect.yMax - 20f, rect.width - 10f, 18f),
                  meetsRequirements ? $"{skill.manaCost:0} mana" : "<color=#f66>locked</color>", textStyle);

        // Swing / cast in progress
        float busy = user.BusyFraction;
        if (busy > 0f)
            Fill(new Rect(rect.x, rect.y + rect.height * (1f - busy), rect.width, rect.height * busy), new Color(0f, 0f, 0f, 0.55f));
    }

    private void DrawBar(Rect rect, float fraction, Color color, string label)
    {
        Fill(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0f, 0f, 0f, 0.85f));
        Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
        centerStyle.normal.textColor = Color.white;
        GUI.Label(rect, label, centerStyle);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void Outline(Rect r, Color color)
    {
        Fill(new Rect(r.x, r.y, r.width, 2f), color);
        Fill(new Rect(r.x, r.yMax - 2f, r.width, 2f), color);
        Fill(new Rect(r.x, r.y, 2f, r.height), color);
        Fill(new Rect(r.xMax - 2f, r.y, 2f, r.height), color);
    }

    private void EnsureStyles()
    {
        if (textStyle != null && textStyle.fontSize == fontSize) return;

        textStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true, wordWrap = false };
        centerStyle = new GUIStyle(textStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
        };
    }
}
