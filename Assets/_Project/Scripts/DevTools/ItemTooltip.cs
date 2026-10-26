using UnityEngine;

/// <summary>Temporary tooltip box drawn next to the mouse. Call from OnGUI during Repaint.</summary>
public static class ItemTooltip
{
    private const float Padding = 10f;
    private static GUIStyle style;

    public static void Draw(Item item, Vector2 guiMousePosition)
    {
        if (item == null || Event.current.type != EventType.Repaint) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false, fontSize = 14 };
            style.normal.textColor = Color.white;
        }

        var content = new GUIContent(item.BuildTooltip());
        Vector2 size = style.CalcSize(content);

        var rect = new Rect(guiMousePosition.x + 18f, guiMousePosition.y + 18f,
                            size.x + Padding * 2f, size.y + Padding * 2f);

        // Keep it on screen.
        if (rect.xMax > Screen.width) rect.x = guiMousePosition.x - rect.width - 10f;
        if (rect.yMax > Screen.height) rect.y = Screen.height - rect.height;
        if (rect.x < 0f) rect.x = 0f;
        if (rect.y < 0f) rect.y = 0f;

        Color previous = GUI.color;
        GUI.color = Item.RarityColor(item.Rarity);
        GUI.DrawTexture(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.97f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(rect.x + Padding, rect.y + Padding, size.x, size.y), content, style);
    }
}
